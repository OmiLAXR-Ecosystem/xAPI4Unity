/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;

namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Represents a resolved type, including its system type and its type definition as a string.
    /// Provides a base implementation for more specialized types (e.g., ranges, tuples, regular expressions).
    /// </summary>
    public class ResolvedType
    {
        /// <summary>
        /// The corresponding <see cref="System.Type"/> of the resolved type.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// The string representation of the resolved type (e.g., as defined in user input).
        /// </summary>
        public readonly string Definition;

        /// <summary>
        /// Indicates whether the type is defined by a regular expression.
        /// </summary>
        public virtual bool IsRegexExpr => false;

        /// <summary>
        /// Indicates whether the type is a tuple.
        /// </summary>
        public virtual bool IsTuple => false;

        /// <summary>
        /// Indicates whether the type is a range.
        /// </summary>
        public virtual bool IsRange => false;

        public virtual bool IsNullable => false;

        /// <summary>
        /// Initializes a default instance of <see cref="ResolvedType"/> with an unspecified type (defaults to <see cref="object"/>).
        /// </summary>
        public ResolvedType()
        {
            Type = typeof(object);
            Definition = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ResolvedType"/> with a specific type and definition.
        /// </summary>
        /// <param name="type">The system type of the resolved type.</param>
        /// <param name="definition">The string definition of the resolved type.</param>
        public ResolvedType(Type type, string definition)
        {
            Type = type;
            Definition = definition;
        }

        /// <summary>
        /// Returns the syntax representation of the resolved type.
        /// By default, this returns the fully qualified name of the type.
        /// </summary>
        /// <returns>A string representing the syntax of the type.</returns>
        public virtual string GetTypeSyntax() => Type.FullName;
    }
}