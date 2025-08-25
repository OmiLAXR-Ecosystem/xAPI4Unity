/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Implements the IOManager to manage local file-based xAPI definition access.
    /// Provides functionality for reading, writing, and traversing JSON-based directories and files.
    /// </summary>
    internal class FileManager : IOManager
    {
        private readonly string _rootPath;

        /// <summary>
        /// Gets the associated source type for this file manager.
        /// </summary>
        public static SourceType SourceKey => SourceType.Local;

        /// <summary>
        /// Initializes a new instance of the FileManager class.
        /// </summary>
        /// <param name="rootPath">The root directory for local file access.</param>
        public FileManager(string rootPath)
        {
            _rootPath = rootPath;
        }

        /// <summary>
        /// Retrieves subdirectories within the specified path.
        /// </summary>
        /// <param name="rootPath">The path to search.</param>
        /// <param name="exclude">Names of folders to exclude.</param>
        /// <param name="ignoreNotFoundException">Whether to suppress exceptions if the directory does not exist.</param>
        /// <returns>List of FileMeta representing the directories found.</returns>
        private static List<FileMeta> GetDirectories(string rootPath, string[] exclude = null, bool ignoreNotFoundException = false)
        {
            var dirs = new List<FileMeta>();
            if (!Directory.Exists(rootPath))
            {
                if (ignoreNotFoundException)
                    return dirs;
                throw new DirectoryNotFoundException("Cannot find directory: " + rootPath);
            }

            dirs.AddRange(from path in Directory.GetDirectories(rootPath)
                          let name = Path.GetFileNameWithoutExtension(path)
                          where exclude == null || !exclude.Contains(name)
                          select new FileMeta(name, path));

            return dirs;
        }

        /// <summary>
        /// Recursively retrieves all files and subdirectories matching criteria from the root path.
        /// </summary>
        /// <param name="rootPath">The path to scan.</param>
        /// <param name="exclude">Names to exclude.</param>
        /// <returns>List of FileMeta representing files and folders.</returns>
        private static List<FileMeta> GetFiles(string rootPath, string[] exclude = null)
        {
            var files = new List<FileMeta>();
            if (!Directory.Exists(rootPath)) return files;

            files.AddRange(from path in Directory.GetDirectories(rootPath)
                           let name = Path.GetFileNameWithoutExtension(path)
                           where exclude == null || !exclude.Contains(name)
                           select new FileMeta(name, path, GetFiles(path, exclude)));

            files.AddRange(from path in Directory.GetFiles(rootPath)
                           where Path.GetExtension(path) == ".json"
                           let name = Path.GetFileNameWithoutExtension(path)
                           where exclude == null || !exclude.Contains(name)
                           select new FileMeta(name, path));

            return files;
        }

        /// <summary>
        /// Gets all file paths under the root directory, including subdirectories.
        /// </summary>
        private static IEnumerable<string> GetAllFiles(string rootPath, string pattern = "*.*")
            => Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories);

        /// <inheritdoc/>
        public override List<FileMeta> ListDeep(string rootPath, string[] includes = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override IEnumerable<string> GetAllKeys(string rootPath)
            => GetAllFiles(rootPath).Select(s => s.Replace(rootPath, "").Replace(".json", "")).ToArray();

        /// <summary>
        /// Reads the full content of a file as string.
        /// </summary>
        private string GetFileContent(string path)
        {
#if UNITY_2020_1_OR_NEWER
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            return reader.ReadToEnd();
#else
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                return reader.ReadToEnd();
            }
#endif
        }

        /// <inheritdoc/>
        public override IEnumerable<DefinitionFile> GetAllEntries()
        {
            var allFiles = GetAllFiles(_rootPath, "*.json");
            return allFiles.Select(f => new DefinitionFile(f.Substring(_rootPath.Length + 1), GetFileContent(f))).Where(f => f.Name[0] != '.');
        }

        /// <summary>
        /// Gets the keys (paths) filtered by include patterns.
        /// </summary>
        private IEnumerable<string> GetKeys(string rootPath, IEnumerable<string> includes)
        {
            var safeIncludes = includes.Select(MakeSafePath).ToArray();
            var keys = GetAllKeys(rootPath);
            var filteredKeys = new List<string>();
            foreach (var key in keys)
                filteredKeys.AddRange(from i in safeIncludes
                                      where key.IndexOf(i, StringComparison.Ordinal) == 0
                                      select key);
            return filteredKeys.OrderBy(k => k).ToArray();
        }

        /// <summary>
        /// Normalizes path format (adds leading slash, removes trailing slash).
        /// </summary>
        private static string MakeSafePath(string path)
        {
            if (path[0] != '/')
                path = '/' + path;
            if (path[path.Length - 1] == '/')
                path = path.Substring(0, path.Length - 1);
            return path;
        }

        /// <inheritdoc/>
        protected override List<FileMeta> ListActivities(FileMeta context) =>
            GetFiles(Path.Combine(context.Path, "activities"));

        /// <inheritdoc/>
        protected override List<FileMeta> ListContexts(string rootPath, string[] exclude = null) =>
            GetDirectories(rootPath, exclude);

        /// <inheritdoc/>
        public override List<FileMeta> GetFileMetasByDefs(string rootPath, IEnumerable<string> includes)
        {
            var dirs = new List<FileMeta>();
            if (!Directory.Exists(rootPath))
                return dirs;

            var keys = GetKeys(rootPath, includes);
            var metas = keys.Select(k => new FileMeta(Path.GetFileNameWithoutExtension(k), rootPath + k + ".json"));

            return metas.ToList();
        }

        /// <inheritdoc/>
        public override List<Context> ReadContextsByDefs(string rootPath, IEnumerable<string> includes)
        {
            var contexts = new Dictionary<string, Context>();
            if (!Directory.Exists(rootPath))
                return contexts.Values.ToList();

            var keys = GetKeys(rootPath, includes);
            var subPathMetas = new Dictionary<string, FileMeta>();

            foreach (var key in keys)
            {
                var filePath = rootPath + key;

                var keyParts = key.Substring(1).Split('/');
                var s0 = keyParts[0];
                var s1 = keyParts[1];
                var s2 = keyParts[2];

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileMeta = new FileMeta(fileName.ClearName(), filePath);

                var context = contexts[s0];

                Console.WriteLine("s0:{0},s1:{1},s2:{2}", s0, s1, s2);

                if (keyParts.Length >= 4)
                {
                    var s3 = keyParts[3];
                    var subPath = Path.Combine(rootPath, s0, s1, s2, s3 + ".json");
                    var subMeta = new FileMeta(s3.ClearName(), subPath, File.ReadAllText(subPath));
                    var k = Path.Combine(s0, s1, s2);

                    var newPath = Path.Combine(rootPath, s0, s1, s2);

                    if (subPathMetas.ContainsKey(k))
                    {
                        fileMeta = subPathMetas[k];
                        fileMeta.AppendToDirContent(subMeta);
                        continue;
                    }
                    else
                    {
                        fileMeta = new FileMeta(s2.ClearName(), newPath);
                        subPathMetas.Add(k, fileMeta);
                        fileMeta.AppendToDirContent(subMeta);
                    }
                }
                else
                {
                    fileMeta.Path += ".json";
                    fileMeta.FileContent = File.ReadAllText(fileMeta.Path);
                }

                switch (s1)
                {
                    case "verbs":
                        context.AddVerb(fileMeta);
                        break;
                    case "activities":
                        context.AddActivity(fileMeta);
                        break;
                    case "extensions":
                        context.AddExtension(s2, fileMeta);
                        continue;
                }

                if (contexts.ContainsKey(s0))
                    contexts[s0] = context;
                else
                    contexts.Add(s0, context);
            }

            return contexts.Values.ToList();
        }

        /// <inheritdoc/>
        protected override List<FileMeta> ListExtensions(FileMeta contextExtType) => GetFiles(contextExtType.Path);

        /// <inheritdoc/>
        protected override List<FileMeta> ListExtensionTypes(FileMeta context) =>
            GetDirectories(rootPath: Path.Combine(context.Path, "extensions"), ignoreNotFoundException: true);

        /// <inheritdoc/>
        protected override List<FileMeta> ListVerbs(FileMeta context) => GetFiles(Path.Combine(context.Path, "verbs"));

        /// <inheritdoc/>
        protected override List<FileMeta> ReadFiles(List<FileMeta> files)
        {
            foreach (var file in files)
            {
                file.Name = file.Name.ClearName();
                if (file.IsDirectory)
                {
                    file.DirContent = ReadFiles(file.DirContent);
                }
                else
                {
                    file.FileContent = ReadFile(file.Path);
                }
            }

            return files;
        }

        /// <inheritdoc/>
        protected override async Task<List<FileMeta>> ReadFilesAsync(List<FileMeta> files)
        {
            foreach (var file in files)
            {
                file.Name = file.Name.ClearName();
                if (file.IsDirectory)
                {
                    file.DirContent = await ReadFilesAsync(file.DirContent);
                }
                else
                {
                    file.FileContent = await ReadFileAsync(file.Path);
                }
            }

            return files;
        }

        /// <inheritdoc/>
        protected override string ReadFile(string path)
        {
            Log($"FileManager reading file: {path}");
            return File.ReadAllText(path);
        }

        /// <inheritdoc/>
        protected override async Task<string> ReadFileAsync(string path)
        {
            Log($"FileManager reading file: {path}");
            var task = Task.Run(() => File.ReadAllText(path));
            return await task;
        }

        /// <inheritdoc/>
        protected override void WriteFiles(string dest, List<FileMeta> files)
        {
            if (dest != null && !Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            foreach (var file in files)
            {
                var filePath = CombinePaths(dest, file.Name);
                if (filePath == null)
                    continue;

                Log($"FileManager writing file: {filePath}");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (var writer = File.CreateText(filePath))
                {
                    writer.Write(file.FileContent);
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task WriteFilesAsync(string dest, List<FileMeta> files)
        {
            if (dest != null && !Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            var tasks = new List<Task>();

            foreach (var file in files)
            {
                var filePath = CombinePaths(dest, file.Name);
                if (filePath == null)
                    continue;
                Log($"FileManager writing file: {filePath}");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                tasks.Add(Task.Run(() =>
                {
                    using (var writer = File.CreateText(filePath))
                    {
                        writer.Write(file.FileContent);
                    }
                }));

                await Task.WhenAll(tasks);
            }
        }

        /// <inheritdoc/>
        protected override string CombinePaths(params string[] paths)
        {
            return Path.Combine(paths);
        }
    }
}
#endif