/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree
{
    /// <summary>
    /// Represents an abstract syntax node in the syntax tree.
    /// </summary>
    /// <typeparam name="T">The derived type of the syntax node, enabling fluent API chaining.</typeparam>
    internal abstract class SyntaxNode<T> where T : SyntaxNode<T>
    {
        // Holds the list of modifiers associated with this syntax node.
        private readonly List<Modifier> _modifiers = new List<Modifier>();

        /// <summary>
        /// Name of the syntax node
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Access modifiers of the syntax node (e.g., public, private, static)
        /// </summary>
        public IReadOnlyList<Modifier> Modifiers => _modifiers.AsReadOnly();
  
        /// <summary>
        /// Comment/Description of the syntax node
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// Initializes an empty syntax node with no name or comment.
        /// </summary>
        public SyntaxNode() { }

        /// <summary>
        /// Initializes a syntax node with a specified name and optional comment.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="comment">The comment/description of the node. Optional.</param>
        public SyntaxNode(string name, string comment = default) {
            Name = name;
            Comment = comment;
        }

        /// <summary>
        /// Sets the name of the syntax node.
        /// </summary>
        /// <param name="name">The name to assign.</param>
        /// <returns>The same syntax node instance for fluent chaining.</returns>
        public T WithName(string name) {
            Name = name;
            return (T)this;
        }

        /// <summary>
        /// Adds a single modifier to the syntax node.
        /// </summary>
        /// <param name="mod">The modifier to add.</param>
        /// <returns>The same syntax node instance for fluent chaining.</returns>
        public T AddModifier(Modifier mod) {
            _modifiers.Add(mod);
            return (T)this;
        }

        /// <summary>
        /// Adds multiple modifiers to the syntax node.
        /// </summary>
        /// <param name="mods">The modifiers to add.</param>
        /// <returns>The same syntax node instance for fluent chaining.</returns>
        public T AddModifiers(params Modifier[] mods) {
            _modifiers.AddRange(mods);
            return (T)this;
        }

        /// <summary>
        /// Adds a collection of modifiers to the syntax node.
        /// </summary>
        /// <param name="mods">The collection of modifiers to add.</param>
        /// <returns>The same syntax node instance for fluent chaining.</returns>
        public T AddModifiers(IEnumerable<Modifier> mods) {
            _modifiers.AddRange(mods);
            return (T)this;
        }

        /// <summary>
        /// Sets the comment/description for the syntax node.
        /// </summary>
        /// <param name="comment">The description or XML comment to assign. Replaces < and > for safety.</param>
        /// <returns>The same syntax node instance for fluent chaining.</returns>
        public T WithComment(string comment) {
            Comment = comment.Replace(">", "&gt;").Replace("<", "&lt;");
            return (T)this;
        }
    }
}
#endif