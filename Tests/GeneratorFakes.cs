/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// In-memory implementations of the generator interfaces, so tests never touch
    /// <c>Assets/</c> or the Unity console.
    /// </summary>
    internal static class GeneratorFakes
    {
        /// <summary>
        /// Creates a definition as it would be read from <c>definitions/&lt;path&gt;</c>.
        /// </summary>
        public static DefinitionFile Definition(string path, string name = "Name", string description = "Description") =>
            new DefinitionFile(path,
                "{\"name\":{\"en-US\":\"" + name + "\"},\"description\":{\"en-US\":\"" + description + "\"}}");
    }

    /// <summary>
    /// Returns <c>namespace xAPI.Registry { // &lt;file&gt; }</c> for every core file.
    /// </summary>
    internal sealed class FakeTemplateSource : ICoreTemplateSource
    {
        /// <summary>
        /// Names of the templates that were read.
        /// </summary>
        public readonly List<string> Requested = new List<string>();

        /// <summary>
        /// Records the name and returns a one-line template.
        /// </summary>
        public string Read(string fileName)
        {
            Requested.Add(fileName);
            return "namespace xAPI.Registry { // " + fileName + " }";
        }
    }

    /// <summary>
    /// Collects the generator messages instead of writing them to the console.
    /// </summary>
    internal sealed class CollectingGeneratorLog : IGeneratorLog
    {
        /// <summary>
        /// Info messages.
        /// </summary>
        public readonly List<string> Infos = new List<string>();
        /// <summary>
        /// Warnings.
        /// </summary>
        public readonly List<string> Warnings = new List<string>();
        /// <summary>
        /// Errors.
        /// </summary>
        public readonly List<string> Errors = new List<string>();

        /// <summary>
        /// Collects an info message.
        /// </summary>
        public void Info(string message) => Infos.Add(message);
        /// <summary>
        /// Collects a warning.
        /// </summary>
        public void Warning(string message) => Warnings.Add(message);
        /// <summary>
        /// Collects an error.
        /// </summary>
        public void Error(string message) => Errors.Add(message);
    }

    /// <summary>
    /// Returns a fixed list of definitions.
    /// </summary>
    internal sealed class InMemoryDefinitionSource : IDefinitionSource
    {
        /// <summary>
        /// The definitions to return.
        /// </summary>
        private readonly List<DefinitionFile> _definitions;

        /// <summary>
        /// Creates a source with the given definitions.
        /// </summary>
        public InMemoryDefinitionSource(params DefinitionFile[] definitions) =>
            _definitions = new List<DefinitionFile>(definitions);

        /// <summary>
        /// Returns the definitions passed to the constructor.
        /// </summary>
        public IReadOnlyList<DefinitionFile> ReadDefinitions() => _definitions;
    }

    /// <summary>
    /// Keeps written registries in memory.
    /// </summary>
    internal sealed class InMemoryRegistryWriter : IRegistryWriter
    {
        /// <summary>
        /// Every registry that was written, in order.
        /// </summary>
        public readonly List<GeneratedRegistry> Written = new List<GeneratedRegistry>();

        /// <summary>
        /// Keeps the registry.
        /// </summary>
        public void Write(GeneratedRegistry registry) => Written.Add(registry);
    }
}
#endif
