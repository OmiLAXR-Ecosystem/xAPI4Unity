/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A property in the syntax tree with a getter
    /// </summary>
    internal sealed class GetableProperty : TypedNode<GetableProperty> {
        // Backing field holding the getter node for this property.
        private Getter _getter;
        
        /// <summary>
        /// Getter of the property
        /// </summary>
        public ref readonly Getter Getter => ref _getter;

        /// <summary>
        /// Initializes a new getable property with no name/type/comment set.
        /// </summary>
        public GetableProperty()
        { }

        /// <summary>
        /// Initializes a new getable property with the provided name, type, optional getter and comment.
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <param name="type">Property type.</param>
        /// <param name="getter">Optional getter node.</param>
        /// <param name="comment">Optional XML summary comment.</param>
        public GetableProperty(string name, string type, Getter getter = null, string comment = null) : base(name, type, comment) {
            _getter = getter;
        }

        /// <summary>
        /// Assigns a getter to this property.
        /// </summary>
        /// <param name="g">The getter to set.</param>
        /// <returns>The same GetableProperty instance for fluent chaining.</returns>
        public GetableProperty WithGetter(Getter g) {
            _getter = g;
            return this;
        }
    }
}
#endif