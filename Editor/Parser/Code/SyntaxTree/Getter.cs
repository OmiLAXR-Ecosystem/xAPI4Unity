/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A getter in the syntax tree
    /// </summary>
    internal sealed class Getter {
        // Backing field for the getter body. Stored as a MethodBody node representing the serialized statements.
        private MethodBody _body;

        /// <summary>
        /// Method body of the getter
        /// </summary>
        public ref readonly MethodBody Body => ref _body;

        /// <summary>
        /// Creates an empty getter with nobody.
        /// </summary>
        public Getter() { }

        /// <summary>
        /// Creates a getter with the provided method body.
        /// </summary>
        /// <param name="body">The method body to associate with this getter.</param>
        public Getter(MethodBody body) {
            _body = body;
        }

        /// <summary>
        /// Sets the method body for this getter.
        /// </summary>
        /// <param name="body">The method body to assign.</param>
        /// <returns>The same Getter instance for fluent chaining.</returns>
        public Getter WithMethodBody(MethodBody body) {
            _body = body;
            return this;
        }
    }
}
#endif