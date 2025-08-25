/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Represents the configuration file used by xAPI4Unity.
    /// Contains type mappings, references, and ignored contexts.
    /// </summary>
    [Serializable]
    public class ConfigFile
    {
        /// <summary>
        /// A dictionary mapping custom type names to their fully qualified definitions.
        /// </summary>
        [JsonProperty("types")]
        public Dictionary<string, string> Types = new Dictionary<string, string>();

        /// <summary>
        /// Assembly references that are included in the configuration.
        /// </summary>
        public string[] references = Array.Empty<string>();
        
        /// <summary>
        /// A list of contexts that should be ignored.
        /// </summary>
        public string[] ignoredContexts = Array.Empty<string>();

        /// <summary>
        /// Reads and deserializes a ConfigFile instance from the specified path.
        /// </summary>
        /// <param name="path">The path to the configuration file to read.</param>
        /// <returns>A deserialized <see cref="ConfigFile"/> instance.</returns>
        public static ConfigFile Read(string path)
        {
            var content = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ConfigFile>(content);
        }

        /// <summary>
        /// Provides a menu item in Unity to create a default 'xapi.config.json' file in the Assets folder.
        /// Prompts the user with a Yes/No dialog if the file already exists.
        /// </summary>
        [MenuItem("xAPI4Unity / Create 'xapi.config.json' file", false, 200)]
        private static void CreateAsset()
        {
            var filePath = Path.Combine(Application.dataPath, "xapi.config.json");

            // Check if the file already exists
            if (File.Exists(filePath))
            {
                // Prompt the user with a Yes/No dialog
                if (!EditorUtility.DisplayDialog("File Exists",
                        "'xapi.config.json' already exists. Do you want to overwrite it?", "Yes", "No"))
                {
                    return; // Exit if the user selects "No"
                }
            }

            // Create a new default ConfigFile instance
            var file = new ConfigFile
            {
                ignoredContexts = new[]
                {
                    "360tours", "assistanceSystem", "exerciseGenerator", "gitanalysis",
                    "ide", "lms", "media", "multitouch", "observation", "projectJupyter",
                    "rightsEngine", "studybuddy", "tagformance", "uhfReader", "vrRfidChamber"
                }
            };

            // Serialize it to JSON format
            var json = JsonConvert.SerializeObject(file, Formatting.Indented);

            // Write the JSON file to the Assets directory
            File.WriteAllText(filePath, json);

            Debug.Log($"'xapi.config.json' has been created at {filePath}");
        }
    }
}
#endif