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

namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Reads the <c>*.json</c> definitions below a <c>definitions/</c> folder on disk.
    /// </summary>
    internal sealed class DirectoryDefinitionSource : IDefinitionSource
    {
        private readonly string _rootPath;

        /// <param name="rootPath">The <c>definitions</c> folder, which holds one folder per context.</param>
        public DirectoryDefinitionSource(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("The definitions path must not be empty.", nameof(rootPath));
            _rootPath = rootPath;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Files are returned in ordinal path order, so a generation run does not depend on
        /// the order the file system lists them in. Files whose name starts with <c>.</c> are skipped.
        /// </remarks>
        /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
        public IReadOnlyList<DefinitionFile> ReadDefinitions()
        {
            if (!Directory.Exists(_rootPath))
                throw new DirectoryNotFoundException("Cannot find directory: " + _rootPath);

            var root = Path.GetFullPath(_rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                .Select(path => path.Substring(root.Length + 1))
                .OrderBy(relative => relative.Replace('\\', '/'), StringComparer.Ordinal)
                .Select(relative => new DefinitionFile(relative, ReadShared(Path.Combine(root, relative))))
                .ToList();
        }

        /// <summary>
        /// Reads a file even while another process (an editor, git) holds it open.
        /// </summary>
        private static string ReadShared(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
#endif
