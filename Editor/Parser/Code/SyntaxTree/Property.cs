/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// Represents a property in the syntax tree. 
    /// A Property is a node that has a name, a type, and can optionally hold an initializer.
    /// </summary>
    internal class Property : TypedNode<Property> {
        
        /// <summary>
        /// The initializer for the property (e.g., default value assignment).
        /// </summary>
        public string Initializer { get; set; } 

        /// <summary>
        /// Initializes an empty instance of the Property class.
        /// </summary>
        public Property() { }

        /// <summary>
        /// Assigns an initializer to the property.
        /// </summary>
        /// <param name="initializer">The initializer value as a string.</param>
        /// <returns>The same Property instance for fluent chaining.</returns>
        public Property WithInitializer(string initializer)
        {
            this.Initializer = initializer;
            return this;
        }

        /// <summary>
        /// Initializes a Property with the specified name, type, and optional comment.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="type">The data type of the property.</param>
        /// <param name="comment">An optional comment/description for the property.</param>
        public Property(string name, string type, string comment = null) : base(name, type, comment) {
        }
    }
}
#endif