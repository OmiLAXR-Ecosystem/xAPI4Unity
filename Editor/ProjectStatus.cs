/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// What is set up in a project: definitions, registry, types config, merge reports.
    /// The wizard shows these checks; <see cref="Startup"/> opens the wizard when one is missing.
    /// </summary>
    internal sealed class ProjectStatus
    {
        public ProjectStatus(RegistryPaths paths)
        {
            Paths = paths;
        }

        public RegistryPaths Paths { get; }

        /// <summary>
        /// The source folder has a <c>definitions</c> folder.
        /// </summary>
        public bool HasDefinitions => Paths.DefinitionsFolder != null && Directory.Exists(Paths.DefinitionsFolder);

        /// <summary>
        /// The registry folder exists in <c>Assets</c>.
        /// </summary>
        public bool HasRegistry => Directory.Exists(Paths.RegistryFolder);

        /// <summary>
        /// The types config exists next to the source folder.
        /// </summary>
        public bool HasConfig => Paths.ConfigFile != null && File.Exists(Paths.ConfigFile);

        /// <summary>
        /// The source folder holds merge reports (<c>*-xapi-report.json</c>) of earlier patches.
        /// </summary>
        public bool HasMergeReports => Paths.SourceFolder != null && Directory.Exists(Paths.SourceFolder) &&
                                       Directory.GetFiles(Paths.SourceFolder, "*-xapi-report.json", SearchOption.TopDirectoryOnly).Length > 0;

        /// <summary>
        /// Number of C# files in the registry, 0 if it does not exist.
        /// </summary>
        public int RegistryFileCount => HasRegistry
            ? Directory.GetFiles(Paths.RegistryFolder, "*.cs", SearchOption.AllDirectories).Length
            : 0;
    }
}
#endif
