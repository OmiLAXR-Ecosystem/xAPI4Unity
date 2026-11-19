/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Settings for one registry generation run.
    /// </summary>
    internal sealed class GenerationOptions
    {
        /// <summary>
        /// Creates options for the given namespace.
        /// </summary>
        /// <param name="namespace">Namespace of the generated code, also the name of the registry folder and assembly.</param>
        /// <exception cref="ArgumentException">The namespace is null or blank.</exception>
        public GenerationOptions(string @namespace)
        {
            if (string.IsNullOrWhiteSpace(@namespace))
                throw new ArgumentException("The namespace must not be empty.", nameof(@namespace));
            Namespace = @namespace;
        }

        /// <summary>
        /// Namespace of the generated code, also the name of the registry folder and assembly.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Assembly references written into the generated <c>.asmdef</c>.
        /// </summary>
        public IReadOnlyList<string> AsmDefReferences { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Optional schema that types the extension methods. Without it, every extension takes an <c>object</c>.
        /// </summary>
        public TypeSchema TypeSchema { get; set; }

        /// <summary>
        /// Contexts whose definitions are left out of the registry.
        /// </summary>
        public IReadOnlyCollection<string> IgnoredContexts { get; set; } = Array.Empty<string>();
    }
}
#endif
