using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace xAPI4Unity.Editor.Parser.IO {
    /// <summary>
    /// Manages the read and write operations of a source type
    /// </summary>
    public abstract class IOManager {
        /// <summary>
        /// Logging event
        /// </summary>
        public event Action<string> OnLog;

        protected void Log(string message) => OnLog?.Invoke(message);

        /// <summary>
        /// Creates an IOManager
        /// </summary>
        protected IOManager() {
            Debug.Assert(HasSourceKey(GetType()));
        }

        private static bool HasSourceKey(Type type) {
            return type
                .GetProperties()
                .Any(p => p.Name == "SourceKey"
                       && p.PropertyType == typeof(SourceType)
                       && p.GetAccessors().All(ac => ac.IsPublic || ac.IsStatic));
        }

        /// <summary>
        /// Gets the SourceKeys of all classes that inherits from IOManager
        /// </summary>
        /// <returns>List of the SourceKeys</returns>
        private static IEnumerable<SourceType> GetSubClassesSourceKeys() {
            var sourceTypes = typeof(IOManager).Assembly.GetTypes().Where(t => t.BaseType == typeof(IOManager));
            return sourceTypes
                .Where(HasSourceKey)
                .Select(st => (SourceType)st.GetProperty("SourceKey").GetValue(null)).ToList();
        }

        public static string[] GetSubClassesSourceKeyStrings()
        {
            var sourceKeys = GetSubClassesSourceKeys();
            return sourceKeys.Select(s => s.GetSourceKeyString()).ToArray();
        }

        /// <summary>
        /// Lists the contexts with all definitions
        /// </summary>
        /// <param name="rootPath">Path with the contexts</param>
        /// <param name="exclude">Files and folders to be excluded</param>
        /// <returns>List of contexts</returns>
        public virtual List<Context> ListContextsFull(string rootPath, string[] exclude = null) {
            var contexts = new List<Context>();
            var contextMetas = ListContexts(rootPath, exclude);
            foreach (var contextMeta in contextMetas) {
                var context = new Context(contextMeta);
                context.AddActivities(ListActivities(contextMeta));
                context.AddExtensions(ListAllExtensions(contextMeta));
                context.AddVerbs(ListVerbs(contextMeta));
                contexts.Add(context);
            }
            return contexts;
        }

        public abstract List<FileMeta> ListDeep(string rootPath, string[] includes = null);
        public abstract IEnumerable<string> GetAllKeys(string rootPath);

        public abstract IEnumerable<DefinitionFile> GetAllEntries();

        /// <summary>
        /// Reads the contexts
        /// </summary>
        /// <param name="rootPath">Directory that the definitions are in</param>
        /// <param name="exclude">Files and folders to exclude</param>
        /// <param name="includes">Files and folders to be included</param>
        /// <returns>List of contexts that have been read</returns>
        public virtual List<Context> ReadContexts(string rootPath, string[] exclude = null)
        {
            var contextMetas = ListContexts(rootPath, exclude);
            var contexts = contextMetas.Select(ReadContext).ToList();
            return contexts;
        }

        /// <summary>
        /// Asynchronously reads the contexts
        /// </summary>
        /// <param name="rootPath">Directory that the definitions are in</param>
        /// <param name="exclude">Files and folders to exclude</param>
        /// <returns>List of contexts that have been read</returns>
        public async Task<List<Context>> ReadContextsAsync(string rootPath, string[] exclude = null) {
            var contexts = new List<Context>();
            var contextMetas = ListContexts(rootPath, exclude);
            var tasks = contextMetas.Select(ReadContextAsync).ToList();
            for (var i = 0; i < contextMetas.Count; ++i) {
                contexts.Add(await tasks[i]);
            }
            return contexts;
        }

        /// <summary>
        /// Reads the Contents of the Files in all contexts
        /// </summary>
        /// <param name="contexts">Contexts to be read</param>
        /// <returns>Filled Contexts</returns>
        public virtual List<Context> ReadContexts(List<Context> contexts) {
            foreach (var context in contexts) {
                ReadContext(context);
            }
            return contexts;
        }
        
        /// <summary>
        /// Asynchronously reads the Contents of the Files in all contexts
        /// </summary>
        /// <param name="contexts">Contexts to be read</param>
        /// <returns>Filled Contexts</returns>
        public virtual async Task<List<Context>> ReadContextsAsync(List<Context> contexts) {
            var tasks = contexts.Select(ReadContextAsync).ToList();
            await Task.WhenAll(tasks);
            return contexts;
        }

        /// <summary>
        /// Reads a context
        /// </summary>
        /// <param name="contextMeta">Context to read</param>
        /// <returns>Context that has been read</returns>
        protected virtual Context ReadContext(FileMeta contextMeta) {
            var context = new Context(contextMeta);
            context.AddActivities(ReadActivities(contextMeta));
            context.AddExtensions(ReadExtensions(contextMeta));
            context.AddVerbs(ReadVerbs(contextMeta));
            return context;
        }

        /// <summary>
        /// Asynchronously reads a context
        /// </summary>
        /// <param name="contextMeta">Context to read</param>
        /// <returns>Context that has been read</returns>
        protected virtual async Task<Context> ReadContextAsync(FileMeta contextMeta) {
            var context = new Context(contextMeta);
            context.AddActivities(await ReadActivitiesAsync(contextMeta));
            context.AddExtensions(await ReadExtensionsAsync(contextMeta));
            context.AddVerbs(await ReadVerbsAsync(contextMeta));
            return context;
        }

        /// <summary>
        /// Reads the Contents of the Files in the context
        /// </summary>
        /// <param name="context">Context to be read</param>
        /// <returns>Filled context</returns>
        protected virtual Context ReadContext(Context context) {
            ReadDefinitions(context.Activities);
            foreach (var extList in context.Extensions.Values) {
                ReadDefinitions(extList);
            }
            ReadDefinitions(context.Verbs);
            return context;
        }

        /// <summary>
        /// Asynchronously reads the Contents of the Files in the context
        /// </summary>
        /// <param name="context">Context to be read</param>
        /// <returns>Filled context</returns>
        protected virtual async Task<Context> ReadContextAsync(Context context) {
            await ReadDefinitionsAsync(context.Activities);
            foreach (var extList in context.Extensions.Values) {
                await ReadDefinitionsAsync(extList);
            }
            await ReadDefinitionsAsync(context.Verbs);
            return context;
        }

        /// <summary>
        /// Reads the activities of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <returns>List of activity files that have been read</returns>
        protected virtual IEnumerable<FileMeta> ReadActivities(FileMeta context) {
            return ReadFiles(ListActivities(context));
        }

        /// <summary>
        /// Asynchronously reads the activities of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <returns>List of activity files that have been read</returns>
        protected virtual async Task<List<FileMeta>> ReadActivitiesAsync(FileMeta context) {
            return await ReadFilesAsync(ListActivities(context));
        }

        /// <summary>
        /// Reads listed definitions
        /// </summary>
        /// <param name="definitions">Definitions to read</param>
        /// <returns>List of definitions passed in</returns>
        protected virtual List<FileMeta> ReadDefinitions(List<FileMeta> definitions) {
            foreach (var definition in definitions) {
                if (definition.IsDirectory) {
                    definition.DirContent = ReadFiles(definition.DirContent);
                } else {
                    definition.FileContent = ReadFile(definition.Path);
                }
            }
            return definitions;
        }

        /// <summary>
        /// Asynchronously reads listed definitions
        /// </summary>
        /// <param name="definitions">Definitions to read</param>
        /// <returns>List of definitions passed in</returns>
        protected virtual async Task<List<FileMeta>> ReadDefinitionsAsync(List<FileMeta> definitions) {
            foreach (var definition in definitions) {
                if (definition.IsDirectory) {
                    definition.DirContent = await ReadFilesAsync(definition.DirContent);
                } else {
                    definition.FileContent = await ReadFileAsync(definition.Path);
                }
            }
            return definitions;
        }

        /// <summary>
        /// Reads the extensions of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <returns>List of extension files that have been read</returns>
        protected virtual IDictionary<string, List<FileMeta>> ReadExtensions(FileMeta context) {
            var allExtensions = new Dictionary<string, List<FileMeta>>();
            var extensionTypes = ListExtensionTypes(context);
            foreach (var extensionType in extensionTypes) {
                var extensions = ReadExtensions(context, extensionType);
                allExtensions.Add(extensionType.Name, extensions);
            }
            return allExtensions;
        }

        /// <summary>
        /// Asynchronously reads the extensions of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <returns>List of extension files that have been read</returns>
        protected virtual async Task<IDictionary<string, List<FileMeta>>> ReadExtensionsAsync(FileMeta context) {
            var allExtensions = new Dictionary<string, List<FileMeta>>();
            var extensionTypes = ListExtensionTypes(context);
            foreach (var extensionType in extensionTypes) {
                var extensions = await ReadExtensionsAsync(context, extensionType);
                allExtensions.Add(extensionType.Name, extensions);
            }
            return allExtensions;
        }
        
        /// <summary>
        /// Reads the extensions of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <param name="type">Type of extension to read</param>
        /// <returns>List of extension files that have been read</returns>
        protected virtual List<FileMeta> ReadExtensions(FileMeta context, FileMeta type) {
            return ReadFiles(ListExtensions(type));
        }

        /// <summary>
        /// Asynchronously reads the extensions of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <param name="type">Type of extension to read</param>
        /// <returns>List of extension files that have been read</returns>
        protected virtual async Task<List<FileMeta>> ReadExtensionsAsync(FileMeta context, FileMeta type) {
            return await ReadFilesAsync(ListExtensions(type));
        }

        /// <summary>
        /// Reads the verbs of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <returns>List of verb files that have been read</returns>
        protected virtual List<FileMeta> ReadVerbs(FileMeta context) {
            return ReadFiles(ListVerbs(context));
        }

        /// <summary>
        /// Asynchronously reads the verbs of a context
        /// </summary>
        /// <param name="context">Context to read from</param>
        /// <returns>List of verb files that have been read</returns>
        protected virtual async Task<List<FileMeta>> ReadVerbsAsync(FileMeta context) {
            return await ReadFilesAsync(ListVerbs(context));
        }

        /// <summary>
        /// Reads files
        /// </summary>
        /// <param name="files">List of files to read</param>
        /// <returns>List of files that have been read</returns>
        protected abstract List<FileMeta> ReadFiles(List<FileMeta> files);

        /// <summary>
        /// Reads files asynchronously
        /// </summary>
        /// <param name="files">List of files to read</param>
        /// <returns>Task that returns list of files that have been read</returns>
        protected abstract Task<List<FileMeta>> ReadFilesAsync(List<FileMeta> files);

        /// <summary>
        /// Reads file
        /// </summary>
        /// <param name="path">Path of the file to read</param>
        /// <returns>Content of the file</returns>
        protected abstract string ReadFile(string path);

        /// <summary>
        /// Reads file asynchronously
        /// </summary>
        /// <param name="path">File to read</param>
        /// <returns>Task that returns content of the file</returns>
        protected abstract Task<string> ReadFileAsync(string path);

        /// <summary>
        /// Lists the activities in a context
        /// </summary>
        /// <param name="context">Context whose activities are listed</param>
        /// <returns>List of activities in the context</returns>
        protected abstract List<FileMeta> ListActivities(FileMeta context);
        /// <summary>
        /// Lists the contexts
        /// </summary>
        /// <param name="rootPath">Path with the contexts</param>
        /// <param name="exclude">Files and folders to be excluded</param>
        /// <returns>List of contexts</returns>
        protected abstract List<FileMeta> ListContexts(string rootPath, string[] exclude = null);
        public abstract List<Context> ReadContextsByDefs(string rootPath, IEnumerable<string> includes);
        public abstract List<FileMeta> GetFileMetasByDefs(string rootPath, IEnumerable<string> includes);
        /// <summary>
        /// Lists the extension types in a context
        /// </summary>
        /// <param name="context">Context whose extension types are listed</param>
        /// <returns>List of extension types in the context</returns>
        protected abstract List<FileMeta> ListExtensionTypes(FileMeta context);
        /// <summary>
        /// Lists the extensions in a context
        /// </summary>
        /// <param name="contextExtType">Context whose extensions are listed</param>
        /// <returns>List of extensions in the context</returns>
        protected abstract List<FileMeta> ListExtensions(FileMeta contextExtType);
        /// <summary>
        /// Lists the verbs in a context
        /// </summary>
        /// <param name="context">Context whose verbs are listed</param>
        /// <returns>List of verbs in the context</returns>
        protected abstract List<FileMeta> ListVerbs(FileMeta context);

        /// <summary>
        /// Lists all extensions for a context
        /// </summary>
        /// <param name="contextMeta">Context whose extensions are listed</param>
        /// <returns>Dictionary of extensions grouped by extension type</returns>
        protected virtual IDictionary<string, List<FileMeta>> ListAllExtensions(FileMeta contextMeta) {
            var allExtensions = new Dictionary<string, List<FileMeta>>();
            foreach (var extType in ListExtensionTypes(contextMeta)) {
                var typeExtensions = ListExtensions(extType);
                allExtensions.Add(extType.Name, typeExtensions);
            }
            return allExtensions;
        }

        /// <summary>
        /// Combines two paths into one
        /// </summary>
        /// <param name="paths">Paths to combine</param>
        /// <returns>Combined Path</returns>
        protected abstract string CombinePaths(params string[] paths);
        
        /// <summary>
        /// Writes CodeFiles files of a given language
        /// </summary>
        /// <param name="dest">Destination to write files to</param>
        /// <param name="coreFiles">Core files to write</param>
        /// <param name="files">Files to write</param>
        /// <param name="definitionsDirName">Name of definitions directory</param>
        /// <param name="coreDirName">Name of core directory</param>
        public virtual void WriteCodeFiles(string dest, List<FileMeta> coreFiles, string coreDirName,
            List<FileMeta> files, string definitionsDirName) {
            WriteFiles(CombinePaths(dest, coreDirName), coreFiles);
            WriteFiles(CombinePaths(dest, definitionsDirName), files);
        }

        /// <summary>
        /// Asynchronously writes CodeFiles files of a given language
        /// </summary>
        /// <param name="dest">Destination to write files to</param>
        /// <param name="coreFiles">Core files to write</param>
        /// <param name="files">Definitions files to write</param>
        /// <param name="definitionsDirName">Name of definitions directory</param>
        /// <param name="coreDirName">Name of core directory</param>
        public virtual async Task WriteCodeFilesAsync(string dest, List<FileMeta> coreFiles, string coreDirName,
            List<FileMeta> files, string definitionsDirName) {
            var taskCore = WriteFilesAsync(CombinePaths(dest, coreDirName), coreFiles);
            var taskDefs = WriteFilesAsync(CombinePaths(dest, definitionsDirName), files);
            await Task.WhenAll(taskCore, taskDefs);
        }

        /// <summary>
        /// Writes files
        /// </summary>
        /// <param name="dest">Destination to write to</param>
        /// <param name="files">Files to write</param>
        protected abstract void WriteFiles(string dest, List<FileMeta> files);

        /// <summary>
        /// Asynchronously writes files
        /// </summary>
        /// <param name="dest">Destination to write to</param>
        /// <param name="files">Files to write</param>
        protected abstract Task WriteFilesAsync(string dest, List<FileMeta> files);
    }
}