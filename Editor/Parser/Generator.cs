#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Class that provides easy to use access to code generation functionality
    /// </summary>
    internal static class Generator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ResolvedPath(this string path)
            => path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));

        public static string EnsuredPath(this string path)
        {
            path = path.Replace("\\\\", "/");
            var dirs = path.Split('/');
            var dirPath = "";
            for (var i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                dirPath += dir;
                if (i < dirs.Length - 1)
                    dirPath += '/';
                if (i < 1)
                    continue;
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
            }

            return path;
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static (string, string, string) DetectPaths(string @namespace)
        {
            var rootFolder = Path.Combine(Application.dataPath, @namespace);
            var coreFolder = Path.Combine(rootFolder, "Core");
            var definitionsFolder = Path.Combine(rootFolder, "Definitions");
            return (rootFolder, coreFolder, definitionsFolder);
        }
        
        private static void EnsureFolders(string rootFolder, string coreFolder, string definitionsFolder)
        {
            EnsureFolder(rootFolder);
            EnsureFolder(coreFolder);
            EnsureFolder(definitionsFolder);
        }

        public static void Generate(string @namespace, string definitionsPath)
        {
            var fm = new FileManager(definitionsPath);
            var definitionsFiles = fm.GetAllEntries();

            var (rootFolder, coreFolder, definitionsFolder) = DetectPaths(@namespace);
            
            if (Directory.Exists(rootFolder))
                Directory.Delete(rootFolder, recursive: true);
            
            EnsureFolders(rootFolder, coreFolder, definitionsFolder);
            
            CoreFiles.CopyFiles(@namespace, coreFolder);
            DefinitionsFiles.UpdateFiles(@namespace, definitionsFiles, definitionsFolder);
            CoreFiles.CreateAsmDefFile(@namespace, rootFolder);
                
            Debug.Log("[xAPI 4 Unity]: Created xAPI Registry with namespace " + @namespace);
        }
    }
}
#endif