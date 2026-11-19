/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// One generated file, addressed by its path inside the registry folder.
    /// </summary>
    internal sealed class GeneratedFile
    {
        /// <summary>
        /// Creates a file with its path inside the registry folder and its text.
        /// </summary>
        public GeneratedFile(string relativePath, string content)
        {
            RelativePath = relativePath;
            Content = content;
        }

        /// <summary>
        /// Path inside the registry folder, with <c>/</c> as separator (e.g. <c>Core/xAPI_Verb.cs</c>).
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// Text of the file.
        /// </summary>
        public string Content { get; }
    }

    /// <summary>
    /// The complete output of a generation run, held in memory.
    /// <see cref="IRegistryWriter"/> implementations decide where it goes.
    /// </summary>
    internal sealed class GeneratedRegistry
    {
        /// <summary>
        /// Paths in the order they were first added.
        /// </summary>
        private readonly List<string> _order = new List<string>();
        /// <summary>
        /// Files by path.
        /// </summary>
        private readonly Dictionary<string, GeneratedFile> _files =
            new Dictionary<string, GeneratedFile>(StringComparer.Ordinal);

        /// <summary>
        /// Creates an empty registry for a namespace.
        /// </summary>
        public GeneratedRegistry(string @namespace)
        {
            Namespace = @namespace;
        }

        /// <summary>
        /// Namespace of the generated code.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Number of definition files the registry was generated from.
        /// </summary>
        public int DefinitionCount { get; internal set; }

        /// <summary>
        /// All files in the order they were first added.
        /// </summary>
        public IReadOnlyList<GeneratedFile> Files => _order.Select(p => _files[p]).ToList();

        /// <summary>
        /// Paths of all files in the order they were first added.
        /// </summary>
        public IReadOnlyList<string> Paths => _order.ToList();

        /// <summary>
        /// Adds a file. A file with the same path is replaced, like a second write to disk would.
        /// </summary>
        public void Add(string relativePath, string content)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("The path must not be empty.", nameof(relativePath));

            relativePath = relativePath.Replace('\\', '/');
            if (!_files.ContainsKey(relativePath))
                _order.Add(relativePath);
            _files[relativePath] = new GeneratedFile(relativePath, content);
        }

        /// <summary>
        /// True if the registry holds a file at the path.
        /// </summary>
        public bool Contains(string relativePath) => _files.ContainsKey(relativePath);

        /// <summary>
        /// Returns the text of the file at the path.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No file has this path.</exception>
        public string GetContent(string relativePath)
        {
            if (_files.TryGetValue(relativePath, out var file))
                return file.Content;
            throw new KeyNotFoundException($"The registry has no file '{relativePath}'.");
        }
    }
}
#endif
