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
        private readonly LocalSourceView _localSourceView = new LocalSourceView();
        
        private FetcherWindow()
        {
        }
        
        private static bool IsTransformationPossible => GitHelper.IsValidRepo(Settings.localSource.path);
        
        [MenuItem("xAPI4Unity / Setup xAPI definitions", priority = 0)]
        public static void ShowWindow()
        {
            const string title = "xAPI4Unity";
            Instance = GetWindow<FetcherWindow>(title);
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
            Instance.titleContent = new GUIContent(title);
#else
            Instance.title = title;
#endif
            Instance.minSize = new Vector2(1000, 800);
            Instance.Show(true);
        }

        [MenuItem("xAPI4Unity / Open registry page")]
        public static void OpenXApiRegistryPage()
        {
            Application.OpenURL("https://xapi.elearn.rwth-aachen.de/");
        }
        
        
        [MenuItem("xAPI4Unity / Update xAPI.Registry (JSON => C#)", priority = 2)]
        public static void QuickFetch()
            => ExecuteFetcher();
        
        private void OnGUI()
        {
            CustomGUI.Title("Using xAPI Definitions Fetcher with xAPI4Unity package");
            
            CustomGUI.Description("With this package you can download the needed xAPI repository definition files as json. Further it transforms the JSON files into C# code you can use in your project. If you want to create a new definition file, just create a new json file and this tool does the rest. If Watcher is enabled, everything will be updated automatically.");
            
            Settings.isOpeningOnStartup =
                CustomGUI.YesNoPopup("Open at load on demand", Settings.isOpeningOnStartup , newValue =>
                {
                    Settings.isOpeningOnStartup = newValue;
                    Settings.Instance.Save();
                });            
            CustomGUI.Description("(Recommended Yes) If no xAPI Registry exists it opens this window on startup automatically.");

            CustomGUI.ScrollView(ref _scrollPosition, () =>
            {
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("1) xAPI JSON Definitions Location");

                    CustomGUI.Description(
                        "Here you can get a local copy of xAPI repository into your project. You can either work with a downloaded copy or use git instead. In case of git, you will ne write permissions in the git repository to at least a branch. Alternatively you can use your git fork repository.");

                    _localSourceView.Show(() =>
                    {
                        ExecuteFetcher();
                    });
                });
                
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("2) Do transformation!");
                    CustomGUI.Description("Now, you have a local copy of the xAPI registry repository. It contains several JSON files which define the xAPI registry.");
                    CustomGUI.Description("The next step is to transform them into C# code which you can use in your project. We strongly recommend to enable the watcher.");
                    CustomGUI.Description("This feature listen for changes in the JSON files automatically and trigger the transformation on each change automatically.");
                    CustomGUI.Description("If you don't want to use the watcher, you can trigger the transformation manually by clicking on the button below.");
                    Settings.fetcher.watcherEnabled =
                        CustomGUI.YesNoPopup("Watcher Enabled", Settings.fetcher.watcherEnabled, newValue =>
                        {
                            Settings.fetcher.watcherEnabled = newValue;
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
                
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("3) Work with xAPI Definitions");
                    CustomGUI.Description("Now, you have C# files you can use in your project to define xAPI statements. For creating new definitions or edit existing you have two options:");
                    CustomGUI.Description("For searching fast for definitions you can also use our registry website.");
                    CustomGUI.Hyperlink("https://xapi.elearn.rwth-aachen.de");
                    CustomGUI.Description("1) Create a new JSON file in the 'definitions' folder structure of your local copy of the xAPI registry according the folder rules or you can edit existing files. This can be done with your IDE.");
                    CustomGUI.Description("2) Use our 'xAPI Definitions Editor' window for create new definitions or for changing existing. You can reopen this window anytime on menu 'xAPI4Unity > Edit xAPI definitions'.");
                    if (CustomGUI.Button("Open xAPI Definitions Editor", GUILayout.MaxWidth(1000), GUILayout.Height(50),
                            GUILayout.ExpandWidth(true)))
                    {
                        XapiDefinitionsEditorWindow.ShowWindow();
                    }
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