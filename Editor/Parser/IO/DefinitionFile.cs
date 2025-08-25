/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;
using Newtonsoft.Json;

namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Represents different types of extensions in the xAPI definition files (e.g., Activity, Context, or Result).
    /// </summary>
    internal enum ExtensionType
    {
        Unknown,
        Activity,
        Context,
        Result
    }

    /// <summary>
    /// Represents different types of definitions in the xAPI definition files (e.g., Extension, Verb, or Activity).
    /// </summary>
    internal enum DefinitionType
    {
        Unknown,
        Extension,
        Verb,
        Activity
    }

    /// <summary>
    /// Represents the content of an xAPI definition file, including translated names, descriptions, and matching information.
    /// </summary>
    internal struct DefinitionFileContent
    {
        /// <summary>
        /// Dictionary of localized names for the definition.
        /// </summary>
        [JsonProperty("name")]
        public Dictionary<string, string> Names { get; set; }

        /// <summary>
        /// Dictionary of localized descriptions for the definition.
        /// </summary>
        [JsonProperty("description")]
        public Dictionary<string, string> Descriptions { get; set; }

        /// <summary>
        /// Related match URIs. May be null if not provided.
        /// </summary>
        [JsonProperty("relatedMatch", NullValueHandling = NullValueHandling.Ignore)]
        public string[] RelatedMatch { get; set; }

        /// <summary>
        /// Exact match URIs. May be null if not provided.
        /// </summary>
        [JsonProperty("exactMatch", NullValueHandling = NullValueHandling.Ignore)]
        public string[] ExactMatch { get; set; }
    }

    /// <summary>
    /// Represents a single xAPI definition file, including its content and metadata.
    /// </summary>
    internal class DefinitionFile
    {
        /// <summary>
        /// The file path (slashed for cross-platform support).
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// The raw JSON content of the file.
        /// </summary>
        public readonly string JsonContent;

        /// <summary>
        /// The parsed content of the JSON in the file.
        /// </summary>
        public readonly DefinitionFileContent Content;

        /// <summary>
        /// The name of the definition (derived from the file name, without extension).
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The context this definition belongs to (e.g., a namespace or custom scope).
        /// </summary>
        public readonly string Context;

        /// <summary>
        /// The associated category of this definition, if applicable.
        /// </summary>
        public readonly string Category;

        /// <summary>
        /// Indicates whether this definition belongs to a specific category.
        /// </summary>
        public bool HasCategory => !string.IsNullOrEmpty(Category);

        /// <summary>
        /// The type of the xAPI definition (e.g., Extension, Verb, or Activity).
        /// </summary>
        public readonly DefinitionType DefinitionType;

        /// <summary>
        /// The type of extension for xAPI definitions categorized as extensions (e.g., Activity, Context, or Result).
        /// </summary>
        public readonly ExtensionType ExtensionType;

        /// <summary>
        /// Creates an instance of a DefinitionFile based on its path and content.
        /// </summary>
        /// <param name="path">The file path in the definition structure.</param>
        /// <param name="content">The raw JSON content of the file.</param>
        public DefinitionFile(string path, string content)
        {
            // Normalize the path to use slashes for compatibility across environments.
            Path = path.Replace('\\', '/');
            JsonContent = content;

            // Derive the definition name from the file name (without extension).
            Name = System.IO.Path.GetFileNameWithoutExtension(path).ClearName();

            // Split path segments to extract context, definition type, and other metadata.
            var s = Path.Split('/');
            Context = s[0];
            DefinitionType = DetectDefinitionType(s[1]);

            // Determine the specific extension type, if applicable.
            if (DefinitionType == DefinitionType.Extension)
            {
                ExtensionType = DetectExtensionType(s[2]);
            }

            // Parse the JSON content into structured definition file content.
            if (!string.IsNullOrEmpty(JsonContent))
            {
                Content = JsonConvert.DeserializeObject<DefinitionFileContent>(JsonContent);
            }

            // Extract category information based on path depth and definition type.
            if (s.Length >= 4 && (DefinitionType == DefinitionType.Verb || DefinitionType == DefinitionType.Activity))
                Category = s[2];
            else if (s.Length >= 5 && DefinitionType == DefinitionType.Extension)
                Category = s[3];
        }

        /// <summary>
        /// Detects the definition type based on the path segment.
        /// </summary>
        /// <param name="type">The path segment representing the type.</param>
        /// <returns>The detected DefinitionType.</returns>
        private DefinitionType DetectDefinitionType(string type)
        {
            switch (type)
            {
                case "extensions":
                    return DefinitionType.Extension;
                case "verbs":
                    return DefinitionType.Verb;
                case "activities":
                    return DefinitionType.Activity;
                default:
                    return DefinitionType.Unknown;
            }
        }

        /// <summary>
        /// Detects the extension type based on the path segment.
        /// </summary>
        /// <param name="type">The path segment representing the extension type.</param>
        /// <returns>The detected ExtensionType.</returns>
        private ExtensionType DetectExtensionType(string type)
        {
            switch (type)
            {
                case "result":
                    return ExtensionType.Result;
                case "context":
                    return ExtensionType.Context;
                case "activity":
                    return ExtensionType.Activity;
                default:
                    return ExtensionType.Unknown;
            }
        }

        /// <summary>
        /// Provides a string representation of the DefinitionFile, including key metadata.
        /// </summary>
        /// <returns>A formatted string containing the file's details.</returns>
        public override string ToString() => $"[File: Name=\"{Name}\", Context=\"{Context}\", DefinitionType=\"{DefinitionType}\", ExtensionType=\"{ExtensionType}\", Path=\"{Path}\", Content=\"{Content}\"]";
    }
}
#endif