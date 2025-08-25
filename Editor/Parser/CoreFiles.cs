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

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Handles operations related to the core files of xAPI4Unity, such as copying templates and creating .asmdef files.
    /// </summary>
    internal static class CoreFiles
    {
        /// <summary>
        /// List of core file names used in xAPI4Unity.
        /// </summary>
        private static readonly string[] Files = new[]
        {
            "xAPI_Activities.cs", "xAPI_Activity.cs", "xAPI_Actor.cs", "xAPI_Body.cs",
            "xAPI_Context.cs", "xAPI_Definition.cs", "xAPI_Extension.cs", "xAPI_Extensions.cs",
            "xAPI_Verb.cs", "xAPI_Verbs.cs"
        };

        // Relative paths within the package structure
        const string RelativePackagePath = "Packages/com.rwth.unity.omilaxr.xapi4unity";
        private static string RelativeEditorPath => Path.Combine(RelativePackagePath, "Editor");
        private static string RelativeAssetPath => Path.Combine(RelativeEditorPath, "Assets");

        /// <summary>
        /// Path to the core directory containing the template .cs files.
        /// </summary>
        private static string CorePath => Path.GetFullPath(Path.Combine(RelativeAssetPath, "Core"));

        /// <summary>
        /// Copies and processes core template files to the specified destination path, setting the appropriate namespace.
        /// </summary>
        /// <param name="namespace">The namespace to replace in the template files.</param>
        /// <param name="destination">The destination directory to copy the files to.</param>
        public static void CopyFiles(string @namespace, string destination)
        {
            foreach (var f in Files)
            {
                var fullPath = Path.Combine(CorePath, f + ".tmpl");
                var targetPath = Path.Combine(destination, f);
                var content = File.ReadAllText(fullPath);

                // Replace the placeholder namespace in the template content with the provided namespace.
                content = content.Replace("xAPI.Registry", @namespace);

                File.WriteAllText(targetPath, content);
            }
        }

        /// <summary>
        /// Creates an Assembly Definition (.asmdef) file at the specified destination.
        /// </summary>
        /// <param name="namespace">The namespace to associate with the assembly.</param>
        /// <param name="destination">The destination directory for the .asmdef file.</param>
        /// <param name="asmDefReferences">Optional assembly references to include in the .asmdef file.</param>
        public static void CreateAsmDefFile(string @namespace, string destination, IEnumerable<string> asmDefReferences = null)
        {
            var path = Path.Combine(destination, @namespace + ".asmdef");
            var metaPath = Path.Combine(destination, @namespace + ".asmdef.meta");
            
            if (asmDefReferences == null)
                asmDefReferences = Array.Empty<string>();
            var newAsmDefReferences = asmDefReferences.ToList();

            // Only create the .asmdef file if it doesn't already exist.
            if (!File.Exists(path))
                File.WriteAllText(path, AsmDefFile(@namespace, @namespace, newAsmDefReferences.ToArray()));
            
            // Only create the .asmdef.meta file if it doesn't already exist.
            if (!File.Exists(metaPath))
                File.WriteAllText(metaPath, AsmDefFileMeta);
        }

        /// <summary>
        /// Generates the content of an Assembly Definition (.asmdef) file as a JSON-formatted string.
        /// </summary>
        /// <param name="name">The name of the assembly.</param>
        /// <param name="rootNamespace">The root namespace of the assembly.</param>
        /// <param name="references">References to include in the assembly.</param>
        /// <returns>A JSON-formatted string representing the .asmdef file content.</returns>
        private static string AsmDefFile(string name, string rootNamespace, string[] references) =>
            "{\n  \"name\": \""+ name +"\",\n" +
                          "  \"rootNamespace\": \""+ rootNamespace + "\",\n" +
                          "  \"references\": [\n"+ string.Join(",\n", references.Select(r => "\""+ r + "\"")) +"],\n" +
                          "  \"includePlatforms\": [],\n" +
                          "  \"excludePlatforms\": [],\n" +
                          "  \"allowUnsafeCode\": false,\n" +
                          "  \"overrideReferences\": false,\n" +
                          "  \"precompiledReferences\": [],\n" +
                          "  \"autoReferenced\": true,\n" +
                          "  \"defineConstraints\": [],\n" +
                          "  \"versionDefines\": [],\n" +
                          "  \"noEngineReferences\": false\n}"
            ;

        /// <summary>
        /// Predefined meta file content for .asmdef files.
        /// </summary>
        const string AsmDefFileMeta = "fileFormatVersion: 2\nguid: 5b80f9e8c84ce594bbd1284cff4292dd\nAssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n\n";
    }
}
#endif