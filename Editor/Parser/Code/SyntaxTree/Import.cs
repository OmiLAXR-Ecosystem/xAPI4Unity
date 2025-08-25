/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// An import of the syntax tree
    /// </summary>
    internal sealed class Import {
        /// <summary>
        /// Target to import (i.e., a class)
        /// </summary>
        public string Target { get; private set; }

        /// <summary>
        /// Source to import from (i.e. a package)
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Initializes an empty import.
        /// </summary>
        public Import() { }

        /// <summary>
        /// Initializes an import with a target (and optional source).
        /// </summary>
        /// <param name="target">The symbol or namespace being imported.</param>
        /// <param name="source">Optional source/package/module identifier.</param>
        public Import(string target, string source = null) {
            Target = target;
            Source = source;
        }

        /// <summary>
        /// Sets the import target.
        /// </summary>
        /// <param name="target">The symbol or namespace being imported.</param>
        /// <returns>The same Import instance for fluent chaining.</returns>
        public Import WithTarget(string target) {
            Target = target;
            return this;
        }

        /// <summary>
        /// Sets the import source.
        /// </summary>
        /// <param name="source">The package/module identifier.</param>
        /// <returns>The same Import instance for fluent chaining.</returns>
        public Import WithSource(string source) {
            Source = source;
            return this;
        }
    }
}
#endif