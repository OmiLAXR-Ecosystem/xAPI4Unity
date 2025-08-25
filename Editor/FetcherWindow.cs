/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Settings;
using xAPI4Unity.Editor.Types;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Editor window for managing xAPI definitions.
    /// Provides functionality for downloading, transforming, and editing JSON definition files.
    /// </summary>
    public class FetcherWindow : EditorWindow
    {
        private static MainSettings mainSettings => MainSettings.Instance;
        private bool _isDownloading;
        private Vector2 _scrollPosition = Vector2.zero;

        private static FetcherWindow Instance { get; set; }
        private LocalSourceView _localSourceView;

        /// <summary>
        /// Gets the local source view for managing local copies of the xAPI repository.
        /// </summary>
        public LocalSourceView LocalSourceView
        {
            get
            {
                _localSourceView ??= new LocalSourceView();
                return _localSourceView;
            }
        }
        
        private FetcherWindow() { }
        
        /// <summary>
        /// Determines whether JSON to C# transformation is possible.
        /// </summary>
        private static bool IsTransformationPossible => GitHelper.IsValidRepo(mainSettings.localSource.path);
        
        /// <summary>
        /// Opens the xAPI definitions setup window.
        /// </summary>
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

        /// <summary>
        /// Opens the xAPI registry web page in the default browser.
        /// </summary>
        [MenuItem("xAPI4Unity / Open registry page")]
        private static void OpenXApiRegistryPage()
        {
            Application.OpenURL("https://xapi.elearn.rwth-aachen.de/");
        }

        /// <summary>
        /// Downloads the latest xAPI patch into the local folder.
        /// </summary>
        [MenuItem("xAPI4Unity / Download newest 'xapi' patch into mine", priority = 100)]
        private static void UpdateXApiFolder()
        {
            var localSourceView = new LocalSourceView();
            MainSettings.Instance.Load();
            localSourceView.UpdateXApiFolder();
        }
        
        /// <summary>
        /// Quickly transforms JSON definitions into corresponding C# classes.
        /// </summary>
        [MenuItem("xAPI4Unity / Update xAPI.Registry (JSON => C#)", priority = 1000)]
        public static void QuickFetch() => ExecuteFetcher();
        
        private void OnGUI()
        {
            CustomGUI.Title("Using xAPI Definitions Fetcher with xAPI4Unity package");
            
            // Add description and options
            CustomGUI.Description("With this package you can download the needed xAPI repository definition files as JSON. Further, it transforms the JSON files into C# code you can use in your project. If Watcher is enabled, everything will be updated automatically."); 

            // Toggle for auto-opening the window on startup
            mainSettings.isOpeningOnStartup =
                CustomGUI.YesNoPopup("Open at load or startup on demand", mainSettings.isOpeningOnStartup, newValue =>
                {
                    mainSettings.isOpeningOnStartup = newValue;
                    MainSettings.Instance.Save();
                });            
            CustomGUI.Description("(Recommended Yes) If no xAPI Registry exists it opens this window on startup automatically.");

            // Scrollable region for GUI elements
            CustomGUI.ScrollView(ref _scrollPosition, () =>
            {
                // Section 1: Manage xAPI JSON Definitions
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("1) xAPI JSON Definitions Location");
                    CustomGUI.Description(
                        "Here you can get a local copy of the xAPI repository into your project. You can either work with a downloaded copy or use git instead.");
                    LocalSourceView.Show(() => { });
                });
                
                // Section 2: Transform JSON to C#
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("2) Do transformation!");
                    CustomGUI.Description("Use this section to transform JSON definitions into usable C# classes.");
                    
                    mainSettings.fetcher.watcherEnabled =
                        CustomGUI.YesNoPopup("Watcher Enabled", mainSettings.fetcher.watcherEnabled, newValue =>
                        {
                            mainSettings.fetcher.watcherEnabled = newValue;
                            MainSettings.Instance.Save();
                        });
                    CustomGUI.Description("(Recommended Yes)");
                    
                    // Execute transformation via button
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
                
                // Section 3: Using xAPI Definitions
                CustomGUI.Region(() =>
                {
                    CustomGUI.Subtitle("3) Work with xAPI Definitions");
                    CustomGUI.Description("Use C# files generated from xAPI JSON definitions in your project.");
                    CustomGUI.Hyperlink("https://xapi.elearn.rwth-aachen.de"); // Link to registry site for reference
                    
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
            // Save settings when the window is closed
            MainSettings.Instance.Save();
        }
        
        /// <summary>
        /// Executes the transformation of JSON files into C# classes.
        /// </summary>
        private static void ExecuteFetcher()
        {
            var definitionsPath = Path.Combine(mainSettings.localSource.path, "definitions");
            if (!Directory.Exists(definitionsPath))
            {
                Debug.LogError($"[xAPI4Unity] Cannot transform JSON files because '{definitionsPath}' doesn't exist.");
                return;
            }

            var folder = Path.GetDirectoryName(definitionsPath);
            var configFile = ReadConfigFile(folder);
            var aliases = new Aliases();
            var typeNameResolver = new TypeNameResolver(aliases);

            var typesSchema = ReadTypesSchema(configFile, typeNameResolver);

            if (typesSchema != null)
            {
                Debug.Log($"[xAPI4Unity] Parsed Types Schema for '{typesSchema.Count}' definitions..");
            }

            // Generate C# classes from JSON definitions
            Generator.Generate(
                mainSettings.@namespace, 
                definitionsPath, 
                configFile?.references, 
                typesSchema, 
                configFile?.ignoredContexts);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Debugs and validates the configuration file.
        /// </summary>
        [MenuItem("xAPI4Unity / Validate '*.config.json'", priority = 201)]
        private static void DebugTypes()
        {
            var configFile = ReadConfigFile(mainSettings.localSource.path);
            if (configFile == null)
            {
                Debug.LogError("Failed to read config file.");
                return;           
            }

            var aliases = new Aliases();
            var typeNameResolver = new TypeNameResolver(aliases);
            
            var typesSchema = ReadTypesSchema(configFile, typeNameResolver);
            if (typesSchema != null)
            {
                Debug.Log($"[xAPI4Unity] Parsed Types Schema for '{typesSchema.Count}' definitions..");
            }
            else
            {
                Debug.LogError("Type schema is null.");
            }

            foreach (var ts in typesSchema!)
            {
                var types = ts.Value.ResolveAll(typeNameResolver);
                Debug.Log(ts.Key + ":" + string.Join(",", types.Select(t => t.GetTypeSyntax())));
            }
        }

        /// <summary>
        /// Reads and deserializes the configuration file from a given path.
        /// </summary>
        private static ConfigFile ReadConfigFile(string path)
        {
            if (!Directory.Exists(path))
            {
                Debug.LogError($"[xAPI4Unity] Cannot find '{path}'.");
                return null;
            }

            var folderName = path.Replace('\\', '/').Split('/').Last();
            var typesFilename = folderName + ".config.json";
            var typesFilepath = Path.Combine(Directory.GetParent(path)!.FullName, typesFilename);

            if (!File.Exists(typesFilepath))
            {
                Debug.LogWarning($"[xAPI4Unity] Cannot find '{typesFilepath}'. Default types will be applied.");
                return null;
            }

            return ConfigFile.Read(typesFilepath);
        }

        /// <summary>
        /// Parses the type schema from a configuration file.
        /// </summary>
        private static TypeSchema ReadTypesSchema(ConfigFile configFile, TypeNameResolver resolver)
            => configFile == null ? null : TypeSchema.Parse(configFile?.Types, validateResolution: true, resolver);

        private static string SafePath(string path) => $"\"{path}\"";

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.LogError(e.Data);

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.Log(e.Data);

        protected void OnEnable()
        {
            // Load settings when the window is enabled
            MainSettings.Instance.Load();
        }
    }
}
#endif