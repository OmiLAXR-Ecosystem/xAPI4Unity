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
using xAPI4Unity.Editor.Parser.IO;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Builds the C# files generated from the definitions (verbs, activities, extensions, contexts).
    /// </summary>
    internal static class DefinitionsFiles
    {
        /// <summary>
        /// List of predefined extension types supported by xAPI4Unity.
        /// </summary>
        private static readonly string[] ExtensionTypes = { "activity", "context", "result" };

        /// <summary>
        /// Generates the definition classes in memory.
        /// </summary>
        /// <param name="namespace">The target namespace for the generated files.</param>
        /// <param name="definitions">The definitions to generate from.</param>
        /// <param name="typeSchema">Optional schema for resolving types within the definitions.</param>
        /// <param name="log">Receives warnings and details. Defaults to the Unity console.</param>
        /// <returns>The files, each with its file name and C# content.</returns>
        public static IReadOnlyList<FileMeta> Create(string @namespace,
            IEnumerable<IO.DefinitionFile> definitions, TypeSchema typeSchema = null, IGeneratorLog log = null)
        {
            log = log ?? UnityGeneratorLog.Instance;

            // Parse the provided definitions into xAPIDefinitions
            var xAPIDefinitions = new xAPI_Definitions(definitions, log);

            // Initialize the C# generator with the specified namespace
            var generator = new CSharpGenerator(@namespace, log);
            var files = new List<FileMeta>();

            // Extension type base classes (xAPI_Extensions_Activity, …)
            files.AddRange(ExtensionTypes.Select(generator.GenerateExtensionTypeFile));

            foreach (var ctx in xAPIDefinitions.Contexts.Values)
            {
                files.Add(generator.GenerateContextExtensionsFile(ctx));

                foreach (var extensionType in ExtensionTypes)
                    files.AddRange(generator.GenerateExtensionsFiles(ctx, extensionType, typeSchema));

                files.AddRange(generator.GenerateVerbsFiles(ctx));
                files.AddRange(generator.GenerateActivitiesFiles(ctx));
                files.Add(generator.GenerateContextFile(ctx));
            }

            // xAPI_Definitions.cs (static) and xAPI_Contexts.cs list all contexts
            var contexts = xAPIDefinitions.Contexts.Values.Select(c => new xAPIContext(c.Name)).ToList();
            files.Add(generator.GenerateDefinitionsFile(contexts));
            files.Add(generator.GenerateContextsFile(contexts));

            return files;
        }
    }
}
#endif
