/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Turns xAPI definitions into the files of an xAPI registry.
    /// It only talks to its collaborators through interfaces, so it never touches the disk
    /// or the Unity console by itself. <see cref="Generator"/> wires it up for the editor.
    /// </summary>
    internal sealed class RegistryGenerator
    {
        /// <summary>
        /// Folder inside the registry that holds the static core classes.
        /// </summary>
        public const string CoreFolder = "Core";

        /// <summary>
        /// Folder inside the registry that holds the classes generated from definitions.
        /// </summary>
        public const string DefinitionsFolder = "Definitions";

        private readonly ICoreTemplateSource _templates;
        private readonly IGeneratorLog _log;

        /// <param name="templates">Source of the core class templates.</param>
        /// <param name="log">Receives progress messages and warnings.</param>
        public RegistryGenerator(ICoreTemplateSource templates, IGeneratorLog log)
        {
            _templates = templates ?? throw new ArgumentNullException(nameof(templates));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Builds all registry files in memory.
        /// </summary>
        /// <param name="options">Namespace, assembly references, types and ignored contexts.</param>
        /// <param name="definitions">The definitions to generate from.</param>
        /// <returns>The registry: core classes, definition classes and the assembly definition.</returns>
        public GeneratedRegistry Build(GenerationOptions options, IEnumerable<IO.DefinitionFile> definitions)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            var ignored = new HashSet<string>(options.IgnoredContexts ?? Array.Empty<string>(), StringComparer.Ordinal);
            var included = definitions.Where(d => !ignored.Contains(d.Context)).ToArray();
            _log.Info($"[xAPI4Unity]: Found {included.Length} xAPI definitions");

            var registry = new GeneratedRegistry(options.Namespace) { DefinitionCount = included.Length };

            foreach (var file in CoreFiles.Create(options.Namespace, _templates))
                registry.Add(CoreFolder + "/" + file.Name, file.FileContent);

            foreach (var file in DefinitionsFiles.Create(options.Namespace, included, options.TypeSchema, _log))
                registry.Add(DefinitionsFolder + "/" + file.Name, file.FileContent);

            registry.Add(options.Namespace + ".asmdef", CoreFiles.CreateAsmDef(options.Namespace, options.AsmDefReferences));
            registry.Add(options.Namespace + ".asmdef.meta", CoreFiles.AsmDefMeta);

            return registry;
        }

        /// <summary>
        /// Reads the definitions, builds the registry and writes it.
        /// </summary>
        /// <returns>The registry that was written.</returns>
        public GeneratedRegistry Generate(GenerationOptions options, IDefinitionSource source, IRegistryWriter writer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            var registry = Build(options, source.ReadDefinitions());
            writer.Write(registry);
            _log.Info("[xAPI4Unity]: Created xAPI Registry with namespace " + options.Namespace);
            return registry;
        }
    }
}
#endif
