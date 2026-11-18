/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Parser.IO;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Updates a project's registry the way the editor does: reads the types config next to the
    /// source folder, turns it into <see cref="GenerationOptions"/> and runs the generator.
    /// The menu items, the wizard and the file watcher share this, so all of them apply the config.
    /// </summary>
    internal sealed class RegistryUpdate
    {
        private readonly RegistryGenerator _generator;
        private readonly ITypeNameResolver _resolver;
        private readonly IGeneratorLog _log;

        public RegistryUpdate(RegistryGenerator generator, ITypeNameResolver resolver, IGeneratorLog log)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Reads the types config. Without one, the registry uses default types.
        /// </summary>
        /// <returns>The config, or null (with a warning) if there is none.</returns>
        public ConfigFile ReadConfig(RegistryPaths paths)
        {
            if (paths.ConfigFile == null || !File.Exists(paths.ConfigFile))
            {
                _log.Warning($"[xAPI4Unity] Cannot find '{paths.ConfigFile}'. Default types will be applied.");
                return null;
            }

            return ConfigFile.Read(paths.ConfigFile);
        }

        /// <summary>
        /// Parses the types of a config, with its variables expanded.
        /// </summary>
        /// <returns>The schema, or null without a config.</returns>
        public TypeSchema ReadTypes(ConfigFile config) =>
            config == null
                ? null
                : TypeSchema.Parse(config.Types ?? new Dictionary<string, string>(), validateResolution: true,
                    _resolver, config.Variables, strictVariables: true);

        /// <summary>
        /// Turns a config into generation options. A missing config gives default options.
        /// </summary>
        public GenerationOptions CreateOptions(string @namespace, ConfigFile config) =>
            new GenerationOptions(@namespace)
            {
                AsmDefReferences = config?.references ?? Array.Empty<string>(),
                IgnoredContexts = config?.ignoredContexts ?? Array.Empty<string>(),
                TypeSchema = ReadTypes(config)
            };

        /// <summary>
        /// Generates the registry from the source folder and hands it to the writer.
        /// </summary>
        /// <returns>False (with an error) if the source folder has no <c>definitions</c> folder.</returns>
        public bool Run(RegistryPaths paths, IRegistryWriter writer)
        {
            if (paths.DefinitionsFolder == null || !Directory.Exists(paths.DefinitionsFolder))
            {
                _log.Error($"[xAPI4Unity] Cannot transform JSON files because '{paths.DefinitionsFolder}' doesn't exist.");
                return false;
            }

            var options = CreateOptions(paths.Namespace, ReadConfig(paths));
            if (options.TypeSchema != null)
                _log.Info($"[xAPI4Unity] Parsed Types Schema for '{options.TypeSchema.Count}' definitions..");

            _generator.Generate(options, new DirectoryDefinitionSource(paths.DefinitionsFolder), writer);
            return true;
        }
    }
}
#endif
