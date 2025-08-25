/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Represents a resolved type based on a regular expression pattern.
    /// Extends <see cref="ResolvedType"/> with additional information specific to regex-based types.
    /// </summary>
    public class ResolvedRegex : ResolvedType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResolvedRegex"/> class
        /// with a given regex definition.
        /// </summary>
        /// <param name="definition">The regex pattern or definition for this type.</param>
        public ResolvedRegex(string definition) : base(typeof(string), definition) {}

        /// <summary>
        /// Indicates that this type is defined by a regular expression pattern.
        /// </summary>
        public override bool IsRegexExpr => true;

        /// <summary>
        /// Provides the syntax representation of this type, which corresponds to "string".
        /// </summary>
        /// <returns>The string "string".</returns>
        public override string GetTypeSyntax() => "string";
    }
}