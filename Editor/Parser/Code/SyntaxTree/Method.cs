/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A method in the syntax tree
    /// </summary>
    internal sealed class Method : TypedNode<Method> {
        // Holds the serialized body for this method (its statements).
        private MethodBody _body;
        // Arguments to be forwarded to a base method invocation, when applicable.
        private readonly List<Argument> _baseArguments = new List<Argument>();
        
        /// <summary>
        /// Body of the method
        /// </summary>
        public ref readonly MethodBody Body => ref _body;
        /// <summary>
        /// Arguments for the method's base method
        /// </summary>
        public IReadOnlyList<Argument> BaseArguments => _baseArguments.AsReadOnly();

        /// <summary>
        /// Initializes an empty method node.
        /// </summary>
        public Method() { }

        /// <summary>
        /// Initializes a method node with name, return type, optional body, and XML summary comment.
        /// </summary>
        /// <param name="name">Method name.</param>
        /// <param name="type">Return type (nullable).</param>
        /// <param name="body">Optional method body.</param>
        /// <param name="comment">Optional XML summary comment.</param>
        public Method(string name, string type = null, MethodBody body = null, string comment = null) : base(name, type, comment) {
            _body = body;
        }

        /// <summary>
        /// Sets the method body.
        /// </summary>
        /// <param name="b">The MethodBody to assign.</param>
        /// <returns>The same Method instance for fluent chaining.</returns>
        public Method WithBody(MethodBody b) {
            _body = b;
            return this;
        }

        /// <summary>
        /// Adds a single base-method argument.
        /// </summary>
        /// <param name="arg">Argument to forward to the base method.</param>
        /// <returns>The same Method instance for fluent chaining.</returns>
        public Method AddBaseArgument(Argument arg) {
            _baseArguments.Add(arg);
            return this;
        }

        /// <summary>
        /// Adds multiple base-method arguments.
        /// </summary>
        /// <param name="args">Arguments to forward to the base method.</param>
        /// <returns>The same Method instance for fluent chaining.</returns>
        public Method AddBaseArguments(params Argument[] args) {
            _baseArguments.AddRange(args);
            return this;
        }

        /// <summary>
        /// Adds a sequence of base-method arguments.
        /// </summary>
        /// <param name="args">Arguments to forward to the base method.</param>
        /// <returns>The same Method instance for fluent chaining.</returns>
        public Method AddBaseArguments(IEnumerable<Argument> args) {
            _baseArguments.AddRange(args);
            return this;
        }
       
    }
}
#endif