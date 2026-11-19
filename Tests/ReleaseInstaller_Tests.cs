/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using xAPI4Unity.Editor.Releases;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for installing and updating an official release with <see cref="ReleaseInstaller"/>, in a temporary folder.
    /// </summary>
    [TestFixture]
    public class ReleaseInstaller_Tests
    {
        /// <summary>
        /// Temporary folder of a test, deleted afterwards.
        /// </summary>
        private string _directory;

        /// <summary>
        /// Extracted new release.
        /// </summary>
        private string Release => Path.Combine(_directory, "release");
        /// <summary>
        /// Extracted installed release.
        /// </summary>
        private string Baseline => Path.Combine(_directory, "baseline");
        /// <summary>
        /// The project's source folder, like <c>Assets/xapi</c>.
        /// </summary>
        private string Destination => Path.Combine(_directory, "xapi");

        /// <summary>
        /// Creates the temporary folder.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "xAPI4Unity.Tests." + Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
        }

        /// <summary>
        /// Deletes the temporary folder.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        /// <summary>
        /// Writes a file below a root, creating its folders.
        /// </summary>
        private static void Write(string root, string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Reads a file below the destination.
        /// </summary>
        private string Read(string relativePath) => File.ReadAllText(Path.Combine(Destination, relativePath));

        /// <summary>
        /// A first install copies the definitions and the root files and writes the version.
        /// </summary>
        [Test]
        public void Install_Copies_A_Release_Into_An_Empty_Folder()
        {
            // Arrange
            Write(Release, "definitions/a/verbs/x.json", "x");
            Write(Release, "CHANGELOG.md", "# log");
            Write(Release, "site/index.html", "<html>");

            // Act
            var report = ReleaseInstaller.Install(Release, null, Destination, "v2.1.0");

            // Assert
            Assert.AreEqual(1, report.copied);
            Assert.AreEqual("x", Read("definitions/a/verbs/x.json"));
            Assert.AreEqual("# log", Read("CHANGELOG.md"));
            Assert.IsFalse(Directory.Exists(Path.Combine(Destination, "site")));
            Assert.AreEqual("2.1.0", ReleaseInstaller.ReadVersion(Destination));
        }

        /// <summary>
        /// With the installed release as baseline, unchanged files are updated or removed, changed files become conflicts.
        /// </summary>
        [Test]
        public void Install_Updates_Unchanged_Files_And_Keeps_Changed_Ones()
        {
            // Arrange
            Write(Baseline, "definitions/same.json", "same");
            Write(Baseline, "definitions/updated.json", "old");
            Write(Baseline, "definitions/mine.json", "old");
            Write(Baseline, "definitions/gone/removed.json", "old");
            Write(Baseline, "definitions/kept.json", "old");

            Write(Destination, "definitions/same.json", "same");
            Write(Destination, "definitions/updated.json", "old");
            Write(Destination, "definitions/mine.json", "my change");
            Write(Destination, "definitions/gone/removed.json", "old");
            Write(Destination, "definitions/gone/removed.json.meta", "guid");
            Write(Destination, "definitions/kept.json", "my change");
            Write(Destination, "definitions/own.json", "my own");

            Write(Release, "definitions/same.json", "same");
            Write(Release, "definitions/updated.json", "new");
            Write(Release, "definitions/mine.json", "new");
            Write(Release, "definitions/added.json", "new");

            // Act
            var report = ReleaseInstaller.Install(Release, new[] { Baseline }, Destination, "2.1.0");

            // Assert
            CollectionAssert.AreEqual(new[] { "definitions/same.json" }, report.SkipResults.Select(r => r.path));
            CollectionAssert.AreEqual(new[] { "definitions/updated.json" }, report.UpdateResults.Select(r => r.path));
            CollectionAssert.AreEqual(new[] { "definitions/mine.json" }, report.ConflictResults.Select(r => r.path));
            CollectionAssert.AreEqual(new[] { "definitions/added.json" }, report.CopyResults.Select(r => r.path));
            CollectionAssert.AreEqual(new[] { "definitions/gone/removed.json" }, report.RemoveResults.Select(r => r.path));

            Assert.AreEqual("new", Read("definitions/updated.json"));
            Assert.AreEqual("my change", Read("definitions/mine.json"));
            Assert.AreEqual("new", Read("definitions/mine.json-conflict"));
            Assert.AreEqual("my change", Read("definitions/kept.json"), "Changed files stay even if the release removed them.");
            Assert.AreEqual("my own", Read("definitions/own.json"));
            Assert.IsFalse(Directory.Exists(Path.Combine(Destination, "definitions/gone")), "Emptied folders are removed with their .meta files.");
        }

        /// <summary>
        /// With several baselines, a file that equals any earlier release counts as unchanged.
        /// </summary>
        [Test]
        public void Install_Accepts_Files_From_Any_Baseline()
        {
            // Arrange
            var older = Path.Combine(_directory, "older");
            Write(older, "definitions/a.json", "v1");
            Write(Baseline, "definitions/a.json", "v2");
            Write(Baseline, "definitions/b.json", "v2");
            Write(Destination, "definitions/a.json", "v1");
            Write(Destination, "definitions/b.json", "v2");
            Write(Destination, "definitions/b.json-conflict", "from an install without baseline");
            Write(Release, "definitions/a.json", "v3");
            Write(Release, "definitions/b.json", "v3");

            // Act
            var report = ReleaseInstaller.Install(Release, new[] { Baseline, older, null }, Destination, "3.0.0");

            // Assert
            Assert.AreEqual(2, report.updated);
            Assert.AreEqual(0, report.conflicts);
            Assert.AreEqual("v3", Read("definitions/a.json"));
            Assert.IsFalse(File.Exists(Path.Combine(Destination, "definitions/b.json-conflict")));
        }

        /// <summary>
        /// Without a baseline, every file that differs is a conflict, because nothing tells whether you changed it.
        /// </summary>
        [Test]
        public void Install_Without_Baseline_Keeps_Every_Differing_File()
        {
            // Arrange
            Write(Destination, "definitions/x.json", "local");
            Write(Release, "definitions/x.json", "release");

            // Act
            var report = ReleaseInstaller.Install(Release, null, Destination, "2.1.0");

            // Assert
            Assert.AreEqual(1, report.conflicts);
            Assert.AreEqual("local", Read("definitions/x.json"));
            Assert.AreEqual("release", Read("definitions/x.json-conflict"));
        }

        /// <summary>
        /// A conflict file from an earlier update is deleted once the file matches the release.
        /// </summary>
        [Test]
        public void Install_Deletes_Resolved_Conflict_Files()
        {
            // Arrange
            Write(Destination, "definitions/x.json", "release");
            Write(Destination, "definitions/x.json-conflict", "older release");
            Write(Release, "definitions/x.json", "release");

            // Act
            ReleaseInstaller.Install(Release, null, Destination, "2.1.0");

            // Assert
            Assert.IsFalse(File.Exists(Path.Combine(Destination, "definitions/x.json-conflict")));
        }

        /// <summary>
        /// A release without a definitions folder is rejected before anything is written.
        /// </summary>
        [Test]
        public void Install_Rejects_A_Release_Without_Definitions()
        {
            // Arrange
            Directory.CreateDirectory(Release);

            // Act, Assert
            Assert.Throws<InvalidDataException>(() => ReleaseInstaller.Install(Release, null, Destination, "2.1.0"));
            Assert.IsFalse(Directory.Exists(Destination));
        }

        /// <summary>
        /// The version file is read without 'v' and whitespace; a missing or broken file gives null.
        /// </summary>
        [Test]
        public void ReadVersion_Reads_The_Version_File()
        {
            // Arrange
            Write(Destination, "version.txt", " v2.1.0 \n");

            // Act, Assert
            Assert.AreEqual("2.1.0", ReleaseInstaller.ReadVersion(Destination));
            Assert.IsNull(ReleaseInstaller.ReadVersion(Path.Combine(_directory, "missing")));
            Write(Destination, "version.txt", "not a version");
            Assert.IsNull(ReleaseInstaller.ReadVersion(Destination));
        }

        /// <summary>
        /// GitHub archives put everything into one folder, which is the root. Other layouts are used as they are.
        /// </summary>
        [Test]
        public void FindArchiveRoot_Steps_Into_The_Single_Folder()
        {
            // Arrange
            Write(Path.Combine(_directory, "single"), "xapi-2.1.0/definitions/x.json", "x");
            Write(Path.Combine(_directory, "flat"), "definitions/x.json", "x");
            Write(Path.Combine(_directory, "flat"), "README.md", "x");

            // Act
            var single = ReleaseInstaller.FindArchiveRoot(Path.Combine(_directory, "single"));
            var flat = ReleaseInstaller.FindArchiveRoot(Path.Combine(_directory, "flat"));

            // Assert
            Assert.AreEqual(Path.Combine(_directory, "single", "xapi-2.1.0"), single);
            Assert.AreEqual(Path.Combine(_directory, "flat"), flat);
        }
    }
}
#endif
