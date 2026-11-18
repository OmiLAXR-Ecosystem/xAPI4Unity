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
        public ResolvedNullable(ResolvedType type) : base(type.Type, type.Definition)
        {
            
        }

        public override string GetTypeSyntax() => base.GetTypeSyntax() + "?";
    }
}