/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using xAPI4Unity.Editor.Parser.IO;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Class that provides easy-to-use access to code generation functionality
    /// </summary>
    internal static class Generator
    {
        /// <summary>
        /// Checks if a folder exists at the provided path,
        /// and if not, creates a new directory.
        /// </summary>
        /// <param name="path">Path to the folder.</param>
        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        /// <summary>
        /// Detects the paths for the root, core, and definitions folders based on a provided namespace.
        /// </summary>
        /// <param name="namespace">Namespace for detecting paths.</param>
        private static (string, string, string) DetectPaths(string @namespace)
        {
            var rootFolder = Path.Combine(Application.dataPath, @namespace);
            var coreFolder = Path.Combine(rootFolder, "Core");
            var definitionsFolder = Path.Combine(rootFolder, "Definitions");
            return (rootFolder, coreFolder, definitionsFolder);
        }
        /// <summary>
        /// Ensures the folders for the root, core, and definitions exist.
        /// </summary>
        /// <param name="rootFolder">Path to the root folder.</param>
        /// <param name="coreFolder">Path to the core folder.</param>
        /// <param name="definitionsFolder">Path to the definitions' folder.</param>
        private static void EnsureFolders(string rootFolder, string coreFolder, string definitionsFolder)
        {
            EnsureFolder(rootFolder);
            EnsureFolder(coreFolder);
            EnsureFolder(definitionsFolder);
        }

        /// <summary>
        /// Generates the necessary files and folders based on the provided parameters.
        /// </summary>
        /// <param name="namespace">Namespace for the folders and files.</param>
        /// <param name="definitionsPath">Path to the definitions.</param>
        /// <param name="asmDefReferences"> (optional) Assembly definition references.</param>
        /// <param name="typeSchema"> (optional) Type schema.</param>
        /// <param name="ignoredContexts">(optional) Ignored list of contexts.</param>
        public static void Generate(string @namespace, string definitionsPath, string[] asmDefReferences = null, TypeSchema typeSchema = null, string[] ignoredContexts = null)
        {
            
            // Instantiate a new file manager with the provided definitions path
            var fm = new FileManager(definitionsPath);
            // Get all file entries from the definition path
            var definitionsFiles = fm.GetAllEntries().ToArray();
            
            if (ignoredContexts != null)
                definitionsFiles = definitionsFiles.Where(f => !ignoredContexts.Contains(f.Context)).ToArray();

            // Log the number of xAPI definitions found
            Debug.Log($"[xAPI4Unity]: Found {definitionsFiles.Length} xAPI definitions");

            // Detect the paths for the root, core, and definitions folders
            var (rootFolder, coreFolder, definitionsFolder) = DetectPaths(@namespace);
    
            // If the root folder exists, it is deleted and recreated.
            // This ensures that we're starting with a clean slate for the code generation process
            if (Directory.Exists(rootFolder))
                Directory.Delete(rootFolder, recursive: true);
    
            // Ensures the folders for the root, core, and definitions exist
            EnsureFolders(rootFolder, coreFolder, definitionsFolder);
    
            // Copies the core folder from the source code to the destination folder
            CoreFiles.CopyFiles(@namespace, coreFolder);
    
            // Update the definition files with the content from source files
            // and save them to the destination folder
            DefinitionsFiles.UpdateFiles(@namespace, definitionsFiles, definitionsFolder, typeSchema);

            // Create an Assembly Definition file in the root folder.
            // Assembly Definitions allow you to compile a script into an assembly (.dll),
            // improving compilation times.
            CoreFiles.CreateAsmDefFile(@namespace, rootFolder, asmDefReferences);
        
            // Log a completion message indicating that the xAPI Registry has been created
            Debug.Log("[xAPI4Unity]: Created xAPI Registry with namespace " + @namespace);
        }
    }
}
#endif