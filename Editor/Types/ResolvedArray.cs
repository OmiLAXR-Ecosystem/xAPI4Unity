/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;

namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// An array of another resolved type, such as <c>float[]</c>, <c>int[,]</c> or <c>(int,float)[]</c>.
    /// The C# syntax is built from the element's syntax, so arrays of tuples and nullables
    /// keep a syntax that compiles.
    /// </summary>
    public sealed class ResolvedArray : ResolvedType
    {
        /// <summary>
        /// The type of the elements.
        /// </summary>
        public ResolvedType ElementType { get; }

        /// <summary>
        /// True for <c>[]</c>, false for multidimensional arrays such as <c>[,]</c>.
        /// </summary>
        public bool IsJagged { get; }

        /// <summary>
        /// Number of dimensions: 1 for <c>[]</c>, 2 for <c>[,]</c>, …
        /// </summary>
        public int Rank { get; }

        public ResolvedArray(ResolvedType elementType, bool isJagged, int rank, string definition)
            : base(MakeArrayType(elementType, isJagged, rank), definition)
        {
            ElementType = elementType;
            IsJagged = isJagged;
            Rank = isJagged ? 1 : rank;
        }

        public override string GetTypeSyntax() =>
            ElementType.GetTypeSyntax() + (IsJagged ? "[]" : "[" + new string(',', Rank - 1) + "]");

        private static Type MakeArrayType(ResolvedType elementType, bool isJagged, int rank)
        {
            if (elementType == null) throw new ArgumentNullException(nameof(elementType));
            return isJagged ? elementType.Type.MakeArrayType() : elementType.Type.MakeArrayType(rank);
        }
    }
}
