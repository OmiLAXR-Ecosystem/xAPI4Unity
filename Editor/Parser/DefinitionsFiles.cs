/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.Code.CSharp;
using xAPI4Unity.Editor.Parser.Definitions;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Responsible for managing and updating definition-related files in the xAPI4Unity project.
    /// </summary>
    internal static class DefinitionsFiles
    {
        /// <summary>
        /// List of predefined extension types supported by xAPI4Unity.
        /// </summary>
        private static readonly string[] ExtensionTypes = { "activity", "context", "result" };

        /// <summary>
        /// Updates the definition-related files by generating new classes and context files from the provided definitions.
        /// </summary>
        /// <param name="namespace">The target namespace for the generated files.</param>
        /// <param name="definitions">A collection of DefinitionFile objects to process.</param>
        /// <param name="destPath">The directory path where the generated files should be saved.</param>
        /// <param name="typeSchema">Optional schema for resolving types within the definitions.</param>
        public static void UpdateFiles(string @namespace,
            IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitions, string destPath, TypeSchema typeSchema = null)
        {
            // Parse the provided definitions into xAPIDefinitions
            var xAPIDefinitions = new xAPI_Definitions(definitions);

            // Initialize the C# generator with the specified namespace
            var generator = new CSharpGenerator(@namespace);

            // Generate and save extension type files for each defined extension type
            foreach (var extensionType in ExtensionTypes)
            {
                var extensionTypeFile = generator.GenerateExtensionTypeFile(extensionType);
                extensionTypeFile.Save(destPath);
            }

            // Iterate through all contexts parsed from the definitions
            foreach (var ctx in xAPIDefinitions.Contexts.Values)
            {
                // Generate and save the context extensions file
                var contextExtensions = generator.GenerateContextExtensionsFile(ctx);
                contextExtensions.Save(destPath);

                // Generate and save extension files for each extension type
                foreach (var extensionType in ExtensionTypes)
                {
                    var extFiles = generator.GenerateExtensionsFiles(ctx, extensionType, typeSchema).ToList();
                    extFiles.ForEach(e => e.Save(destPath));
                }

                // Generate and save verbs files
                var verbs = generator.GenerateVerbsFiles(ctx).ToList();
                verbs.ForEach(fm => fm.Save(destPath));

                // Generate and save activities files
                var activities = generator.GenerateActivitiesFiles(ctx).ToList();
                activities.ForEach(fm => fm.Save(destPath));

                // Generate and save generic extensions files
                var extensions = generator.GenerateExtensions(ctx, typeSchema).ToList();
                extensions.ForEach(fm => fm.Save(destPath));

                // Generate and save the main context file
                var ctxFile = generator.GenerateContextFile(ctx);
                ctxFile.Save(destPath);
            }

            // Generate and save the xAPI_Definitions.cs file containing all contexts
            var definitionsFile =
                generator.GenerateDefinitionsFile(xAPIDefinitions.Contexts.Values.Select(c => new xAPIContext(c.Name)));
            definitionsFile.Save(destPath);

            // Generate and save the xAPI_Contexts.cs file for managing contexts
            var contextsFile =
                generator.GenerateContextsFile(xAPIDefinitions.Contexts.Values.Select(c => new xAPIContext(c.Name)));
            contextsFile.Save(destPath);
        }
    }
}
#endif