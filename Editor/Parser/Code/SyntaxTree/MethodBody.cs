/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A method body in the syntax tree
    /// </summary>
    internal sealed class MethodBody {
        // Accumulates the ordered list of statements that compose this method body.
        private readonly List<Statement> _statements = new List<Statement>();

        /// <summary>
        /// Statements in the method
        /// </summary>
        public IReadOnlyList<Statement> Statements => _statements.AsReadOnly();

        /// <summary>
        /// Initializes an empty method body.
        /// </summary>
        public MethodBody() { }

        /// <summary>
        /// Appends a single statement to this method body.
        /// </summary>
        /// <param name="statement">The statement to add.</param>
        /// <returns>The same MethodBody instance for fluent chaining.</returns>
        public MethodBody AddStatement(Statement statement) {
            _statements.Add(statement);
            return this;
        }

        /// <summary>
        /// Appends multiple statements to this method body.
        /// </summary>
        /// <param name="statements">The statements to add.</param>
        /// <returns>The same MethodBody instance for fluent chaining.</returns>
        public MethodBody AddStatements(params Statement[] statements) {
            _statements.AddRange(statements);
            return this;
        }

        /// <summary>
        /// Appends a sequence of statements to this method body.
        /// </summary>
        /// <param name="statements">The statements to add.</param>
        /// <returns>The same MethodBody instance for fluent chaining.</returns>
        public MethodBody AddStatements(IEnumerable<Statement> statements) {
            _statements.AddRange(statements);
            return this;
        }
    }
}
#endif