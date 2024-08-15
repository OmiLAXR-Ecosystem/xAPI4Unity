#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Parser;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    public class FetcherWindow : EditorWindow
    {
        private static Settings Settings => Settings.instance;
        private bool _isDownloading;
        
        private Vector2 _scrollPosition = Vector2.zero;

        public static FetcherWindow Instance { get; private set; }

        private readonly LocalSourceView _localSourceView = new LocalSourceView(Settings);

        private static bool IsTransformationPossible => GitHelper.IsValidRepo(Settings.localSource.path);
        
        public event OnTriggeredControlHandler OnTriggeredControl;

        private FetcherWindow()
        {
            _localSourceView.OnTriggeredControl += FireTriggeredControl;
        }

        private void FireTriggeredControl(Type type, string label, string value)
            => OnTriggeredControl?.Invoke(type, label, value);
        
        [MenuItem("xAPI4Unity / Settings")]
        public static void ShowWindow()
        {
            Instance = GetWindow<FetcherWindow>("xAPI4Unity");
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
            Instance.titleContent = new GUIContent("xAPI4Unity");
#else
            Instance.title = "xAPI4Unity";
#endif
            Instance.minSize = new Vector2(1000, 770);
            Instance.Show(true);
        }

        [MenuItem("xAPI4Unity / Open registry page")]
        public static void OpenXApiRegistryPage()
        {
            Application.OpenURL("https://xapi.elearn.rwth-aachen.de/");
        }
        
        [MenuItem("xAPI4Unity / Update xAPI.Registry (JSON => C#)")]
        public static void QuickFetch()
            => ExecuteFetcher();
        
        private void OnGUI()
        {
            CustomGUI.Title("Using xAPI Definitions Fetcher with xAPI4Unity package");

            CustomGUI.Horizontal.Wrap(() =>
            {
                CustomGUI.Disabled(!Settings.IsDirty(), () =>
                {
                    if (CustomGUI.Button("Save settings", OnTriggeredControl))
                    {
                        GUI.FocusControl(null);
                        Settings.instance.Save();
                    }

                });

                if (CustomGUI.Button("Reset to default", OnTriggeredControl))
                {
                    Settings.instance.Reset();
                    Settings.instance.Load();
                }
                
                CustomGUI.Space();
            });
            
            CustomGUI.Divider(2);
            
            CustomGUI.ScrollView(ref _scrollPosition, () =>
            {
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("1) xAPI JSON Definitions Location");

                    CustomGUI.Description(
                        "Here you can get a local copy of xAPI repository into your project. You can either work with a downloaded copy or use git instead. In case of git, you will ne write permissions in the git repository to at least a branch. Alternatively you can use your git fork repository.");

                    _localSourceView.Show();
                });
                
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("2) Do transformation!");
                    Settings.fetcher.watcherEnabled =
                        CustomGUI.YesNoPopup("Watcher Enabled", Settings.fetcher.watcherEnabled);
                    CustomGUI.Description("(Recommended Yes)");
                    CustomGUI.Warning("If watcher is enabled, than the transformation is done each time when you change a JSON definition file.");
                });
                
                CustomGUI.Horizontal.Wrap(() =>
                {
                    CustomGUI.Disabled(!IsTransformationPossible, () => {
                        if (CustomGUI.Button("Update xAPI.Registry (JSON => C#)", OnTriggeredControl, 
                                GUILayout.MaxWidth(1000), 
                                GUILayout.Height(50), 
                                GUILayout.ExpandWidth(true)))
                        {
                            GUI.FocusControl(null);
                            ExecuteFetcher();
                        }
                    });
                });
 
               
            });
      
        }
        

        private void OnDisable()
        {
            if (!Settings.IsDirty())
                return;
            // Ask the user if they really want to close the window
            var wantsSave = EditorUtility.DisplayDialog("Saving your settings?", "Do you want to save your settings before closing this window?", "Yes", "No");
        
            if (wantsSave)
                Settings.instance.Save();
        }
        
        /// <summary>
        /// Triggers to run the CLI command to transforms JSON to C#.
        /// </summary>
        public static void ExecuteFetcher()
        {
            Generator.Generate(Settings.@namespace, Path.Combine(Settings.localSource.path, "definitions"));
            AssetDatabase.Refresh();
        }

        private static string SafePath(string path) => $"\"{path}\"";

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.LogError(e.Data);

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.Log(e.Data);

        protected void OnEnable() => Settings.instance.Load();
    }
}
#endif