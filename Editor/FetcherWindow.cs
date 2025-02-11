#if UNITY_EDITOR
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
        private static Settings Settings => Settings.Instance;
        private bool _isDownloading;
        
        private Vector2 _scrollPosition = Vector2.zero;

        private static FetcherWindow Instance { get; set; }

        private readonly LocalSourceView _localSourceView = new LocalSourceView(Settings);

        private static bool IsTransformationPossible => GitHelper.IsValidRepo(Settings.localSource.path);
        
        private FetcherWindow()
        {
        }
        
        [MenuItem("xAPI4Unity / Open Fetcher Window (Settings)")]
        public static void ShowWindow()
        {
            const string title = "xAPI4Unity";
            Instance = GetWindow<FetcherWindow>(title);
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
            Instance.titleContent = new GUIContent(title);
#else
            Instance.title = title;
#endif
            Instance.minSize = new Vector2(1000, 370);
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
            
            CustomGUI.Description("With this package you can download the needed xAPI repository definition files as json. Further it transforms the JSON files into C# code you can use in your project. If you want to create a new definition file, just create a new json file and this tool does the rest. If Watcher is enabled, everything will be updated automatically.");

            // CustomGUI.Horizontal.Wrap(() =>
            // {
            //     CustomGUI.Disabled(!Settings.IsDirty(), () =>
            //     {
            //         if (CustomGUI.Button("Save settings"))
            //         {
            //             GUI.FocusControl(null);
            //             Settings.Instance.Save();
            //         }
            //
            //     });
            //
            //     if (CustomGUI.Button("Reset to default"))
            //     {
            //         Settings.Instance.Reset();
            //         Settings.Instance.Load();
            //     }
            //     
            //     CustomGUI.Space();
            // });
            //
            // CustomGUI.Divider(2);
            
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
                        CustomGUI.YesNoPopup("Watcher Enabled", Settings.fetcher.watcherEnabled, _ =>
                        {
                            Settings.Instance.Save();
                        });
                    CustomGUI.Description("(Recommended Yes)");
                    CustomGUI.Warning("If watcher is enabled, than the transformation is done each time when you change a JSON definition file.");
                });
                
                CustomGUI.Horizontal.Wrap(() =>
                {
                    CustomGUI.Disabled(!IsTransformationPossible, () => {
                        if (CustomGUI.Button("Update xAPI.Registry (JSON => C#)",
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
            Settings.Instance.Save();
        }
        
        /// <summary>
        /// Triggers to run the CLI command to transforms JSON to C#.
        /// </summary>
        private static void ExecuteFetcher()
        {
            Generator.Generate(Settings.@namespace, Path.Combine(Settings.localSource.path, "definitions"));
            AssetDatabase.Refresh();
        }

        private static string SafePath(string path) => $"\"{path}\"";

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.LogError(e.Data);

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.Log(e.Data);

        protected void OnEnable() => Settings.Instance.Load();
    }
}
#endif