using System;
using System.IO;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Class that provides easy to use access to code generation functionality
    /// </summary>
    public static class Generator
    {
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <param name="namespace"></param>
        private static void CreateUnityAsmDefFile(string output, string @namespace)
        {
            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);
            File.WriteAllText(
                Path.Combine(output, @namespace + ".asmdef"), 
                GetUnityAsmDefContent(@namespace, @namespace));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="rootNamespace"></param>
        /// <returns></returns>
        private static string GetUnityAsmDefContent(string name, string rootNamespace)
        {
            return "{\r\n" +
                   $"\t\"name\": \"{name}\",\r\n" +
                   $"\t\"rootNamespace\": \"{rootNamespace}\",\r\n" +
                   "\t\"references\": [],\r\n" +
                   "\t\"includePlatforms\": [],\r\n" +
                   "\t\"excludePlatforms\": [],\r\n" +
                   "\t\"allowUnsafeCode\": false,\r\n" +
                   "\t\"overrideReferences\": false,\r\n" +
                   "\t\"precompiledReferences\": [],\r\n" +
                   "\t\"autoReferenced\": true,\r\n" +
                   "\t\"defineConstraints\": [],\r\n" +
                   "\t\"versionDefines\": [],\r\n" +
                   "\t\"noEngineReferences\": false\r\n" +
                   "}";
        }

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
    }
}