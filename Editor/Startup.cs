/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using UnityEditor;
using System.Linq;
using xAPI4Unity.Editor.Settings;

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
        private static bool DefineExist(string define)
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
        /// Checks configuration and optionally opens the Fetcher Window if necessary.
        /// </summary>
        static Startup()
        {
            // Load main settings
            var settings = MainSettings.Instance;

            // Exit if auto-open on startup is disabled
            if (!settings.isOpeningOnStartup)
                return;

            // Delay the execution until the Editor is ready
            EditorApplication.delayCall += () =>
            {
                // Check if the "XAPI_REGISTRY_EXISTS" define exists or if the xAPI definitions folder is missing
                if (!DefineExist("XAPI_REGISTRY_EXISTS") || !GitHelper.HasDefinitionsFolder(settings.localSource.path))
                {
                    // Open the Fetcher Window to guide the user
                    FetcherWindow.ShowWindow();
                }
            };
        }
    }
}