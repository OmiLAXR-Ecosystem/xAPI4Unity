using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Analytics;
using UnityEditor.Callbacks;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    [InitializeOnLoad]
    public class GUIWatcher : EditorWindow
    {
        private static int hotControl = 0;
        private static string Minify(string code)
        {
            var pattern = new Regex("[\t\r ]|[\n]{2}");
            pattern.Replace(code, "");
            return code;
        }
        
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Log("Opened Asset: " + AssetDatabase.GetAssetPath(instanceID));
            var assetPath = AssetDatabase.GetAssetPath(instanceID);
            var text = System.IO.File.ReadAllText(assetPath);
            Log(Minify(text));

            return false;
        }

        static void MyCallback(string path, Rect r)
        {
            Log("Created Asset: " + path);
        }
        
        static GUIWatcher()
        {
            Log(AnalyticsSettings.configUrl);
            var myCallback = new EditorApplication.ProjectWindowItemCallback(MyCallback);
            
            EditorApplication.update += Update;
            EditorApplication.delayCall += AfterAllInspectionUpdate;
            EditorApplication.searchChanged += SearchChanged;
            EditorApplication.pauseStateChanged += EditorApplicationOnpauseStateChanged;
            EditorApplication.hierarchyChanged += EditorApplicationOnHierarchyChanged;
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
            EditorApplication.quitting += EditorApplicationOnQuitting;
            EditorApplication.projectChanged += EditorApplicationOnProjectChanged;
            
        }

        private static void EditorApplicationOnProjectChanged()
        {
            Log("EditorApplicationOnProjectChanged");
        }

        private static void EditorApplicationOnQuitting()
        {
            Log("EditorApplicationOnQuitting");
        }

        private static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange obj)
        {
            Log(obj);
        }

        private static void EditorApplicationOnHierarchyChanged()
        {
            var all = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            var m_NumberVisible = all.Count(obj => (obj.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy);

            Log("Hirarchy changed " + m_NumberVisible);
        }

        private static void EditorApplicationOnpauseStateChanged(PauseState obj)
        {
            Log(obj);
        }

        private static void SearchChanged()
        {
            Log("SearchChanged");
        }

        private static void AfterAllInspectionUpdate()
        {
            Log("AfterAllInspectionUpdate");
        }

        private static void Update()
        {
            //Log("Update");
            if (GUIUtility.hotControl == hotControl)
                return;
            Log(GUIUtility.hotControl);
            hotControl = GUIUtility.hotControl;
        }

        private static void Log(object message) => Debug.Log(message.ToString());
    }
}
