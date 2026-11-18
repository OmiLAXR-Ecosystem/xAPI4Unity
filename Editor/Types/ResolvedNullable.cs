/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
namespace xAPI4Unity.Editor.Types
{
    public class ResolvedNullable : ResolvedType
    {
        public override bool IsNullable => true;

        /// <summary>
        /// The type that is made nullable.
        /// </summary>
        public ResolvedType Inner { get; }

        public ResolvedNullable(ResolvedType type)
            : base((type ?? throw new System.ArgumentNullException(nameof(type))).Type, type.Definition)
        {
            Inner = type;
        }

        public override string GetTypeSyntax() => Inner.GetTypeSyntax() + "?";
    }
}