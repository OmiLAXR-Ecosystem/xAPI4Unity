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
using xAPI4Unity.Editor.Wizard;
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
        private const string RegistryUrl = "https://omilaxr-ecosystem.github.io/xapi/";
        private bool _isDownloading;

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
        /// Opens the xAPI4Unity wizard, which replaces this window.
        /// </summary>
        public static void ShowWindow() => XapiWizardWindow.Open();

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
            // Only reached when an editor layout still contains this window
            EditorGUILayout.HelpBox("The xAPI4Unity setup moved into the wizard.", MessageType.Info);
            if (GUILayout.Button("Open wizard"))
            {
                XapiWizardWindow.Open();
                Close();
            }
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
        internal static void DebugTypes()
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