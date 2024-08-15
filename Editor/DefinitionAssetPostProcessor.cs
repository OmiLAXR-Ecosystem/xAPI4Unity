#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Parser;

namespace xAPI4Unity.Editor
{
    internal class DefinitionAssetPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var settings = Settings.instance;

            if (!settings.fetcher.watcherEnabled)
                return;
            
            var definitionsPath = "";

            var changedAssets = importedAssets.Union(deletedAssets).Union(movedAssets);
            const string definitions = "/definitions";
            
            foreach (var asset in changedAssets)
            {
                var ext = Path.GetExtension(asset);
                var index = asset.IndexOf(definitions, StringComparison.Ordinal);
                if (ext != ".json" || index <= -1) 
                    continue;

                definitionsPath = Path.GetFullPath(asset.Substring(0, index));
                break;
            }

            if (string.IsNullOrEmpty(definitionsPath)) 
                return;
            

            if (string.IsNullOrEmpty(settings.localSource.path))
            {
                settings.localSource.path = definitionsPath;
                settings.Save();
            }

            Debug.Log("xAPI Definitions Path: " + definitionsPath);
            
            var @namespace = settings.@namespace;

            definitionsPath = Path.Combine(definitionsPath, "definitions");
            Generator.Generate(@namespace, definitionsPath);
        }
    }
}
#endif