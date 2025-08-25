/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A statement in a method
    /// </summary>
    internal class Statement {
        /// <summary>
        /// Statement as a string (i.e. "int i = 0;")
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Initializes an empty statement with no value.
        /// </summary>
        public Statement() { }

        /// <summary>
        /// Initializes a statement with the provided string value.
        /// </summary>
        /// <param name="value">The string representation of the statement.</param>
        public Statement(string value) {
            this.Value = value;
        }

        /// <summary>
        /// Sets the value for this statement.
        /// </summary>
        /// <param name="value">The string representation of the statement.</param>
        /// <returns>The same Statement instance for fluent chaining.</returns>
        public Statement WithValue(string value) {
            this.Value = value;
            return this;
        }
    }
}
#endif