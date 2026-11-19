/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace xAPI4Unity.Editor.Releases
{
    /// <summary>
    /// Installs an extracted release of the official definitions into a source folder such as <c>Assets/xapi</c>.
    /// With earlier releases as baselines, it is a three-way update: a file that equals its version in a baseline was
    /// not changed by you, so it is updated or removed. Files you changed are kept, and the release's version is saved
    /// next to them as a conflict.
    /// It only works on folders, so tests can run it on temporary folders.
    /// </summary>
    internal static class ReleaseInstaller
    {
        /// <summary>
        /// Folder of the definitions in a release and in the source folder.
        /// </summary>
        public const string DefinitionsFolder = "definitions";
        /// <summary>
        /// File in the source folder that holds the installed version, such as <c>2.1.0</c>. Releases since 2.1.0 ship it too.
        /// </summary>
        public const string VersionFile = "version.txt";
        /// <summary>
        /// Suffix of the release's version of a file you changed.
        /// </summary>
        public const string ConflictSuffix = "-conflict";
        /// <summary>
        /// Files of the release root that are copied as they are. They are not meant to be edited.
        /// </summary>
        private static readonly string[] RootFiles = { "README.md", "CHANGELOG.md", "LICENSE" };

        /// <summary>
        /// Installs a release into the destination folder.
        /// </summary>
        /// <param name="release">Root of the extracted release, the folder with <c>definitions</c>.</param>
        /// <param name="baselines">Roots of extracted earlier releases, usually the installed one. Null or empty if unknown:
        /// then every file that differs is a conflict.</param>
        /// <param name="destination">The source folder, such as <c>Assets/xapi</c>. Created if missing.</param>
        /// <param name="version">Version written to <see cref="VersionFile"/>, such as <c>2.1.0</c>.</param>
        /// <returns>What happened to each definition file, with paths relative to the destination.</returns>
        /// <exception cref="InvalidDataException">The release has no <c>definitions</c> folder.</exception>
        public static MergeReport Install(string release, IEnumerable<string> baselines, string destination, string version)
        {
            var releaseDefinitions = Path.Combine(release, DefinitionsFolder);
            if (!Directory.Exists(releaseDefinitions))
                throw new InvalidDataException($"The release has no '{DefinitionsFolder}' folder.");

            var roots = (baselines ?? Enumerable.Empty<string>())
                .Where(b => b != null && Directory.Exists(Path.Combine(b, DefinitionsFolder)))
                .ToList();

            // True if the file is unchanged since one of the baselines
            bool FromBaseline(string target, string relative)
                => roots.Any(root => File.Exists(Path.Combine(root, relative)) && SameContent(target, Path.Combine(root, relative)));

            Directory.CreateDirectory(destination);
            var report = new MergeReport(release);

            foreach (var releaseFile in Directory.GetFiles(releaseDefinitions, "*.json", SearchOption.AllDirectories))
            {
                var relative = Relative(release, releaseFile);
                var target = Path.Combine(destination, relative);

                if (!File.Exists(target))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(releaseFile, target);
                    DeleteIfExists(target + ConflictSuffix);
                    report.AddCopy(relative);
                }
                else if (SameContent(target, releaseFile))
                {
                    // An older conflict is resolved once the file matches the release
                    DeleteIfExists(target + ConflictSuffix);
                    report.AddSkip(relative);
                }
                else if (FromBaseline(target, relative))
                {
                    // Unchanged since an earlier release
                    File.Copy(releaseFile, target, true);
                    DeleteIfExists(target + ConflictSuffix);
                    report.AddUpdate(relative);
                }
                else
                {
                    File.Copy(releaseFile, target + ConflictSuffix, true);
                    report.AddConflict(relative);
                }
            }

            if (roots.Count > 0)
            {
                var removedFromRelease = roots
                    .SelectMany(root => Directory.GetFiles(Path.Combine(root, DefinitionsFolder), "*.json", SearchOption.AllDirectories)
                        .Select(file => Relative(root, file)))
                    .Distinct()
                    .Where(relative => !File.Exists(Path.Combine(release, relative)))
                    .ToList();
                foreach (var relative in removedFromRelease)
                {
                    var target = Path.Combine(destination, relative);
                    // Keep files you changed, even if the release removed them
                    if (!File.Exists(target) || !FromBaseline(target, relative))
                        continue;
                    File.Delete(target);
                    DeleteIfExists(target + ".meta");
                    DeleteIfExists(target + ConflictSuffix);
                    report.AddRemove(relative);
                }
                DeleteEmptyFolders(Path.Combine(destination, DefinitionsFolder));
            }

            foreach (var name in RootFiles)
            {
                var file = Path.Combine(release, name);
                if (File.Exists(file))
                    File.Copy(file, Path.Combine(destination, name), true);
            }
            WriteVersion(destination, version);
            return report;
        }

        /// <summary>
        /// The installed version from <see cref="VersionFile"/> in the source folder, or null if there is none.
        /// </summary>
        public static string ReadVersion(string sourceFolder)
        {
            if (string.IsNullOrEmpty(sourceFolder))
                return null;
            var file = Path.Combine(sourceFolder, VersionFile);
            if (!File.Exists(file))
                return null;
            try
            {
                var text = File.ReadAllText(file).Trim();
                return ReleaseVersion.TryParse(text, out var version) ? version.ToString() : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// Writes the version to <see cref="VersionFile"/> in the source folder, without a leading <c>v</c>.
        /// </summary>
        public static void WriteVersion(string sourceFolder, string version)
        {
            if (ReleaseVersion.TryParse(version, out var parsed))
                version = parsed.ToString();
            File.WriteAllText(Path.Combine(sourceFolder, VersionFile), version + "\n");
        }

        /// <summary>
        /// The root of an extracted archive: its only folder if it has exactly one folder and no files
        /// (GitHub and GitLab archives put everything into <c>&lt;repo&gt;-&lt;ref&gt;/</c>), otherwise the folder itself.
        /// </summary>
        public static string FindArchiveRoot(string extractedFolder)
        {
            var folders = Directory.GetDirectories(extractedFolder);
            var files = Directory.GetFiles(extractedFolder);
            return folders.Length == 1 && files.Length == 0 ? folders[0] : extractedFolder;
        }

        /// <summary>
        /// Path of a file relative to a root, with <c>/</c> as separator.
        /// </summary>
        private static string Relative(string root, string file)
            => FileHelper.GetRelativePath(root, file).Replace('\\', '/');

        /// <summary>
        /// True if both files have the same bytes.
        /// </summary>
        private static bool SameContent(string a, string b)
        {
            var left = new FileInfo(a);
            var right = new FileInfo(b);
            return left.Length == right.Length && File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b));
        }

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        private static void DeleteIfExists(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        /// <summary>
        /// Deletes folders below the root that hold no files anymore, together with their <c>.meta</c> files.
        /// </summary>
        private static void DeleteEmptyFolders(string root)
        {
            if (!Directory.Exists(root))
                return;
            foreach (var folder in Directory.GetDirectories(root))
            {
                DeleteEmptyFolders(folder);
                if (Directory.EnumerateFileSystemEntries(folder).Any())
                    continue;
                Directory.Delete(folder);
                DeleteIfExists(folder + ".meta");
            }
        }
    }
}
