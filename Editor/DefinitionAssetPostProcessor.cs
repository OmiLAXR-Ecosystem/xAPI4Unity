#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor
{
    public class DefinitionAssetPostProcessor : AssetPostprocessor
    {
        static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        static (string, string, string) EnsureFolders(string @namespace)
        {
            var rootFolder = Path.Combine(Application.dataPath, @namespace);
            var coreFolder = Path.Combine(rootFolder, "Core");
            var definitionsFolder = Path.Combine(rootFolder, "Definitions");
            
            EnsureFolder(rootFolder);
            EnsureFolder(coreFolder);
            EnsureFolder(definitionsFolder);

            return (rootFolder, coreFolder, definitionsFolder);
        }

        private void OnPreprocessAsset()
        {
            Debug.Log("on preprocess");
        }

        

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

            var fm = new FileManager(definitionsPath);
            var definitionsFiles = fm.GetAllEntries();

            var (rootFolder, coreFolder, definitionsFolder) = EnsureFolders(@namespace);
            CoreFiles.CopyFiles(@namespace, coreFolder);
            
            DefinitionsFiles.UpdateFiles(@namespace, definitionsFiles, definitionsFolder);
            
            CoreFiles.CreateAsmDefFile(@namespace, rootFolder);
                
            Debug.Log("[xAPI 4 Unity]: Created xAPI Registry with namespace " + @namespace);
        }
    }
}
#endif