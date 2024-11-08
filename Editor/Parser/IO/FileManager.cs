#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xAPI4Unity.Editor.Parser.IO
{

    /// <summary>
    /// Manages the read and write operations of local files
    /// </summary>
    internal class FileManager : IOManager
    {
        private readonly string _rootPath;
        /// <summary>
        /// Key of the manager's source type
        /// </summary>
        public static SourceType SourceKey => SourceType.Local;

        public FileManager(string rootPath)
        {
            _rootPath = rootPath;
        }

        /// <summary>
        /// Gets the directories in a path
        /// </summary>
        /// <param name="rootPath">Path to get directories from.</param>
        /// <param name="exclude">Files to exclude.</param>
        /// <param name="ignoreNotFoundException">If true, don't throw DirectoryNotFoundException.</param>
        /// <returns>List of directories</returns>
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
        /// Gets the files in a path
        /// </summary>
        /// <param name="rootPath">Path to get files from</param>
        /// <param name="exclude">Files to exclude</param>
        /// <returns>List of files</returns>
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

        private static IEnumerable<string> GetAllFiles(string rootPath, string pattern = "*.*")
            => Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories);

        public override List<FileMeta> ListDeep(string rootPath, string[] includes = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetAllKeys(string rootPath)
            => GetAllFiles(rootPath).Select(s => s.Replace(rootPath, "").Replace(".json", "")).ToArray();

        private string GetFileContent(string path)
        {
            #if UNITY_2019 
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    return reader.ReadToEnd();
                }
            }
            #else 
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            return reader.ReadToEnd();
            #endif
        }
        
        public override IEnumerable<DefinitionFile> GetAllEntries()
        {
            var allFiles = GetAllFiles(_rootPath, "*.json");
            return allFiles.Select(f => new DefinitionFile(f.Substring(_rootPath.Length + 1), GetFileContent(f))).Where(f => f.Name[0] != '.');
        }

        private IEnumerable<string> GetKeys(string rootPath, IEnumerable<string> includes)
        {
            // clear slash notations
            var safeIncludes = includes.Select(MakeSafePath).ToArray();
            var keys = GetAllKeys(rootPath);
            var filteredKeys = new List<string>();
            foreach (var key in keys)
                filteredKeys.AddRange(from i in safeIncludes
                    where key.IndexOf(i, StringComparison.Ordinal) == 0
                    select key);
            return filteredKeys.OrderBy(k => k).ToArray();
        }

        protected override List<FileMeta> ListActivities(FileMeta context) =>
            GetFiles(Path.Combine(context.Path, "activities"));

        private static string MakeSafePath(string path)
        {
            // ensure slash at beginning
            if (path[0] != '/')
                path = '/' + path;
            // remote slash in the end
            if (path[path.Length - 1] == '/')
                path = path.Substring(0, path.Length - 1);
            return path;
        }

        protected override List<FileMeta> ListContexts(string rootPath, string[] exclude = null) =>
            GetDirectories(rootPath, exclude);

        public override List<FileMeta> GetFileMetasByDefs(string rootPath, IEnumerable<string> includes)
        {
            var dirs = new List<FileMeta>();
            if (!Directory.Exists(rootPath))
                return dirs;

            var keys = GetKeys(rootPath, includes);
            var metas = keys.Select(k
                => new FileMeta(
                    Path.GetFileNameWithoutExtension(k),
                    rootPath + k + ".json")
            );

            return metas.ToList();
        }

        public override List<Context> ReadContextsByDefs(string rootPath, IEnumerable<string> includes)
        {
            var contexts = new Dictionary<string, Context>();
            if (!Directory.Exists(rootPath))
                return contexts.Values.ToList();

            var keys = GetKeys(rootPath, includes);

            var subPathMetas = new Dictionary<string, FileMeta>();

            foreach (var key in keys)
            {
                // file information
                var filePath = rootPath + key;

                var keyParts = key.Substring(1).Split('/');
                var s0 = keyParts[0];
                var s1 = keyParts[1];
                var s2 = keyParts[2];

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileMeta = new FileMeta(fileName.ClearName(), filePath);

                // fetch context
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
                    {
                        context.AddExtension(s2, fileMeta);
                        continue;
                    }
                }

                // save context
                if (contexts.ContainsKey(s0))
                    contexts[s0] = context;
                else
                    contexts.Add(s0, context);
            }

            return contexts.Values.ToList();
        }

        protected override List<FileMeta> ListExtensions(FileMeta contextExtType) => GetFiles(contextExtType.Path);

        protected override List<FileMeta> ListExtensionTypes(FileMeta context) =>
            GetDirectories(rootPath: Path.Combine(context.Path, "extensions"), ignoreNotFoundException: true);

        protected override List<FileMeta> ListVerbs(FileMeta context) => GetFiles(Path.Combine(context.Path, "verbs"));

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

        protected override string ReadFile(string path)
        {
            Log($"FileManager reading file: {path}");
            return File.ReadAllText(path);
        }

        protected override async Task<string> ReadFileAsync(string path)
        {
            Log($"FileManager reading file: {path}");
            var task = Task.Run(() => File.ReadAllText(path));
            return await task;
        }

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

        protected override string CombinePaths(params string[] paths)
        {
            return Path.Combine(paths);
        }
    }
}
#endif