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
        public readonly List<string> Requested = new List<string>();

        public string Read(string fileName)
        {
            Requested.Add(fileName);
            return "namespace xAPI.Registry { // " + fileName + " }";
        }
    }

    internal sealed class CollectingGeneratorLog : IGeneratorLog
    {
        public readonly List<string> Infos = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public void Info(string message) => Infos.Add(message);
        public void Warning(string message) => Warnings.Add(message);
    }

    internal sealed class InMemoryDefinitionSource : IDefinitionSource
    {
        private readonly List<DefinitionFile> _definitions;

        public InMemoryDefinitionSource(params DefinitionFile[] definitions) =>
            _definitions = new List<DefinitionFile>(definitions);

        public IReadOnlyList<DefinitionFile> ReadDefinitions() => _definitions;
    }

    internal sealed class InMemoryRegistryWriter : IRegistryWriter
    {
        public readonly List<GeneratedRegistry> Written = new List<GeneratedRegistry>();

        public void Write(GeneratedRegistry registry) => Written.Add(registry);
    }
}
#endif
