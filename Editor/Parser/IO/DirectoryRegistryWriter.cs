/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;

namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Writes a registry into a folder. The folder is deleted first, so files of
    /// definitions that no longer exist do not stay behind.
    /// </summary>
    internal sealed class DirectoryRegistryWriter : IRegistryWriter
    {
        /// <param name="rootFolder">Registry folder, such as <c>Assets/xAPI.Registry</c>.</param>
        public DirectoryRegistryWriter(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                throw new ArgumentException("The registry folder must not be empty.", nameof(rootFolder));
            RootFolder = rootFolder;
        }

        /// <summary>
        /// Registry folder the files are written into.
        /// </summary>
        public string RootFolder { get; }

        /// <inheritdoc/>
        public void Write(GeneratedRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            if (Directory.Exists(RootFolder))
                Directory.Delete(RootFolder, recursive: true);
            Directory.CreateDirectory(RootFolder);

            foreach (var file in registry.Files)
            {
                var path = Path.Combine(RootFolder, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);
                File.WriteAllText(path, file.Content);
            }
        }
    }
}
#endif
