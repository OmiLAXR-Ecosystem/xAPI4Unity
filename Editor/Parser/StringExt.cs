#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using UnityEngine;

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

        public static string Repeat(this string value, int n)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < n; i++)
            {
                sb.Append(value);
            }
            return sb.ToString();
        }


        /// <summary>
        /// Clears a name string of special characters that cannot be used
        /// </summary>
        /// <param name="value">String to clear</param>
        /// <returns>Cleared string with replaced or removed characters</returns>
        public static string ClearName(this string value) {
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

            var firstLetter = name[0];
            
            return (!char.IsLetter(firstLetter) ? "_" : "") + char.ToLowerInvariant(firstLetter) + name.Substring(1);
        }

        /// <summary>
        /// Clears a string of characters that must be escaped
        /// </summary>
        /// <param name="value">String to clear</param>
        /// <returns>Cleared string with replaced characters</returns>
        public static string ClearString(this string value) {
            return value
                .Replace("\"", "\\\"");
        }
    }
}
#endif