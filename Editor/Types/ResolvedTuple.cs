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
            : base(MakeValueTupleType(elements), definition)
        {
            Elements = elements;
        }

        /// <summary>
        /// Open <c>ValueTuple</c> types with one to eight type arguments.
        /// </summary>
        private static readonly Type[] ValueTupleDefinitions =
        {
            typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>)
        };

        /// <summary>
        /// Builds the <see cref="ValueTuple"/> type of the elements, as the C# compiler does for
        /// <c>(T1, T2, …)</c>. More than seven elements nest the rest into the eighth argument.
        /// </summary>
        private static Type MakeValueTupleType(IReadOnlyList<ResolvedType> elements)
        {
            if (elements == null || elements.Count == 0 || elements.Any(e => e == null))
                throw new ArgumentException("Tuple needs at least one element.");
            return MakeValueTupleType(elements.Select(e => e.Type).ToArray());
        }

        /// <summary>
        /// Builds the <c>ValueTuple</c> type of the element types.
        /// </summary>
        private static Type MakeValueTupleType(Type[] types)
        {
            if (types.Length <= 7)
                return ValueTupleDefinitions[types.Length - 1].MakeGenericType(types);

            var rest = MakeValueTupleType(types.Skip(7).ToArray());
            return ValueTupleDefinitions[7].MakeGenericType(types.Take(7).Concat(new[] { rest }).ToArray());
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