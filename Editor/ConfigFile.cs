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
        /// Variables for the type expressions: <c>${name}</c> in <see cref="Types"/> is replaced by the value of <c>name</c>.
        /// </summary>
        public Dictionary<string, string> Variables = new Dictionary<string, string>();

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
        private static void CreateAsset() =>
            CreateAsking(Path.Combine(Application.dataPath, "xapi.config.json"),
                UnityEditorDialogs.Instance, UnityAssetRefresher.Instance);

        /// <summary>
        /// Writes a default config file. If one exists, it is only overwritten when the user agrees.
        /// </summary>
        /// <returns>True if the file was written.</returns>
        internal static bool CreateAsking(string filePath, IEditorDialogs dialogs, IAssetRefresher refresher)
        {
            if (File.Exists(filePath) &&
                !dialogs.Confirm("File Exists",
                    $"'{Path.GetFileName(filePath)}' already exists. Do you want to overwrite it?", "Yes", "No"))
                return false;

            Create(filePath, refresher);
            return true;
        }

        /// <summary>
        /// Reads a config file and parses its types like the generator does. Problems are logged as errors.
        /// </summary>
        /// <param name="filePath">Path of the config file.</param>
        /// <returns>The number of typed extensions.</returns>
        internal static int Validate(string filePath)
        {
            var config = Read(filePath);
            if (config == null)
                throw new InvalidDataException($"'{Path.GetFileName(filePath)}' is empty.");

            var types = config.Types ?? new Dictionary<string, string>();
            TypeSchema.Parse(types, validateResolution: true, new TypeNameResolver(new Aliases()), config.Variables, true);
            return types.Count;
        }

        /// <summary>
        /// The config a new project starts with: no types, and the contexts that are not about XR ignored.
        /// </summary>
        internal static ConfigFile CreateDefault() => new ConfigFile
        {
            ignoredContexts = new[]
            {
                "360tours", "assistanceSystem", "exerciseGenerator", "gitanalysis",
                "ide", "lms", "media", "multitouch", "observation", "projectJupyter",
                "rightsEngine", "studybuddy", "tagformance", "uhfReader", "vrRfidChamber"
            }
        };

        /// <summary>
        /// Writes a default config file to the given path, overwriting an existing one.
        /// </summary>
        /// <param name="filePath">Path of the config file, '{def_folder}.config.json' next to the definitions folder.</param>
        /// <param name="refresher">Makes the new file show up in Unity. Defaults to <see cref="AssetDatabase.Refresh()"/>.</param>
        internal static void Create(string filePath, IAssetRefresher refresher = null)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(CreateDefault(), Formatting.Indented));
            Debug.Log($"'{Path.GetFileName(filePath)}' has been created at {filePath}");
            (refresher ?? UnityAssetRefresher.Instance).Refresh();
        }
    }
}
#endif