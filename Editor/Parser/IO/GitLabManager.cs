using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Manages the read and write operations of GitLab files
    /// </summary>
    public class GitLabManager : IOManager
    {
        private readonly string privateKey;

        /// <summary>
        /// Branch to work on
        /// </summary>
        public string Branch { get; }

        /// <summary>
        /// Project to work on
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// Key of the manager's source type
        /// </summary>
        public static SourceType SourceKey => SourceType.GitLab;

        /// <summary>
        /// Creates a GitLabManager
        /// </summary>
        /// <param name="projectId">ID of the project to work on</param>
        /// <param name="privateKey">Private key to work with</param>
        /// <param name="branch">Branch of the project to work on</param>
        public GitLabManager(string projectId, string privateKey = default, string branch = "master")
        {
            ProjectId = projectId;
            this.privateKey = privateKey;
            Branch = branch;
        }

        /// <summary>
        /// Gets the directories in a path
        /// </summary>
        /// <param name="rootPath">Path to get directories from</param>
        /// <param name="exclude">Files to exclude</param>
        /// <returns>List of directories</returns>
        private List<FileMeta> GetDirectories(string rootPath, string[] exclude = null)
            => (from file in FetchTree(rootPath)
                    where Path.GetExtension(file.Path) == string.Empty
                    let name = Path.GetFileNameWithoutExtension(file.Path)
                    where exclude == null || !exclude.Contains(name)
                    select new FileMeta(name, file.Path))
                .ToList();

        /// <summary>
        /// Gets the definition files in a path
        /// </summary>
        /// <param name="rootPath">Path to get files from</param>
        /// <param name="exclude">Files to exclude</param>
        /// <returns>List of files</returns>
        private List<FileMeta> GetDefinitionFiles(string rootPath, string[] exclude = null)
        {
            var files = FetchTree(rootPath, true);
            files.Sort(CompareFileMeta);

            var filteredFiles = new List<FileMeta>();

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.Path);
                var name = Path.GetFileNameWithoutExtension(file.Path);
                if (exclude != null && exclude.Contains(name)) continue;
                switch (ext)
                {
                    case "":
                        filteredFiles.Add(new FileMeta(name, file.Path, new List<FileMeta>()));
                        break;
                    case ".json":
                    {
                        if (file.Path == null)
                            break;

                        var relativePath = file.Path.Substring(rootPath.Length);
                        if (relativePath.Length > 0 && relativePath.StartsWith("/"))
                        {
                            relativePath = relativePath.Substring(1);
                        }

                        var parts = relativePath.Split('/');

                        if (parts.Length == 1)
                        {
                            filteredFiles.Add(new FileMeta(name, file.Path));
                        }
                        else
                        {
                            var ownerFile = filteredFiles.First(f => f.Path == CombinePaths(rootPath, parts[0]));
                            for (var i = 1; i < parts.Length - 1; ++i)
                            {
                                ownerFile = ownerFile.DirContent.First(f => f.Name == parts[i]);
                            }

                            ownerFile.DirContent.Add(new FileMeta(name, file.Path));
                        }

                        break;
                    }
                }
            }

            return filteredFiles;
        }

        /// <summary>
        /// Compares two FileMetas for sorting like a file explorer
        /// </summary>
        /// <param name="fm1">First FileMeta</param>
        /// <param name="fm2">Second FileMeta</param>
        /// <returns>1 if fm1 comes after fm2, 0 if they are the same, -1 if fm1 comes before fm2</returns>
        private static int CompareFileMeta(FileMeta fm1, FileMeta fm2)
        {
            var parts1 = fm1.Path.Split('/');
            var parts2 = fm2.Path.Split('/');
            var i = 0;

            while (i < parts1.Length && i < parts2.Length)
            {
                var compare = parts1[i].CompareTo(parts2[i]);
                if (compare != 0)
                {
                    var ext1 = Path.GetExtension(parts1[i]);
                    var ext2 = Path.GetExtension(parts2[i]);
                    switch (ext1)
                    {
                        case "" when ext2 == ".json":
                            return -1;
                        case ".json" when ext2 == string.Empty:
                            return 1;
                        default:
                            return compare;
                    }
                }

                ++i;
            }

            if (parts1.Length < parts2.Length)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Fetches a directory tree from GitLab
        /// </summary>
        /// <param name="path">Path to fetch a tree from</param>
        /// <param name="rec">Should the tree be fetched recursively or only for the direct path</param>
        /// <returns>List of files</returns>
        private List<FileMeta> FetchTree(string path, bool rec = false)
        {
            var trees = HttpHelper.GetGitLabTree(ProjectId, Branch, path, privateKey, rec);

            var files = new List<FileMeta>();
            if (trees == null) return files;
            foreach (var tree in trees)
            {
                files.AddRange(JsonConvert.DeserializeObject<FileMeta[]>(tree));
            }

            return files;
        }

        /// <summary>
        /// Fetches a file from GitLab
        /// </summary>
        /// <param name="path">Path of the file to fetch</param>
        /// <returns>Content of the fetched file</returns>
        private string FetchFile(string path)
        {
            return HttpHelper.GetGitLabFile(ProjectId, Branch, path, privateKey);
        }

        /// <summary>
        /// Asynchronously fetches a file from GitLab
        /// </summary>
        /// <param name="path">Path of the file to fetch</param>
        /// <returns>Content of the fetched file</returns>
        private async Task<string> FetchFileAsync(string path)
        {
            return await HttpHelper.GetGitLabFileAsync(ProjectId, Branch, path, privateKey);
        }

        protected override List<FileMeta> ListActivities(FileMeta context)
        {
            return GetDefinitionFiles(context.Path + "/activities");
        }

        protected override List<FileMeta> ListContexts(string rootPath, string[] exclude = null)
        {
            return GetDirectories(rootPath, exclude);
        }

        public override List<Context> ReadContextsByDefs(string rootPath, IEnumerable<string> includes)
        {
            throw new NotImplementedException();
        }

        public override List<FileMeta> GetFileMetasByDefs(string rootPath, IEnumerable<string> includes)
        {
            throw new NotImplementedException();
        }

        public override List<Context> ListContextsFull(string rootPath, string[] exclude = null)
        {
            var files = FetchTree(rootPath, true);
            files.Sort(CompareFileMeta);

            var contexts = new List<Context>();

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.Path);
                var name = Path.GetFileNameWithoutExtension(file.Path);
                if (exclude != null && exclude.Contains(name) || file.Path == null) continue;
                var relativePath = file.Path.Substring(rootPath.Length);
                if (relativePath.Length > 0 && relativePath.StartsWith("/"))
                {
                    relativePath = relativePath.Substring(1);
                }

                var parts = relativePath.Split('/');
                if (parts.Length == 1)
                {
                    contexts.Add(new Context(parts[0]));
                }
                else if (parts.Length >= 3)
                {
                    var owner = CombinePaths(rootPath, parts[0], parts[1]);
                    var context = contexts.First(c => c.Name == parts[0]);
                    List<FileMeta> container = null;

                    switch (parts[1])
                    {
                        case "activities":
                            container = context.Activities;
                            break;
                        case "extensions":
                        {
                            if (!context.Extensions.ContainsKey(parts[2]))
                            {
                                context.Extensions.Add(parts[2], new List<FileMeta>());
                            }

                            container = context.Extensions[parts[2]];
                            break;
                        }
                        case "verbs":
                            container = context.Verbs;
                            break;
                    }

                    if (container == null
                        || (parts[1] == "extensions"
                            && parts.Length <= 3)) continue;
                    var start = parts[1] == "extensions" ? 3 : 2;
                    for (var i = start; i < parts.Length - 1 && container != null; ++i)
                    {
                        container = container.Find(f => f.Name == parts[i])?.DirContent;
                    }

                    container?.Add(ext == string.Empty
                        ? new FileMeta(name, file.Path, new List<FileMeta>())
                        : new FileMeta(name, file.Path));
                }
            }

            return contexts;
        }

        public override List<FileMeta> ListDeep(string rootPath, string[] includes = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetAllKeys(string rootPath)
        {
            throw new NotImplementedException();
        }

        protected override List<FileMeta> ListExtensions(FileMeta contextExtType)
        {
            return GetDefinitionFiles(contextExtType.Path);
        }

        protected override List<FileMeta> ListExtensionTypes(FileMeta context)
        {
            return GetDirectories(context.Path + "/extensions");
        }

        protected override List<FileMeta> ListVerbs(FileMeta context)
        {
            return GetDefinitionFiles(context.Path + "/verbs");
        }

        protected override List<FileMeta> ReadFiles(List<FileMeta> files)
        {
            foreach (var file in files)
            {
                file.Name = Path.GetFileNameWithoutExtension(file.Name).ClearName();
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
            var fileTaskDict = new Dictionary<FileMeta, Task>();
            foreach (var file in files)
            {
                file.Name = Path.GetFileNameWithoutExtension(file.Name).ClearName();
                if (file.IsDirectory)
                {
                    fileTaskDict.Add(file, ReadFilesAsync(file.DirContent));
                }
                else
                {
                    fileTaskDict.Add(file, ReadFileAsync(file.Path));
                }
            }

            foreach (var kvp in fileTaskDict)
            {
                if (kvp.Key.IsDirectory)
                {
                    kvp.Key.DirContent = await (Task<List<FileMeta>>)kvp.Value;
                }
                else
                {
                    kvp.Key.FileContent = await (Task<string>)kvp.Value;
                }
            }

            return files;
        }

        protected override string ReadFile(string path)
        {
            Log($"GitLabManager reading file: {path}");
            return FetchFile(path);
        }

        protected override async Task<string> ReadFileAsync(string path)
        {
            Log($"GitLabManager reading file: {path}");
            return await FetchFileAsync(path);
        }

        public override void WriteCodeFiles(string dest, List<FileMeta> coreFiles, string coreDirName,
            List<FileMeta> files, string definitionsDirName)
        {
            HttpHelper.InitGitLabCommitActions();

            base.WriteCodeFiles(dest, coreFiles, coreDirName, files, definitionsDirName);

            HttpHelper.PostGitLabCommit(ProjectId, Branch, privateKey, "Generated xAPI Definitions Files");
            HttpHelper.ResetGitLabCommitActions();
        }

        protected override string CombinePaths(params string[] paths)
            => paths.Aggregate(string.Empty,
                (current, path) => current + (current == string.Empty ? path : "/" + path));

        protected override void WriteFiles(string dest, List<FileMeta> files)
        {
            var directCall = !HttpHelper.IsActionsInitialized;
            if (directCall)
            {
                HttpHelper.InitGitLabCommitActions();
            }

            var gitFiles = FetchTree(dest);

            foreach (var file in files)
            {
                var filePath = $"{dest}/{file.Name}";
                Log($"GitLabManager writing file: {filePath}");
                var exists = gitFiles.Any(f => f.Path == filePath);
                if (exists)
                {
                    HttpHelper.AddGitLabCommitUpdate(filePath, file.FileContent);
                }
                else
                {
                    HttpHelper.AddGitLabCommitCreate(filePath, file.FileContent);
                }
            }

            if (!directCall) return;
            HttpHelper.PostGitLabCommit(ProjectId, Branch, privateKey, "Generated xAPI Definitions Files");
            HttpHelper.ResetGitLabCommitActions();
        }

        protected override async Task WriteFilesAsync(string dest, List<FileMeta> files)
        {
            var directCall = !HttpHelper.IsActionsInitialized;
            if (directCall)
            {
                HttpHelper.InitGitLabCommitActions();
            }

            var gitFiles = FetchTree(dest);

            foreach (var file in files)
            {
                var filePath = $"{dest}/{file.Name}";
                Log($"GitLabManager writing file: {filePath}");
                var exists = gitFiles.Any(f => f.Path == filePath);
                if (exists)
                {
                    HttpHelper.AddGitLabCommitUpdate(filePath, file.FileContent);
                }
                else
                {
                    HttpHelper.AddGitLabCommitCreate(filePath, file.FileContent);
                }
            }

            if (directCall)
            {
                await HttpHelper.PostGitLabCommitAsync(ProjectId, Branch, privateKey,
                    "Generated xAPI Definitions Files");
                HttpHelper.ResetGitLabCommitActions();
            }
        }
    }
}