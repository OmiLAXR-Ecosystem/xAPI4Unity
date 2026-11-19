/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
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
        /// <summary>
        /// The settings of this project.
        /// </summary>
        private static MainSettings mainSettings => MainSettings.Instance;
        /// <summary>
        /// Website of the xAPI registry.
        /// </summary>
        private const string RegistryUrl = "https://omilaxr-ecosystem.github.io/xapi/";

        /// <summary>
        /// Lazily created view for <see cref="LocalSourceView"/>.
        /// </summary>
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
        
        /// <summary>
        /// Created by Unity only. Use <see cref="ShowWindow"/>.
        /// </summary>
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
        
        /// <summary>
        /// Only shown when a saved editor layout still contains this window: links to the wizard.
        /// </summary>
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

        /// <summary>
        /// Saves the settings when the window closes.
        /// </summary>
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
            // Reads the types config next to the source folder and generates Assets/<namespace>
            if (Generator.Update(RegistryPaths.FromSettings(mainSettings)))
                AssetDatabase.Refresh();
        }

        /// <summary>
        /// Debugs and validates the configuration file.
        /// </summary>
        [MenuItem("xAPI4Unity / Validate '*.config.json'", priority = 201)]
        internal static void DebugTypes()
        {
            var typeNameResolver = new TypeNameResolver(new Aliases());
            var update = Generator.CreateUpdate(typeNameResolver);
            var configFile = update.ReadConfig(RegistryPaths.FromSettings(mainSettings));
            if (configFile == null)
            {
                Debug.LogError("Failed to read config file.");
                return;           
            }

            var typesSchema = update.ReadTypes(configFile);
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
        /// Loads the settings when the window opens.
        /// </summary>
        protected void OnEnable()
        {
            // Load settings when the window is enabled
            MainSettings.Instance.Load();
        }
    }
}
#endif