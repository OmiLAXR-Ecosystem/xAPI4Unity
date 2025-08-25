/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Settings;

#if UNITY_EDITOR 

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Handles exporting Unity assets and folders as .unitypackage files for xAPI4Unity.
    /// </summary>
    internal class UnityPackageExporter
    {
        /// <summary>
        /// Gets the relative path within the Unity "Assets/..." directory for a given absolute path.
        /// </summary>
        /// <param name="path">The absolute path to convert.</param>
        /// <returns>The path relative to the Unity "Assets" folder.</returns>
        private static string GetRelativePath(string path)
        {
            var assetsPathIndex = path.IndexOf("/Assets", StringComparison.Ordinal);
            return path.Substring(assetsPathIndex);
        }

        /// <summary>
        /// Retrieves the parent directory of the given path.
        /// </summary>
        /// <param name="path">The original path.</param>
        /// <returns>The parent directory path.</returns>
        private static string GetParentPath(string path)
        {
            var s = path.Replace("\\", "/").Split('/');
            return string.Join("/", s, 0, s.Length - 1);
        }
        
        /// <summary>
        /// Exports a single path as a .unitypackage file.
        /// </summary>
        /// <param name="path">The path to export.</param>
        /// <param name="name">The name of the output .unitypackage file.</param>
        public static void ExportAsPackage(string path, string name)
            => ExportAsPackage(new []{path}, name);

        /// <summary>
        /// Exports multiple paths as a single .unitypackage file.
        /// </summary>
        /// <param name="paths">The array of paths to include in the package.</param>
        /// <param name="name">The name of the output .unitypackage file.</param>
        public static void ExportAsPackage(string[] paths, string name)
        {
            AssetDatabase.ExportPackage(paths, name, ExportPackageOptions.Recurse);

            // Determine the target path for the created package
            var targetPath = Path.Combine(GetParentPath(Application.dataPath), name);
            Debug.Log($"Exported into '{targetPath}'.");
        }
        
        /// <summary>
        /// Menu option to export only the "xapi" folder as a .unitypackage.
        /// </summary>
        [MenuItem("xAPI4Unity / Export .unitypackage / Only 'xapi' folder", priority = 200)]
        private static void ExportXApiFolder()
        {
            var path = GetRelativePath(MainSettings.Instance.localSource.path);
            ExportAsPackage(path, "xapi.unitypackage");
        }
        
        /// <summary>
        /// Menu option to export only the "xAPI.Registry" folder as a .unitypackage.
        /// </summary>
        [MenuItem("xAPI4Unity / Export .unitypackage / Only 'xAPI.Registry' folder", priority = 200)]
        private static void ExportXApiRegistryFolder()
        {
            ExportAsPackage($"Assets/{MainSettings.Instance.@namespace}", "xAPIRegistry.unitypackage");
        }
        
        /// <summary>
        /// Menu option to export both the "xapi" and "xAPI.Registry" folders as a single .unitypackage.
        /// </summary>
        [MenuItem("xAPI4Unity / Export .unitypackage / 'xapi' and 'xAPI.Registry' folder together", priority = 200)]
        private static void ExportBoth()
        {
            ExportAsPackage(new []
            {
                MainSettings.Instance.localSource.path,
                $"Assets/{MainSettings.Instance.@namespace}"
            }, "xAPIRegistry.unitypackage");
        }
    }
}

#endif