/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A constructor in the syntax tree
    /// </summary>
    internal sealed class Constructor {
        // Backing field for the constructor body statements.
        private MethodBody _body;
        // Arguments to be forwarded to the base(...) constructor invocation.
        private readonly List<Argument> _baseArguments = new List<Argument>();
        // Access and other modifiers applied to this constructor.
        private readonly List<Modifier> _modifiers = new List<Modifier>();
        // Formal parameters declared by this constructor.
        private readonly List<Parameter> _parameters = new List<Parameter>();

        /// <summary>
        /// Type of the constructor
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Body of the constructor
        /// </summary>
        public ref readonly MethodBody Body => ref _body;
        /// <summary>
        /// Arguments for the constructor's base constructor
        /// </summary>
        public IReadOnlyList<Argument> BaseArguments => _baseArguments.AsReadOnly();
        /// <summary>
        /// Access modifiers of the constructor
        /// </summary>
        public IReadOnlyList<Modifier> Modifiers => _modifiers.AsReadOnly();
        /// <summary>
        /// Parameters of the constructor
        /// </summary>
        public IReadOnlyList<Parameter> Parameters => _parameters.AsReadOnly();

        /// <summary>
        /// Initializes a constructor with no type or body.
        /// </summary>
        public Constructor() { }

        /// <summary>
        /// Initializes a constructor with a type (class name) and optional body.
        /// </summary>
        /// <param name="type">The declaring type name (constructor identifier).</param>
        /// <param name="body">Optional method body.</param>
        public Constructor(string type, MethodBody body = default) {
            Type = type;
            this._body = body;
        }

        /// <summary>
        /// Sets the constructor type (declaring class name).
        /// </summary>
        /// <param name="type">Type name to assign.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor WithType(string type) {
            this.Type = type;
            return this;
        }

        /// <summary>
        /// Sets the constructor body.
        /// </summary>
        /// <param name="body">Method body to assign.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor WithBody(MethodBody body) {
            this._body = body;
            return this;
        }

        /// <summary>
        /// Adds a single base(...) argument.
        /// </summary>
        /// <param name="arg">Argument to forward to base constructor.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddBaseArgument(Argument arg) {
            _baseArguments.Add(arg);
            return this;
        }

        /// <summary>
        /// Adds multiple base(...) arguments.
        /// </summary>
        /// <param name="args">Arguments to forward to base constructor.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddBaseArguments(params Argument[] args) {
            _baseArguments.AddRange(args);
            return this;
        }

        /// <summary>
        /// Adds a sequence of base(...) arguments.
        /// </summary>
        /// <param name="args">Arguments to forward to base constructor.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddBaseArguments(IEnumerable<Argument> args) {
            _baseArguments.AddRange(args);
            return this;
        }

        /// <summary>
        /// Adds a single modifier to this constructor.
        /// </summary>
        /// <param name="mod">Modifier to add.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddModifier(Modifier mod) {
            _modifiers.Add(mod);
            return this;
        }

        /// <summary>
        /// Adds multiple modifiers to this constructor.
        /// </summary>
        /// <param name="mods">Modifiers to add.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddModifiers(params Modifier[] mods) {
            _modifiers.AddRange(mods);
            return this;
        }

        /// <summary>
        /// Adds a sequence of modifiers to this constructor.
        /// </summary>
        /// <param name="mods">Modifiers to add.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddModifiers(IEnumerable<Modifier> mods) {
            _modifiers.AddRange(mods);
            return this;
        }

        /// <summary>
        /// Adds a single parameter to the constructor signature.
        /// </summary>
        /// <param name="param">Parameter to add.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddParameter(Parameter param) {
            _parameters.Add(param);
            return this;
        }

        /// <summary>
        /// Adds multiple parameters to the constructor signature.
        /// </summary>
        /// <param name="parameters">Parameters to add.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddParameters(params Parameter[] parameters) {
            this._parameters.AddRange(parameters);
            return this;
        }

        /// <summary>
        /// Adds a sequence of parameters to the constructor signature.
        /// </summary>
        /// <param name="parameters">Parameters to add.</param>
        /// <returns>The same Constructor instance for fluent chaining.</returns>
        public Constructor AddParameters(IEnumerable<Parameter> parameters) {
            this._parameters.AddRange(parameters);
            return this;
        }
    }
}
#endif