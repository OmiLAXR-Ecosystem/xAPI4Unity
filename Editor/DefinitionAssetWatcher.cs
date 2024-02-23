using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_EDITOR
namespace xAPI4Unity.Editor
{
    public class DefinitionAssetWatcher : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            Debug.Log("OnWillSaveAssets " + string.Join(',', paths));
            return paths;
        }
        private static bool IsOpenForEdit(string[] paths, List<string> outNotEditablePaths, StatusQueryOptions statusQueryOptions)
        {
            Debug.Log("IsOpenForEdit: " + string.Join(',', paths) + " " + statusQueryOptions);
            return true;
        }
        static void OnWillCreateAsset(string assetName)
        {
            Debug.Log("OnWillCreateAsset is being called with the following asset: " + assetName + ".");
        }

        private static void FileModeChanged(string[] paths, FileMode mode)
        {
            Debug.Log($"{nameof(FileModeChanged)} ({mode}):" + string.Join(',', paths));
        }
        
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            var assetPath = AssetDatabase.GetAssetPath(instanceID);
            //var text = File.ReadAllText(assetPath);
            Debug.Log("OnOpenAsset");
            return false;
        }
        
        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            Debug.Log("DidReloadScripts");

        }

        [PostProcessBuild]
        private static void PostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
        }

        [PostProcessScene]
        private static void PostProcessScene()
        {
        }

    }
}
#endif