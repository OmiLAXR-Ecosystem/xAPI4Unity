/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using xAPI4Unity.Editor.Parser.IO;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Entry point of the editor: generates <c>Assets/&lt;namespace&gt;</c> from a definitions folder.
    /// It only wires the disk and console implementations into <see cref="RegistryGenerator"/>.
    /// </summary>
    internal static class Generator
    {
        /// <summary>
        /// Generates the registry into <c>Assets/&lt;namespace&gt;</c> and replaces an existing one.
        /// </summary>
        /// <param name="namespace">Namespace for the folders and files.</param>
        /// <param name="definitionsPath">Path to the definitions.</param>
        /// <param name="asmDefReferences"> (optional) Assembly definition references.</param>
        /// <param name="typeSchema"> (optional) Type schema.</param>
        /// <param name="ignoredContexts">(optional) Ignored list of contexts.</param>
        public static void Generate(string @namespace, string definitionsPath, string[] asmDefReferences = null, TypeSchema typeSchema = null, string[] ignoredContexts = null)
        {
            var options = new GenerationOptions(@namespace)
            {
                AsmDefReferences = asmDefReferences ?? Array.Empty<string>(),
                TypeSchema = typeSchema,
                IgnoredContexts = ignoredContexts ?? Array.Empty<string>()
            };

            var generator = new RegistryGenerator(DirectoryCoreTemplateSource.ForPackage(), UnityGeneratorLog.Instance);
            generator.Generate(
                options,
                new DirectoryDefinitionSource(definitionsPath),
                new DirectoryRegistryWriter(Path.Combine(Application.dataPath, @namespace)));
        }

        /// <summary>
        /// Creates the editor's <see cref="RegistryUpdate"/>: package templates, Unity console, built-in aliases.
        /// </summary>
        internal static RegistryUpdate CreateUpdate(ITypeNameResolver resolver = null) =>
            new RegistryUpdate(
                new RegistryGenerator(DirectoryCoreTemplateSource.ForPackage(), UnityGeneratorLog.Instance),
                resolver ?? new TypeNameResolver(new Aliases()),
                UnityGeneratorLog.Instance);

        /// <summary>
        /// Generates <c>Assets/&lt;namespace&gt;</c> from the project's source folder and its types config.
        /// </summary>
        /// <returns>False if the source folder has no <c>definitions</c> folder.</returns>
        internal static bool Update(RegistryPaths paths) =>
            CreateUpdate().Run(paths, new DirectoryRegistryWriter(paths.RegistryFolder));
    }
}
#endif
