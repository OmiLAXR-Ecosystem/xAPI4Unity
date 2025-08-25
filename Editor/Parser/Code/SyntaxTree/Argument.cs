/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// An argument in the syntax tree
    /// </summary>
    internal class Argument {
        // Backing storage for the argument's value. Kept as 'object' to support heterogeneous argument types.
        private object _value;

        /// <summary>
        /// Name of the argument
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Value of the argument
        /// </summary>
        /// <remarks>
        /// Exposed as a ref readonly to avoid copying large values while still preventing external mutation
        /// of the backing field reference.
        /// </remarks>
        public ref readonly object Value => ref _value;

        /// <summary>
        /// Initializes a new, empty argument with no name and no value assigned.
        /// </summary>
        public Argument() { }

        /// <summary>
        /// Initializes a new argument with the provided value and optional name.
        /// </summary>
        /// <param name="value">The value to assign to this argument.</param>
        /// <param name="name">Optional name for the argument. If omitted, the argument remains unnamed.</param>
        public Argument(object value, string name = default) {
            this._value = value;
            Name = name;
        }

        /// <summary>
        /// Sets the argument name.
        /// </summary>
        /// <param name="name">The name to assign.</param>
        /// <returns>The same Argument instance for fluent chaining.</returns>
        public Argument WithName(string name) {
            Name = name;
            return this;
        }

        /// <summary>
        /// Sets the argument value.
        /// </summary>
        /// <param name="value">The value to assign.</param>
        /// <returns>The same Argument instance for fluent chaining.</returns>
        public Argument WithValue(object value) {
            _value = value;
            return this;
        }
    }
}
#endif