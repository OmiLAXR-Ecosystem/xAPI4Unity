/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine;
using xAPI4Unity.Editor.Settings;
using xAPI4Unity.Editor.Wizard;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build; 
#endif

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Executes initialization logic for xAPI4Unity when the Unity Editor starts.
    /// </summary>
    [InitializeOnLoad]
    internal static class Startup
    {
        /// <summary>
        /// Checks if a specific scripting define symbol exists in the current build configuration.
        /// </summary>
        /// <param name="define">The scripting define symbol to search for.</param>
        /// <returns>True if the define exists; otherwise, false.</returns>
        internal static bool DefineExist(string define)
        {
#if UNITY_2021_2_OR_NEWER
            // For Unity 2021.2 or newer, use NamedBuildTarget to get the defines
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var defines = PlayerSettings
                .GetScriptingDefineSymbols(namedBuildTarget)
                .Split(';');
#else
            // For Unity versions before 2021.2, use the older API
            var defines = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';');
#endif

            return defines.Contains(define);
        }
        
        /// <summary>
        /// Static constructor runs automatically when Unity Editor loads.
        /// Checks configuration and optionally opens the wizard if necessary.
        /// </summary>
        static Startup()
        {
            // Windows cannot open in batch mode
            if (Application.isBatchMode)
                return;

            // Load main settings
            var settings = MainSettings.Instance;

            // Exit if auto-open on startup is disabled
            if (!settings.isOpeningOnStartup)
                return;

            // Delay the execution until the Editor is ready
            EditorApplication.delayCall += () =>
            {
                // XAPI_REGISTRY_EXISTS is not checked here: OmiLAXR.xAPI sets it once the registry exists
                var hasRegistry = Directory.Exists(Path.Combine(Application.dataPath, settings.@namespace));
                if (!hasRegistry || !GitHelper.HasDefinitionsFolder(settings.localSource.path))
                {
                    // Open the wizard to guide the user
                    XapiWizardWindow.Open();
                }
            };
        }
    }
}