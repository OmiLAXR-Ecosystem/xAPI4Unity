/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Represents a schema mapping keys to a union of type expressions (<see cref="TypeExpressionSet"/>).
    /// </summary>
    public class TypeSchema : Dictionary<string, TypeExpressionSet>
    {
        private TypeNameResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeSchema"/> class.
        /// Uses <see cref="StringComparer.Ordinal"/> for case-sensitive keys.
        /// </summary>
        public TypeSchema(TypeNameResolver resolver) : base(StringComparer.Ordinal)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Resolves types from a given key in the schema.
        /// If the key doesn't exist, returns a default type "object".
        /// </summary>
        /// <param name="resolveType">Type resolver method.</param>
        /// <param name="key">The schema key to resolve types for.</param>
        /// <returns>An array of resolved types associated with the key.</returns>
        public ResolvedType[] GetTypesFromKey(string key)
        {
            if (!ContainsKey(key)) return new[] { new ResolvedType(typeof(object), null) };
            var entry = this[key];
            return entry.ResolveAll(_resolver).ToArray();
        }

        /// <summary>
        /// Parses a schema from a dictionary of type mappings.
        /// Throws exceptions on validation or parsing errors.
        /// </summary>
        /// <param name="input">The dictionary mapping keys to type expressions.</param>
        /// <param name="validateResolution">If true, validates that all types can be resolved.</param>
        /// <param name="resolver">Optional custom resolver for type names.</param>
        /// <returns>A parsed and validated <see cref="TypeSchema"/> object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the input is null.</exception>
        public static TypeSchema Parse(
            IDictionary<string, string> input,
            bool validateResolution = true,
            TypeNameResolver resolver = null)
        {
            if (input == null) 
                throw new ArgumentNullException("input");

            var result = new TypeSchema(resolver);

            foreach (var kv in input)
            {
                var key = kv.Key?.Trim();
                var expr = kv.Value?.Trim();

                if (string.IsNullOrWhiteSpace(key))
                    Debug.LogError($"Schema key in {nameof(input)} must not be empty.");
                if (string.IsNullOrWhiteSpace(expr))
                    Debug.LogError($"Schema for '{key}' in {nameof(input)} is empty.");

                var set = TypeExpressionSet.Parse(expr);

                if (validateResolution)
                {
                    var candidates = set.ResolveAll(resolver);
                    if (candidates.Count == 0)
                        Debug.LogError(
                            $"None of the types for key '{key}' could be resolved: {set}. " +
                            "Check spelling, namespace, or aliases.");
                }

                result[key] = set;
            }

            return result;
        }

        /// <summary>
        /// Attempts to parse a schema, collecting errors instead of throwing exceptions.
        /// </summary>
        /// <param name="input">The dictionary mapping keys to type expressions.</param>
        /// <param name="schema">The resulting parsed schema.</param>
        /// <param name="errors">A list of error messages encountered during parsing.</param>
        /// <param name="validateResolution">If true, validates that all types can be resolved.</param>
        /// <param name="resolver">Optional custom resolver for type names.</param>
        /// <returns>True if the schema was parsed successfully, otherwise false.</returns>
        public static bool TryParse(
            IDictionary<string, string> input,
            out Dictionary<string, TypeExpressionSet> schema,
            out List<string> errors,
            bool validateResolution = true,
            TypeNameResolver resolver = null)
        {
            schema = new TypeSchema(resolver);
            errors = new List<string>();
            if (input == null)
            {
                errors.Add("Input is null.");
                return false;
            }
            
            foreach (var kv in input)
            {
                var key = kv.Key?.Trim();
                var expr = kv.Value?.Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    errors.Add("Empty schema key detected.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(expr))
                {
                    errors.Add($"Schema for '{key}' is empty.");
                    continue;
                }

                try
                {
                    var set = TypeExpressionSet.Parse(expr);

                    if (validateResolution)
                    {
                        var candidates = set.ResolveAll(resolver);
                        if (candidates.Count == 0)
                        {
                            errors.Add(
                                $"Key '{key}': none of the types could be resolved: {set}.");
                            continue;
                        }
                    }

                    schema[key] = set;
                }
                catch (Exception ex)
                {
                    errors.Add($"Key '{key}': {ex.Message}");
                }
            }

            return errors.Count == 0;
        }
    }
}
#endif