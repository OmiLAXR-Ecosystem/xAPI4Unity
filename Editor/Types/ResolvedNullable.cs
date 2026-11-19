/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// A nullable value type, such as <c>int?</c>. <see cref="ResolvedType.Type"/> stays the inner type;
    /// <see cref="GetTypeSyntax"/> adds the <c>?</c>.
    /// </summary>
    public class ResolvedNullable : ResolvedType
    {
        /// <summary>
        /// Always true.
        /// </summary>
        public override bool IsNullable => true;

        /// <summary>
        /// The type that is made nullable.
        /// </summary>
        public ResolvedType Inner { get; }

        /// <summary>
        /// Makes a resolved type nullable.
        /// </summary>
        public ResolvedNullable(ResolvedType type)
            : base((type ?? throw new System.ArgumentNullException(nameof(type))).Type, type.Definition)
        {
            Inner = type;
        }

        /// <summary>
        /// Returns the inner type's syntax followed by <c>?</c>.
        /// </summary>
        public override string GetTypeSyntax() => Inner.GetTypeSyntax() + "?";
    }
}