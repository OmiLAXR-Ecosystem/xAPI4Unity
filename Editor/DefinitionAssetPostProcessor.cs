#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using xAPI4Unity.Editor.Parser;

namespace xAPI4Unity.Editor
{
    public class DefinitionAssetPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var definitionsPath = "";

            var changedAssets = importedAssets.Union(deletedAssets).Union(movedAssets);
            const string definitions = "/definitions";
            
            foreach (var asset in changedAssets)
            {
                var ext = Path.GetExtension(asset);
                var index = asset.IndexOf(definitions, StringComparison.Ordinal);
                if (ext != ".json" || index <= -1) 
                    continue;

                definitionsPath = Path.GetFullPath(asset.Substring(0, index + definitions.Length));
                break;
            }

            if (string.IsNullOrEmpty(definitionsPath)) 
                return;
            
            var @namespace = "xAPI.Registry";

            Generator.Generate(@namespace, definitionsPath);
        }
    }
}
#endif