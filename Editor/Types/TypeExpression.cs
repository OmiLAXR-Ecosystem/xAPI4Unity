/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Represents a type expression (element type with optional arrays/nullable/tuple).
    /// Examples: "int", "string?", "float[][,,]", "(int, float)?", "(int,(float,float))[]".
    /// </summary>
    public sealed class TypeExpression
    {
        /// <summary>
        /// The original type expression as provided by the user.
        /// </summary>
        public string Original { get; }

        /// <summary>
        /// The core element name without array/nullable suffix (e.g., "int" or "(...)").
        /// </summary>
        public string CoreName { get; }

        /// <summary>
        /// Indicates whether the type is nullable (contains a '?').
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// List of array parts (e.g., "[]" or "[,,]").
        /// </summary>
        public IReadOnlyList<ArrayPart> ArrayParts { get; }

        /// <summary>
        /// Indicates whether the type contains arrays.
        /// </summary>
        public bool IsArray => ArrayParts.Count > 0;

        private TypeExpression(string original, string coreName, bool isNullable, List<ArrayPart> parts)
        {
            Original = original;
            CoreName = coreName;
            IsNullable = isNullable;
            ArrayParts = parts ?? new List<ArrayPart>(0);
        }

        /// <summary>
        /// Parses a string into a <see cref="TypeExpression"/>.
        /// </summary>
        /// <param name="typeName">The type expression string to parse.</param>
        /// <returns>A <see cref="TypeExpression"/> object.</returns>
        public static TypeExpression Parse(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("typeName must not be empty.", nameof(typeName));

            var original = typeName;
            var s = typeName.Trim();

            // Handle nullable types with '?'
            var isNullable = false;
            if (s != null && s.EndsWith("?", StringComparison.Ordinal))
            {
                isNullable = true;
                s = s.Substring(0, s.Length - 1).TrimEnd();
            }

            // Extract array suffixes (e.g., "[][][]" or "[,,]")
            var (baseName, parts) = SplitArraySyntax(s);

            return new TypeExpression(original, baseName, isNullable, parts);
        }

        /// <summary>
        /// Resolves the type expression into a <see cref="ResolvedType"/>.
        /// </summary>
        /// <param name="typeNameResolver">A resolver function to map type names to <see cref="ResolvedType"/> instances.</param>
        /// <returns>A <see cref="ResolvedType"/> corresponding to the type expression.</returns>
        public ResolvedType Resolve(TypeNameResolver typeNameResolver)
        {
            if (typeNameResolver == null) throw new ArgumentNullException(nameof(typeNameResolver));

            ResolvedType elementType;

            // Handle range syntax (e.g., "int[1,10]")
            if (IsRangeSyntax(CoreName))
            {
                ResolvedRange.TryParseRange(CoreName, out var type, out var min, out var max, out var minInclusive, out var maxInclusive);
                elementType = new ResolvedRange(typeNameResolver.ResolveType(type), min, max, minInclusive, maxInclusive, CoreName);
            }
            // Handle tuple syntax (e.g., "(int, string)")
            else if (IsTupleSyntax(CoreName))
            {
                var inner = CoreName.Substring(1, CoreName.Length - 2);
                var items = SplitTopLevel(inner, ',')
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)
                    .Select(Parse) // Recursively parse sub-expressions
                    .Select(e => e.Resolve(typeNameResolver))
                    .ToArray();

                if (items.Any(t => t == null))
                    return null;

                elementType = new ResolvedTuple(items, CoreName);
            }
            // Handle basic alias or fully qualified type resolution
            else
            {
                elementType = typeNameResolver.ResolveType(CoreName);
                if (elementType == null) return null;
            }

            var currentType = elementType;

            // Apply nullable modifier if applicable
            if (IsNullable)
            {
                var t = currentType.Type;
                if (t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                {
                    currentType = new ResolvedNullable(currentType);
                }
            }

            // Apply array parts
            foreach (var part in ArrayParts)
            {
                var arrayType = part.IsJagged
                    ? currentType.Type.MakeArrayType()
                    : currentType.Type.MakeArrayType(part.Rank);

                var brackets = part.IsJagged ? "[]" : $"[{new string(',', part.Rank - 1)}]";
                currentType = new ResolvedType(arrayType, $"{currentType.Definition}{brackets}");
            }

            return currentType;
        }

        /// <summary>
        /// Checks if a string represents a range syntax (e.g., "int[1,10]").
        /// </summary>
        private static bool IsRangeSyntax(string s)
            => s.Length >= 2 && s[0] != '[' && s[0] != '(' && (s[s.Length - 1] == ']' || s[s.Length - 1] == ')');

        /// <summary>
        /// Checks if a string represents a tuple syntax (e.g., "(int, float)").
        /// </summary>
        private static bool IsTupleSyntax(string s)
            => s != null && s.Length >= 2 && s[0] == '(' && s[s.Length - 1] == ')';


        /// <summary>
        /// Splits an input string into its core type and array parts.
        /// </summary>
        /// <param name="input">The input string to split.</param>
        /// <returns>A tuple containing the core type and the list of array parts.</returns>
        private static (string Core, List<ArrayPart> Parts) SplitArraySyntax(string input)
        {
            var rx = new Regex(@"^\s*(?<core>[^\[\]]+?)(?<arr>(\s*\[\s*,*\s*\]\s*)*)\s*$");
            var m = rx.Match(input);
            if (!m.Success)
                return (input.Trim(), new List<ArrayPart>(0));

            var core = m.Groups["core"].Value.Trim();
            var arr = m.Groups["arr"].Value;

            var parts = new List<ArrayPart>();
            var partRx = new Regex(@"\[\s*(?<commas>,*)\s*\]");
            foreach (Match pm in partRx.Matches(arr))
            {
                var commas = pm.Groups["commas"].Value;
                if (commas.Length == 0) parts.Add(ArrayPart.Jagged());
                else parts.Add(ArrayPart.Multi(commas.Length + 1));
            }

            return (core, parts);
        }

        /// <summary>
        /// Splits a string into top-level parts, respecting parentheses, brackets, and angle brackets.
        /// </summary>
        internal static IEnumerable<string> SplitTopLevel(string text, char separator)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            int start = 0;
            int paren = 0, angle = 0, square = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '(': paren++; break;
                    case ')': paren--; break;
                    case '<': angle++; break;
                    case '>': angle--; break;
                    case '[': square++; break;
                    case ']': square--; break;
                }

                if (c == separator && paren == 0 && angle == 0 && square == 0)
                {
                    yield return text.Substring(start, i - start);
                    start = i + 1;
                }
            }

            if (start <= text.Length)
                yield return text.Substring(start);
        }

        /// <summary>
        /// Represents the array part of a type expression (e.g., "[]" or "[,,]").
        /// </summary>
        public readonly struct ArrayPart
        {
            public bool IsJagged { get; }
            public int Rank { get; } // 2 => [,], 3 => [,,]

            private ArrayPart(bool isJagged, int rank)
            {
                IsJagged = isJagged;
                Rank = rank;
            }

            public static ArrayPart Jagged() => new ArrayPart(true, 1);

            public static ArrayPart Multi(int rank)
            {
                if (rank < 2)
                    throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be >= 2 for multidimensional arrays.");
                return new ArrayPart(false, rank);
            }

            public override string ToString() => IsJagged ? "[]" : $"[{new string(',', Rank - 1)}]";
        }
    }

    /// <summary>
    /// Represents a union of multiple type expressions (e.g., "int | string" or "(int,float) | vec3").
    /// </summary>
    public sealed class TypeExpressionSet
    {
        /// <summary>
        /// The list of type expression options.
        /// </summary>
        public IReadOnlyList<TypeExpression> Options { get; }

        private TypeExpressionSet(IReadOnlyList<TypeExpression> options)
        {
            if (options == null || options.Count == 0)
                throw new ArgumentException("At least one type option is required.", nameof(options));
            Options = options;
        }

        /// <summary>
        /// Parses a string into a <see cref="TypeExpressionSet"/>.
        /// </summary>
        /// <param name="expression">The type expression string to parse.</param>
        /// <returns>A <see cref="TypeExpressionSet"/> object.</returns>
        public static TypeExpressionSet Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Expression must not be empty.", nameof(expression));

            var parts = TypeExpression.SplitTopLevel(expression, '|')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Select(TypeExpression.Parse)
                .ToArray();

            return new TypeExpressionSet(parts);
        }

        /// <summary>
        /// Resolves all runtime types represented by the options in the set.
        /// </summary>
        /// <param name="resolver">A function to resolve each type name.</param>
        /// <returns>A list of resolved types.</returns>
        public IReadOnlyList<ResolvedType> ResolveAll(TypeNameResolver resolver)
        {
            var list = new List<ResolvedType>(Options.Count);
            foreach (var p in Options)
            {
                var t = p.Resolve(resolver);
                if (t != null) list.Add(t);
            }

            return list
                .GroupBy(t => t.Type.FullName ?? t.Type.Name, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToArray();
        }

        /// <summary>
        /// Resolves the preferred type using a custom preference function.
        /// </summary>
        public ResolvedType ResolvePrefer(Func<ResolvedType, bool> prefer, TypeNameResolver resolver)
        {
            var all = ResolveAll(resolver);
            if (all.Count == 0) return null;
            var hit = all.FirstOrDefault(prefer);
            return hit ?? all[0];
        }

        /// <summary>
        /// Checks if the value matches any type in the set.
        /// </summary>
        public bool MatchesValue(object value, TypeNameResolver resolver)
        {
            if (value == null) return true;
            var vt = value.GetType();
            foreach (var t in ResolveAll(resolver))
            {
                if (t.Type.IsAssignableFrom(vt)) 
                    return true;
            }
            return false;
        }

        public override string ToString() => string.Join(" | ", Options.Select(o => o.Original));
    }
}
#endif