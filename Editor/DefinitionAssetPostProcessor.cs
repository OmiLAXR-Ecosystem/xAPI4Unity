#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor
{
    public class DefinitionAssetPostProcessor : AssetPostprocessor
    {
        static void EnsureFolders()
        {
            Folders.EnsureRootFolder("xAPI.Registry", Application.dataPath);
        }
        
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            EnsureFolders();
            
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