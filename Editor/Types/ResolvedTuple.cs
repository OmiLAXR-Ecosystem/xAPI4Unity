/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.Collections.Generic;
using System.Linq;

namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Represents a resolved tuple type, which is a combination of multiple element types.
    /// Extends <see cref="ResolvedType"/> to provide tuple-specific functionality.
    /// </summary>
    public class ResolvedTuple : ResolvedType
    {
        /// <summary>
        /// Indicates that this type is a tuple.
        /// </summary>
        public override bool IsTuple => true;

        /// <summary>
        /// The size of the tuple, based on the number of elements.
        /// </summary>
        public int Size => Elements.Count;

        /// <summary>
        /// A list of resolved types that make up the tuple.
        /// </summary>
        public IReadOnlyList<ResolvedType> Elements { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolvedTuple"/> class with the given elements.
        /// </summary>
        /// <param name="elements">The list of resolved types that make up the tuple.</param>
        /// <param name="definition">The string definition of the tuple.</param>
        /// <exception cref="ArgumentException">Thrown if the elements list is null or empty.</exception>
        public ResolvedTuple(IReadOnlyList<ResolvedType> elements, string definition) 
            : base(typeof(ResolvedTuple), definition)
        {
            if (elements == null || elements.Count == 0)
                throw new ArgumentException("Tuple needs at least one element.");
            Elements = elements;
        }

        /// <summary>
        /// Returns the syntax representation of the tuple type.
        /// Combines the syntax of all element types into a tuple-like structure (e.g., "(int, float, string)").
        /// </summary>
        /// <returns>The syntax of the tuple type as a string.</returns>
        public override string GetTypeSyntax()
        {
            return "(" + string.Join(",", Elements.Select(e => e.GetTypeSyntax())) + ")";
        }
    }
}