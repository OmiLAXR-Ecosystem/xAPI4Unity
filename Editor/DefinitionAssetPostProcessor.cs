#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
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

       
        
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            var @namespace = "xAPI.Registry";
            var xapiPath = Path.Combine(Application.dataPath, "xapi");
            var definitionsPath = Path.Combine(xapiPath, "definitions");

            var fm = new FileManager(definitionsPath);
            foreach (var c in fm.GetContexts())
            {
                Debug.Log(c);
            }

            var (rootFolder, coreFolder, definitionsFolder) = EnsureFolders(@namespace);
            CoreFiles.CopyFiles(@namespace, coreFolder);
            CoreFiles.CreateAsmDefFile(@namespace, rootFolder);
            
            foreach (string str in importedAssets)
            {
                Debug.Log("Reimported Asset: " + str);
            }
            
            foreach (string str in deletedAssets)
            {
                Debug.Log("Deleted Asset: " + str);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }

            if (didDomainReload)
            {
                Debug.Log("Domain has been reloaded");
            }
        }
    }
}
#endif