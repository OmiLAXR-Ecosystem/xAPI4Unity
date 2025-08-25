/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Parser;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Handles asset changes related to xAPI definitions and triggers updates when files are modified.
    /// </summary>
    internal class DefinitionAssetPostProcessor : AssetPostprocessor
    {
        /// <summary>
        /// Processes assets after they are imported, moved, or deleted.
        /// </summary>
        /// <param name="importedAssets">List of imported asset paths.</param>
        /// <param name="deletedAssets">List of deleted asset paths.</param>
        /// <param name="movedAssets">List of moved asset paths.</param>
        /// <param name="movedFromAssetPaths">List of original paths for moved assets.</param>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Access xAPI4Unity settings
            var settings = Settings.MainSettings.Instance;

            // Exit if the file watcher (watcherEnabled) is disabled in settings
            if (!settings.fetcher.watcherEnabled)
                return;
            
            var definitionsPath = "";

            // Collect all assets that have changed (imported, deleted, or moved)
            var changedAssets = importedAssets.Union(deletedAssets).Union(movedAssets);
            const string definitions = "/definitions";
            
            foreach (var asset in changedAssets)
            {
                // Check if the asset is a JSON file located under the "definitions" directory
                var ext = Path.GetExtension(asset);
                var index = asset.IndexOf(definitions, StringComparison.Ordinal);
                if (ext != ".json" || index <= -1) 
                    continue;

                // Extract the full path to the "definitions" directory
                definitionsPath = Path.GetFullPath(asset.Substring(0, index));
                break;
            }

            // If no relevant definitions directory was identified, exit
            if (string.IsNullOrEmpty(definitionsPath)) 
                return;

            // Set the local source path in settings, saving it if not already set
            if (string.IsNullOrEmpty(settings.localSource.path))
            {
                settings.localSource.path = definitionsPath;
                settings.Save();
            }

            Debug.Log("xAPI Definitions Path: " + definitionsPath);
            
            var @namespace = settings.@namespace;

            // Expand the path by appending "/definitions" and check if it exists
            definitionsPath = Path.Combine(definitionsPath, "definitions");
            if (!Directory.Exists(definitionsPath))
                return;

            // Trigger code generation for the identified definitions
            Generator.Generate(@namespace, definitionsPath);
            Debug.Log("Generated " + definitionsPath);
        }
    }
}
#endif