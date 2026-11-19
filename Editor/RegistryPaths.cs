/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using xAPI4Unity.Editor.Settings;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Where the definitions, the types config and the generated registry of a project are.
    /// The wizard, the menu items and the file watcher all use these rules.
    /// </summary>
    internal sealed class RegistryPaths
    {
        /// <summary>
        /// Creates the paths of a project.
        /// </summary>
        /// <param name="sourceFolder">Folder that holds the <c>definitions</c> folder (the local source path). May be empty.</param>
        /// <param name="assetsFolder">The project's <c>Assets</c> folder.</param>
        /// <param name="namespace">Namespace of the registry, also the name of its folder in <c>Assets</c>.</param>
        public RegistryPaths(string sourceFolder, string assetsFolder, string @namespace)
        {
            SourceFolder = string.IsNullOrWhiteSpace(sourceFolder) ? null : sourceFolder;
            AssetsFolder = assetsFolder;
            Namespace = @namespace;
        }

        /// <summary>
        /// The paths of the current project, from the saved settings.
        /// </summary>
        public static RegistryPaths FromSettings(MainSettings settings) =>
            new RegistryPaths(settings.localSource.path, Application.dataPath, settings.@namespace);

        /// <summary>
        /// Folder that holds the <c>definitions</c> folder, or null if none is set.
        /// </summary>
        public string SourceFolder { get; }

        /// <summary>
        /// The project's <c>Assets</c> folder.
        /// </summary>
        public string AssetsFolder { get; }

        /// <summary>
        /// Namespace of the registry.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// <c>&lt;source&gt;/definitions</c>, or null without a source folder.
        /// </summary>
        public string DefinitionsFolder => SourceFolder == null ? null : Path.Combine(SourceFolder, "definitions");

        /// <summary>
        /// The types config <c>&lt;source folder&gt;.config.json</c> next to the source folder,
        /// such as <c>Assets/xapi.config.json</c> for <c>Assets/xapi</c>. Null without a source folder.
        /// </summary>
        public string ConfigFile => SourceFolder == null ? null : SourceFolder.TrimEnd('/', '\\') + ".config.json";

        /// <summary>
        /// <c>Assets/&lt;namespace&gt;</c>, where the registry is generated.
        /// </summary>
        public string RegistryFolder => Path.Combine(AssetsFolder, Namespace);
    }
}
#endif
