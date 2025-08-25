/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Represents a resolved range type with a minimum and maximum value.
    /// Extends <see cref="ResolvedType"/> to provide additional range information.
    /// </summary>
    public sealed class ResolvedRange : ResolvedType
    {
        /// <summary>
        /// Indicates that this type is a range.
        /// </summary>
        public override bool IsRange => true;

        /// <summary>
        /// The underlying type of the range (e.g., int for int[0,10]).
        /// </summary>
        public ResolvedType ElementType { get; }

        /// <summary>
        /// The raw minimum value as a string (e.g., "0" in int[0,10]).
        /// </summary>
        public string RawMin { get; }

        /// <summary>
        /// The raw maximum value as a string (e.g., "10" in int[0,10]).
        /// </summary>
        public string RawMax { get; }

        /// <summary>
        /// Indicates whether the minimum value is inclusive.
        /// </summary>
        public bool MinInclusive { get; }

        /// <summary>
        /// Indicates whether the maximum value is inclusive.
        /// </summary>
        public bool MaxInclusive { get; }

        /// <summary>
        /// Constructs a new instance of a resolved range with the specified details.
        /// </summary>
        /// <param name="elementType">The type of the range (e.g., int, double).</param>
        /// <param name="rawMin">The raw minimum value as a string.</param>
        /// <param name="rawMax">The raw maximum value as a string.</param>
        /// <param name="minInclusive">True if the minimum value is inclusive; otherwise, false.</param>
        /// <param name="maxInclusive">True if the maximum value is inclusive; otherwise, false.</param>
        /// <param name="definition">The full range definition as a string.</param>
        public ResolvedRange(ResolvedType elementType, string rawMin, string rawMax, bool minInclusive,
            bool maxInclusive, string definition)
            : base(elementType.Type, definition)
        {
            ElementType = elementType;
            RawMin = rawMin;
            RawMax = rawMax;
            MinInclusive = minInclusive;
            MaxInclusive = maxInclusive;
        }

        /// <summary>
        /// Gets the syntax representation of the type (e.g., "int" for int[0,10]).
        /// </summary>
        /// <returns>The type syntax as a string.</returns>
        public override string GetTypeSyntax()
            => ElementType.GetTypeSyntax();
    }
}