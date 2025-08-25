/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Linq;

namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Resolves alias or fully qualified type names into <see cref="System.Type"/>.
    /// This class supports primitive types, Unity-specific types, and custom ranges or regex patterns.
    /// </summary>
    public class TypeNameResolver
    {
        public readonly Aliases Aliases;

        public TypeNameResolver(Aliases aliases)
        {
            Aliases = aliases;
        }
        
        /// <summary>
        /// Resolves an alias or fully qualified type name to the corresponding type. 
        /// Returns <c>null</c> if the type cannot be resolved.
        /// </summary>
        /// <param name="name">The type name or alias to resolve.</param>
        /// <returns>A <see cref="ResolvedType"/> if the resolution is successful, otherwise null.</returns>
        public ResolvedType ResolveType(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (name.EndsWith("?"))
            {
                return new ResolvedNullable(ResolveType(name.TrimEnd('?')));
            }

            // Handle range definitions such as "int[1,10]" or "double(0.1,1.0)"
            if (!name.StartsWith("(") && !name.StartsWith("[") && name.EndsWith(")") || name.EndsWith("]"))
            {
                ResolvedRange.TryParseRange(name, out var type, out var min, out var max, out var minInclusive, out var maxInclusive);
                return new ResolvedRange(ResolveType(type), min, max, minInclusive, maxInclusive, name);
            }

            // Handle tuple definitions such as "(int, float, string)"
            if (name.StartsWith("(") && name.EndsWith(")"))
            {
                var elements = name.Substring(1, name.Length - 2).Split(',').Select(e => ResolveType(e.Trim())).ToList();
                return new ResolvedTuple(elements, name);
            }

            // Handle regex definitions such as "/pattern/"
            if (name.StartsWith("/") && name.EndsWith("/"))
                return new ResolvedRegex(name);

            // Check if the name matches an alias
            if (Aliases.TryGetValue(name.Trim(), out var alias)) 
                return new ResolvedType(alias, name);

            // Attempt to resolve the type using the default system mechanism
            var t = Type.GetType(name, throwOnError: false);
            if (t != null) return new ResolvedType(t, name);

            // Attempt to resolve the type from loaded assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(name, throwOnError: false);
                if (t != null) return new ResolvedType(t, name);
            }

            return null;
        }
    }
}
#endif