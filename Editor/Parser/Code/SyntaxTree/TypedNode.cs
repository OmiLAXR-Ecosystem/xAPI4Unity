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
    /// Represents a typed node in the syntax tree that extends the base SyntaxNode functionality.
    /// </summary>
    /// <typeparam name="T">The derived type of the syntax node, enabling fluent API chaining.</typeparam>
    internal abstract class TypedNode<T> : SyntaxNode<T> where T : SyntaxNode<T>
    {
        // Holds the list of arguments used to initialize this node.
        public readonly List<Argument> Arguments = new List<Argument>();
        // Holds the list of parameters associated with this node.
        public readonly List<Parameter> Parameters = new List<Parameter>();
        
        /// <summary>
        /// Type of the property or node
        /// </summary>
        public string Type { get; private set; }
        
        /// <summary>
        /// Initializes an empty TypedNode with no name, type, or comment.
        /// </summary>
        public TypedNode() {}

        /// <summary>
        /// Initializes a TypedNode with a specified name, type, and optional comment.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="type">The type of the node (e.g., return type, property type).</param>
        /// <param name="comment">Optional comment/description for the node.</param>
        public TypedNode(string name, string type, string comment = null) : base(name, comment)
        {
            Type = type;
        }
        
        /// <summary>
        /// Sets the type for this node.
        /// </summary>
        /// <param name="type">The type to assign.</param>
        /// <returns>The same TypedNode instance for fluent chaining.</returns>
        public T WithType(string type) {
            Type = type;
            return (T)(object)this;
        }
        
        /// <summary>
        /// Adds a single initialization argument to the node.
        /// </summary>
        /// <param name="arg">The argument to add.</param>
        /// <returns>The same TypedNode instance for fluent chaining.</returns>
        public T AddArgument(Argument arg) {
            Arguments.Add(arg);
            return (T)(object)this;
        }

        /// <summary>
        /// Adds a single parameter to the node.
        /// </summary>
        /// <param name="param">The parameter to add.</param>
        /// <returns>The same TypedNode instance for fluent chaining.</returns>
        public T AddParameter(Parameter param)
        {
            Parameters.Add(param);
            return (T)(object)this;
        }
        
        /// <summary>
        /// Adds multiple parameters to the node.
        /// </summary>
        /// <param name="params">The parameters to add.</param>
        /// <returns>The same TypedNode instance for fluent chaining.</returns>
        public T AddParameters(params Parameter[] @params) {
            Parameters.AddRange(@params);
            return (T)(object)this;
        }

        /// <summary>
        /// Adds a collection of parameters to the node.
        /// </summary>
        /// <param name="params">The collection of parameters to add.</param>
        /// <returns>The same TypedNode instance for fluent chaining.</returns>
        public T AddParameters(IEnumerable<Parameter> @params) {
            Parameters.AddRange(@params);
            return (T)(object)this;
        }

        /// <summary>
        /// Adds multiple initialization arguments to the node.
        /// </summary>
        /// <param name="args">The arguments to add.</param>
        /// <returns>The same TypedNode instance for fluent chaining.</returns>
        public T AddArguments(params Argument[] args) {
            Arguments.AddRange(args);
            return (T)(object)this;
        }

        /// <summary>
        /// Adds a collection of initialization arguments to the node.
        /// </summary>
        /// <param name="args">The collection of arguments to add.</param>
        /// <returns>The same TypedNode instance for fluent chaining.</returns>
        public T AddArguments(IEnumerable<Argument> args) {
            Arguments.AddRange(args);
            return (T)(object)this;
        }
    }
}
#endif