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
        private const string RegistryUrl = "https://xapi.elearn.rwth-aachen.de/";
        private const string RegistryDefine = "XAPI_REGISTRY_EXISTS";
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
            Instance.minSize = new Vector2(480, 500);
            Instance.Show(true);
        }

        /// <summary>
        /// Opens the xAPI registry web page in the default browser.
        /// </summary>
        [MenuItem("xAPI4Unity / Open registry page")]
        private static void OpenXApiRegistryPage()
        {
            Application.OpenURL(RegistryUrl);
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
            // Scrollable region for GUI elements
            CustomGUI.ScrollView(ref _scrollPosition, () =>
            {
                CustomGUI.Title("xAPI4Unity");
                CustomGUI.Description(
                    $"Turns xAPI JSON definitions into C# classes in 'Assets/{mainSettings.@namespace}'. With 'Regenerate on changes' on, the classes are updated whenever a definition changes.");

                CustomGUI.Section("Status", StatusView);

                // Section 1: Manage xAPI JSON Definitions
                CustomGUI.Section("1) Source", () =>
                {
                    CustomGUI.Description("Where the xAPI JSON definitions come from. 'Path' must contain a 'definitions' folder.");
                    LocalSourceView.Show(() => { });
                });

                // Section 2: Transform JSON to C#
                CustomGUI.Section("2) Generate", () =>
                {
                    mainSettings.fetcher.watcherEnabled =
                        CustomGUI.Toggle("Regenerate on changes", mainSettings.fetcher.watcherEnabled, newValue =>
                        {
                            mainSettings.fetcher.watcherEnabled = newValue;
                            MainSettings.Instance.Save();
                        }, "Regenerates the C# classes whenever a JSON file in a 'definitions' folder changes. Recommended.");
                    CustomGUI.Space();

                    // Execute transformation via button
                    CustomGUI.Disabled(!IsTransformationPossible, () => {
                        if (CustomGUI.Button("Update xAPI.Registry (JSON => C#)",
                                GUILayout.Height(28),
                                GUILayout.ExpandWidth(true)))
                        {
                            GUI.FocusControl(null);
                            ExecuteFetcher();
                        }
                    });
                });

                // Section 3: Using xAPI Definitions
                CustomGUI.Section("3) Work with definitions", () =>
                {
                    if (CustomGUI.Button("Open definitions editor", GUILayout.Height(28)))
                    {
                        XapiDefinitionsEditorWindow.ShowWindow();
                    }
                    CustomGUI.Hyperlink("Browse the xAPI registry website", RegistryUrl);
                });

                CustomGUI.Space();
                CustomGUI.Foldout("Settings", "FetcherWindow.Settings", () =>
                {
                    // Toggle for auto-opening the window on startup
                    mainSettings.isOpeningOnStartup =
                        CustomGUI.Toggle("Open on startup", mainSettings.isOpeningOnStartup, newValue =>
                        {
                            mainSettings.isOpeningOnStartup = newValue;
                            MainSettings.Instance.Save();
                        }, "Opens this window when the editor starts and the definitions or the registry are missing. Recommended.");
                });
            });
        }

        /// <summary>
        /// Shows whether the definitions, the generated registry and its scripting define exist.
        /// </summary>
        private static void StatusView()
        {
            var sourcePath = mainSettings.localSource.path;
            var definitionsPath = string.IsNullOrEmpty(sourcePath) ? null : Path.Combine(sourcePath, "definitions");
            var registryPath = Path.Combine(Application.dataPath, mainSettings.@namespace);

            var hasDefinitions = definitionsPath != null && Directory.Exists(definitionsPath);
            var hasRegistry = Directory.Exists(registryPath);
            var hasDefine = Startup.DefineExist(RegistryDefine);

            CustomGUI.Step("JSON definitions", hasDefinitions,
                hasDefinitions ? ToDisplayPath(definitionsPath) : "No 'definitions' folder at 'Path'. Get it in 1).");
            CustomGUI.Step("C# registry", hasRegistry,
                hasRegistry ? ToDisplayPath(registryPath) : "Not generated yet. Run 2).");
            CustomGUI.Step(RegistryDefine, hasDefine,
                hasDefine ? "Set" : "Not set. OmiLAXR.xAPI sets it once the registry exists.");
        }

        /// <summary>
        /// Shortens a path inside the Unity project to a project-relative path.
        /// </summary>
        private static string ToDisplayPath(string path)
        {
            var fullPath = Path.GetFullPath(path).Replace('\\', '/');
            var projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/') + "/";
            return fullPath.StartsWith(projectRoot) ? fullPath.Substring(projectRoot.Length) : fullPath;
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
            => configFile == null ? null : TypeSchema.Parse(configFile?.Types, validateResolution: true, resolver, configFile?.Variables, true);

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