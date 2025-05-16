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
        const string XAPI_REGISTRY_EXISTS = "XAPI_REGISTRY_EXISTS";
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check if the folder was created or deleted
            foreach (var path in importedAssets)
            {
                if (path == "Assets/xAPI.Registry" && AssetDatabase.IsValidFolder(path))
                {
                    DefineUtility.AddXapiRegistryExistsDefine(XAPI_REGISTRY_EXISTS);
                    Debug.Log("xAPI.Registry folder created! \"XAPI_REGISTRY_EXISTS\" was set.");
                }
            }
            foreach (var path in deletedAssets)
            {
                if (path == "Assets/xAPI.Registry")
                {
                    DefineUtility.RemoveXapiRegistryExistsDefine(XAPI_REGISTRY_EXISTS);
                    Debug.Log("xAPI.Registry folder deleted! \"XAPI_REGISTRY_EXISTS\" was removed.");
                }
            }
            
            var settings = Settings.Instance;

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
            Debug.Log("Generated " + definitionsPath);
        }
    }
}
#endif