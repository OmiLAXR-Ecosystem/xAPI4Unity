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
        /// Attempts to parse a range definition from the input string in the form of type[min,max].
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <param name="type">Extracted type name.</param>
        /// <param name="min">Minimum range value.</param>
        /// <param name="max">Maximum range value.</param>
        /// <param name="minInclusive">Indicates if the minimum value is inclusive.</param>
        /// <param name="maxInclusive">Indicates if the maximum value is inclusive.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        public bool TryParseRange(string input, out string type, out string min, out string max, out bool minInclusive, out bool maxInclusive)
        {
            type = min = max = null;
            minInclusive = maxInclusive = false;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var open = input.IndexOfAny(new[] { '[', '(' });
            var close = input.LastIndexOfAny(new[] { ']', ')' });

            if (open <= 0 || close <= open) return false;

            // Extract the type name before the range brackets.
            type = input.Substring(0, open).Trim();

            var rangeContent = input.Substring(open + 1, close - open - 1);
            var parts = rangeContent.Split(',');

            if (parts.Length != 2)
                return false;

            min = parts[0].Trim();
            max = parts[1].Trim();

            minInclusive = input[open] == '[';
            maxInclusive = input[close] == ']';

            return true;
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
                TryParseRange(name, out var type, out var min, out var max, out var minInclusive, out var maxInclusive);
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