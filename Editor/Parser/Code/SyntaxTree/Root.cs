/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// Root of the syntax tree
    /// </summary>
    internal class Root {
        // Holds the primary class node within the syntax tree.
        private Class _cls;
        // Holds the list of imported namespaces or classes.
        private readonly List<Import> _imports = new List<Import>();

        /// <summary>
        /// Class in the syntax tree
        /// </summary>
        public ref readonly Class Class => ref _cls;

        /// <summary>
        /// Namespace in the syntax tree
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Imports in the syntax tree
        /// </summary>
        public IReadOnlyList<Import> Imports => _imports.AsReadOnly();

        /// <summary>
        /// Initializes an empty root node with no class or namespace.
        /// </summary>
        public Root() { }

        /// <summary>
        /// Initializes a root node with the specified class and optional namespace.
        /// </summary>
        /// <param name="cls">The main class node associated with the root.</param>
        /// <param name="namespace">Optional namespace for this root.</param>
        public Root(Class cls, string @namespace = default) {
            this._cls = cls;
            this.Namespace = @namespace;
        }

        /// <summary>
        /// Sets the class node for this root.
        /// </summary>
        /// <param name="cls">The class node to assign.</param>
        /// <returns>The same Root instance for fluent chaining.</returns>
        public Root WithClass(Class cls) {
            this._cls = cls;
            return this;
        }

        /// <summary>
        /// Sets the namespace for this root.
        /// </summary>
        /// <param name="namespace">The namespace to assign.</param>
        /// <returns>The same Root instance for fluent chaining.</returns>
        public Root WithNamespace(string @namespace) {
            this.Namespace = @namespace;
            return this;
        }

        /// <summary>
        /// Adds a single import to the root's list of imports.
        /// </summary>
        /// <param name="import">The import to add.</param>
        /// <returns>The same Root instance for fluent chaining.</returns>
        public Root AddImport(Import import) {
            _imports.Add(import);
            return this;
        }

        /// <summary>
        /// Adds multiple imports to the root's list of imports.
        /// </summary>
        /// <param name="imports">The imports to add.</param>
        /// <returns>The same Root instance for fluent chaining.</returns>
        public Root AddImports(params Import[] imports) {
            this._imports.AddRange(imports);
            return this;
        }

        /// <summary>
        /// Adds a collection of imports to the root's list of imports.
        /// </summary>
        /// <param name="imports">The collection of imports to add.</param>
        /// <returns>The same Root instance for fluent chaining.</returns>
        public Root AddImports(IEnumerable<Import> imports) {
            this._imports.AddRange(imports);
            return this;
        }
    }
}
#endif