/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using xAPI4Unity.Editor.Parser.Code;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Kind of an xAPI definition, taken from its folder in 'definitions/&lt;context&gt;/'.
    /// </summary>
    internal enum DefinitionKind
    {
        Unknown,
        Verb,
        Activity,
        Extension
    }

    /// <summary>
    /// Reads, edits and writes a single xAPI definition JSON file for the definitions editor.
    /// Fields the editor does not know, such as 'choices', are kept as they are.
    /// </summary>
    internal static class DefinitionDocument
    {
        public const string NameField = "name";
        public const string DescriptionField = "description";
        public const string ExactMatchField = "exactMatch";

        /// <summary>
        /// Fields the editor shows as their own cards. Other fields are only shown in the raw JSON.
        /// </summary>
        public static readonly string[] KnownFields = { NameField, DescriptionField, ExactMatchField };

        /// <summary>
        /// Language codes offered when adding a language.
        /// </summary>
        public static readonly string[] CommonLanguages =
            { "en-US", "de-DE", "fr-FR", "es-ES", "it-IT", "nl-NL", "pt-PT", "pl-PL", "ru-RU", "zh-CN", "ja-JP" };

        /// <summary>
        /// Parses JSON without turning date-like strings into dates or changing decimal numbers.
        /// </summary>
        public static JObject Parse(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json))
                   {
                       DateParseHandling = DateParseHandling.None,
                       FloatParseHandling = FloatParseHandling.Decimal
                   })
            {
                var token = JToken.ReadFrom(reader);
                if (token is JObject obj)
                    return obj;
                throw new JsonReaderException("A definition must be a JSON object.");
            }
        }

        /// <summary>
        /// Writes JSON like the files in the xapi repository: indented with four spaces and '\n' line breaks.
        /// </summary>
        public static string Serialize(JToken token)
        {
            var sb = new StringBuilder();
            using (var stringWriter = new StringWriter(sb) { NewLine = "\n" })
            using (var writer = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented, Indentation = 4 })
            {
                token.WriteTo(writer);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Path of a file relative to the definitions folder, with '/' separators and without '.json'.
        /// </summary>
        public static string GetKey(string definitionsRoot, string filePath)
        {
            var root = definitionsRoot.Replace('\\', '/').TrimEnd('/') + "/";
            var path = filePath.Replace('\\', '/');
            if (path.StartsWith(root, StringComparison.Ordinal))
                path = path.Substring(root.Length);
            return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? path.Substring(0, path.Length - 5) : path;
        }

        /// <summary>
        /// IRI of a definition in the xAPI Registry: the base, 'definitions/' and the key, with spaces replaced by '_'.
        /// </summary>
        public static string GetIri(string key)
            => CodeGenerator.XAPI_BASE_URI + "definitions/" + key.Replace(' ', '_');

        /// <summary>
        /// Context of a definition, the first segment of its key.
        /// </summary>
        public static string GetContext(string key)
        {
            var slash = key.IndexOf('/');
            return slash < 0 ? key : key.Substring(0, slash);
        }

        /// <summary>
        /// Kind of a definition from the second segment of its key.
        /// </summary>
        public static DefinitionKind GetKind(string key)
        {
            var segments = key.Split('/');
            if (segments.Length < 3)
                return DefinitionKind.Unknown;
            switch (segments[1])
            {
                case "verbs": return DefinitionKind.Verb;
                case "activities": return DefinitionKind.Activity;
                case "extensions": return DefinitionKind.Extension;
                default: return DefinitionKind.Unknown;
            }
        }

        /// <summary>
        /// Extension type ('activity', 'context' or 'result') of an extension key, otherwise null.
        /// </summary>
        public static string GetExtensionType(string key)
        {
            var segments = key.Split('/');
            return GetKind(key) == DefinitionKind.Extension && segments.Length >= 4 ? segments[2] : null;
        }

        /// <summary>
        /// Language map of a field such as 'name', as (language, text) pairs in file order.
        /// </summary>
        public static List<KeyValuePair<string, string>> GetLanguageMap(JObject document, string field)
        {
            var map = document[field] as JObject;
            return map == null
                ? new List<KeyValuePair<string, string>>()
                : map.Properties().Select(p => new KeyValuePair<string, string>(p.Name, p.Value?.ToString() ?? "")).ToList();
        }

        /// <summary>
        /// Sets the text of one language in a language map field, creating the field if needed.
        /// </summary>
        public static void SetText(JObject document, string field, string language, string text)
        {
            if (!(document[field] is JObject map))
            {
                map = new JObject();
                document[field] = map;
            }
            map[language] = text;
        }

        /// <summary>
        /// Renames a language in a language map field and keeps its position. Does nothing if the new code is taken.
        /// </summary>
        /// <returns>True if the language was renamed.</returns>
        public static bool RenameLanguage(JObject document, string field, string from, string to)
        {
            if (!(document[field] is JObject map) || from == to || map.ContainsKey(to) || !map.ContainsKey(from))
                return false;

            var renamed = new JObject();
            foreach (var property in map.Properties())
                renamed[property.Name == from ? to : property.Name] = property.Value;
            document[field] = renamed;
            return true;
        }

        /// <summary>
        /// Removes a language from a language map field.
        /// </summary>
        public static void RemoveLanguage(JObject document, string field, string language)
            => (document[field] as JObject)?.Remove(language);

        /// <summary>
        /// Adds the first common language that the field does not have yet, or a placeholder code.
        /// </summary>
        /// <returns>The added language code.</returns>
        public static string AddLanguage(JObject document, string field)
        {
            var existing = new HashSet<string>(GetLanguageMap(document, field).Select(p => p.Key));
            var language = CommonLanguages.FirstOrDefault(l => !existing.Contains(l));
            for (var i = 1; language == null; i++)
            {
                var candidate = "xx-XX" + (i == 1 ? "" : i.ToString());
                if (!existing.Contains(candidate))
                    language = candidate;
            }
            SetText(document, field, language, "");
            return language;
        }

        /// <summary>
        /// Entries of 'exactMatch'.
        /// </summary>
        public static List<string> GetExactMatches(JObject document)
            => (document[ExactMatchField] as JArray)?.Select(t => t?.ToString() ?? "").ToList() ?? new List<string>();

        /// <summary>
        /// Replaces 'exactMatch'. An empty list removes the field.
        /// </summary>
        public static void SetExactMatches(JObject document, IEnumerable<string> iris)
        {
            var list = iris.ToList();
            if (list.Count == 0)
                document.Remove(ExactMatchField);
            else
                document[ExactMatchField] = new JArray(list.Cast<object>().ToArray());
        }

        /// <summary>
        /// Names of the fields the editor has no card for.
        /// </summary>
        public static List<string> GetOtherFields(JObject document)
            => document.Properties().Select(p => p.Name).Where(n => !KnownFields.Contains(n)).ToList();

        /// <summary>
        /// Content of a new definition with an English name derived from the file name.
        /// </summary>
        public static JObject CreateTemplate(string fileName) => new JObject
        {
            [NameField] = new JObject { ["en-US"] = XapiDefinitionsEditorWindow.ToLowercaseWords(fileName) },
            [DescriptionField] = new JObject { ["en-US"] = "" }
        };
    }
}
#endif
