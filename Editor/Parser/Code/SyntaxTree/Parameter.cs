/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A parameter in the syntax tree
    /// </summary>
    internal sealed class Parameter {
        /// <summary>
        /// name of the parameter
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Type of the parameter
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Default value of the parameter
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// Initializes an empty parameter with no name, type, or default value.
        /// </summary>
        public Parameter() { }

        /// <summary>
        /// Initializes a parameter with a specified name, type, and optional default value.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The data type of the parameter. Can be null.</param>
        /// <param name="defaultValue">The optional default value for the parameter.</param>
        public Parameter(string name, string type = null, object defaultValue = null) {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Sets the name for the parameter.
        /// </summary>
        /// <param name="name">The name to assign to the parameter.</param>
        /// <returns>The same Parameter instance for fluent chaining.</returns>
        public Parameter WithName(string name) { 
            Name = name;
            return this;
        }

        /// <summary>
        /// Sets the type for the parameter.
        /// </summary>
        /// <param name="type">The type to assign to the parameter.</param>
        /// <returns>The same Parameter instance for fluent chaining.</returns>
        public Parameter WithType(string type) {
            Type = type;
            return this;
        }

        /// <summary>
        /// Sets the default value for the parameter.
        /// </summary>
        /// <param name="defaultValue">The default value to assign to the parameter.</param>
        /// <returns>The same Parameter instance for fluent chaining.</returns>
        public Parameter WithDefVal(object defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }
    }
}
#endif