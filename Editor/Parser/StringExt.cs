/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;

namespace xAPI4Unity.Editor.Parser {
    /// <summary>
    /// Class with extension functions for strings
    /// </summary>
    internal static class StringExt
    {
        public static string ToString(object[] values) => $"[{string.Join(",", values)}]";
        /// <summary>
        /// Capitalizes the first letter of a string
        /// </summary>
        /// <param name="value">String to capitalize</param>
        /// <returns>Capitalized string</returns>
        public static string Capitalize(this string value) {
            if (value == "") {
                throw new ArgumentException($"{nameof(value)} cannot be empty", nameof(value));
            }
            return value.First().ToString().ToUpper() + value.Substring(1);
        }
        
        /// <summary>
        /// Clears a name string of special characters that cannot be used
        /// </summary>
        /// <param name="value">String to clear</param>
        /// <returns>Cleared string with replaced or removed characters</returns>
        /// <exception cref="ArgumentException">Nothing is left after removing the special characters.</exception>
        public static string ClearName(this string value) {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var name = value
                .Replace(' ', '_')
                .Replace(",", "")
                .Replace(".", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("ä", "ae")
                .Replace("ö", "oe")
                .Replace("ü", "ue")
                .Replace("Ä", "Ae")
                .Replace("Ö", "Oe")
                .Replace("Ü", "Ue")
                .Replace("ß", "ss");

            if (name.Length == 0)
                throw new ArgumentException($"'{value}' has no characters that can be used in a C# name.", nameof(value));

            var firstLetter = name[0];
            
            return (!char.IsLetter(firstLetter) ? "_" : "") + char.ToLowerInvariant(firstLetter) + name.Substring(1);
        }

        /// <summary>
        /// Escapes a string for a regular C# string literal ("…"): backslashes, quotes,
        /// line breaks and tabs. Without this, "C:\temp" in a description would become a tab
        /// and a line break would end the literal with compiler error CS1010.
        /// </summary>
        /// <param name="value">String to clear</param>
        /// <returns>Cleared string with replaced characters</returns>
        public static string ClearString(this string value) {
            if (value == null) return null;
            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }
    }
}
#endif