using System;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR 

namespace xAPI4Unity.Editor
{
    internal class UnityPackageExporter
    {
        private static string GetRelativePath(string path)
        {
            var assetsPathIndex = path.IndexOf("/Assets", StringComparison.Ordinal);
            return path.Substring(assetsPathIndex);
        }

        private static string GetParentPath(string path)
        {
            var s = path.Replace("\\", "/").Split('/');
            return string.Join("/", s, 0, s.Length - 1);
        }
        
        public static void ExportAsPackage(string path, string name)
            => ExportAsPackage(new []{path}, name);
        public static void ExportAsPackage(string[] paths, string name)
        {
            AssetDatabase.ExportPackage(paths, name, ExportPackageOptions.Recurse);
            var targetPath = Path.Combine(GetParentPath(Application.dataPath), name);
            Debug.Log($"Exported into '{targetPath}'.");
        }
        
        [MenuItem("xAPI4Unity / Export .unitypackage / Only 'xapi' folder", priority = 200)]
        private static void ExportXApiFolder()
        {
            var path = GetRelativePath(Settings.Instance.localSource.path);
            ExportAsPackage(path, "xapi.unitypackage");
        }
        
        [MenuItem("xAPI4Unity / Export .unitypackage / Only 'xAPI.Registry' folder", priority = 200)]
        private static void ExportXApiRegistryFolder()
        {
            ExportAsPackage($"Assets/{Settings.Instance.@namespace}", "xAPIRegistry.unitypackage");
        }
        
        [MenuItem("xAPI4Unity / Export .unitypackage / 'xapi' and 'xAPI.Registry' folder together", priority = 200)]
        private static void ExportBoth()
        {
            ExportAsPackage(new []
            {
                Settings.Instance.localSource.path,
                $"Assets/{Settings.Instance.@namespace}"
            }, "xAPIRegistry.unitypackage");

        }
    }
}

#endif