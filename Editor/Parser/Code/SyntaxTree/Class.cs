/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A class in the syntax tree
    /// </summary>
    internal sealed class Class : SyntaxNode<Class> {
        // Accumulates field/property declarations to be emitted for this class.
        private readonly List<Property> _properties = new List<Property>();
        // Accumulates constructor declarations for this class.
        private readonly List<Constructor> _constructors = new List<Constructor>();
        // Accumulates get-only properties (with custom getters).
        private readonly List<GetableProperty> _getableProperties = new List<GetableProperty>();
        // Accumulates method declarations for this class.
        private readonly List<Method> _methods = new List<Method>();
        
        /// <summary>
        /// Base class of the class
        /// </summary>
        public string BaseClass { get; private set; }

        /// <summary>
        /// Properties of the class
        /// </summary>
        public IReadOnlyList<Property> Properties => _properties.AsReadOnly();
        /// <summary>
        /// Constructors of the class
        /// </summary>
        public IReadOnlyList<Constructor> Constructors => _constructors.AsReadOnly();
        /// <summary>
        /// Properties of the class that have a getter
        /// </summary>
        public IReadOnlyList<GetableProperty> GetableProperties => _getableProperties.AsReadOnly();
        /// <summary>
        /// Methods of the class
        /// </summary>
        public IReadOnlyList<Method> Methods => _methods.AsReadOnly();

        /// <summary>
        /// Initializes a class node with no name/comment.
        /// </summary>
        public Class()
        { }

        /// <summary>
        /// Initializes a class node with a name, optional base class and comment.
        /// </summary>
        /// <param name="name">Class name.</param>
        /// <param name="baseClass">Optional base class name.</param>
        /// <param name="comment">Optional XML summary comment.</param>
        public Class(string name, string baseClass = null, string comment = null) : base(name, comment) {
            BaseClass = baseClass;
        }

        /// <summary>
        /// Sets the base class for this class node.
        /// </summary>
        /// <param name="baseClass">Base class name.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class WithBaseClass(string baseClass) {
            BaseClass = baseClass;
            return this;
        }

        /// <summary>
        /// Adds a property to the class.
        /// </summary>
        /// <param name="property">Property to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddProperty(Property property) {
            _properties.Add(property);
            return this;
        }

        /// <summary>
        /// Adds multiple properties to the class.
        /// </summary>
        /// <param name="properties">Properties to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddProperties(params Property[] properties) {
            this._properties.AddRange(properties);
            return this;
        }

        /// <summary>
        /// Adds a sequence of properties to the class.
        /// </summary>
        /// <param name="properties">Properties to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddProperties(IEnumerable<Property> properties) {
            this._properties.AddRange(properties);
            return this;
        }

        /// <summary>
        /// Adds a constructor to the class.
        /// </summary>
        /// <param name="constructor">Constructor to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddConstructor(Constructor constructor) {
            _constructors.Add(constructor);
            return this;
        }

        /// <summary>
        /// Adds multiple constructors to the class.
        /// </summary>
        /// <param name="constructors">Constructors to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddConstructors(params Constructor[] constructors) {
            this._constructors.AddRange(constructors);
            return this;
        }

        /// <summary>
        /// Adds a sequence of constructors to the class.
        /// </summary>
        /// <param name="constructors">Constructors to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddConstructors(IEnumerable<Constructor> constructors) {
            this._constructors.AddRange(constructors);
            return this;
        }

        /// <summary>
        /// Adds a get-only property to the class.
        /// </summary>
        /// <param name="getableProperty">Getable property to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddGetableProperty(GetableProperty getableProperty) {
            _getableProperties.Add(getableProperty);
            return this;
        }

        /// <summary>
        /// Adds multiple get-only properties to the class.
        /// </summary>
        /// <param name="getableProperties">Getable properties to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddGetableProperties(params GetableProperty[] getableProperties) {
            this._getableProperties.AddRange(getableProperties);
            return this;
        }

        /// <summary>
        /// Adds a sequence of get-only properties to the class.
        /// </summary>
        /// <param name="getableProperties">Getable properties to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddGetableProperties(IEnumerable<GetableProperty> getableProperties) {
            this._getableProperties.AddRange(getableProperties);
            return this;
        }

        /// <summary>
        /// Adds a method to the class.
        /// </summary>
        /// <param name="method">Method to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddMethod(Method method) {
            _methods.Add(method);
            return this;
        }

        /// <summary>
        /// Adds multiple methods to the class.
        /// </summary>
        /// <param name="methods">Methods to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddMethods(params Method[] methods) {
            this._methods.AddRange(methods);
            return this;
        }

        /// <summary>
        /// Adds a sequence of methods to the class.
        /// </summary>
        /// <param name="methods">Methods to add.</param>
        /// <returns>The same Class instance for fluent chaining.</returns>
        public Class AddMethods(IEnumerable<Method> methods) {
            this._methods.AddRange(methods);
            return this;
        }
    }
}
#endif