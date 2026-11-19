/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Resolves a single type name (an alias such as <c>vec3</c>, or a full name such as
    /// <c>System.Int32</c>) to a <see cref="ResolvedType"/>.
    /// <see cref="TypeExpression"/>, <see cref="TypeExpressionSet"/> and <see cref="TypeSchema"/>
    /// depend on this interface, so they can be tested with a fixed set of names.
    /// </summary>
    public interface ITypeNameResolver
    {
        /// <summary>
        /// Resolves a type name.
        /// </summary>
        /// <returns>The resolved type, or <c>null</c> if the name is unknown.</returns>
        ResolvedType ResolveType(string name);
    }
}
#endif
