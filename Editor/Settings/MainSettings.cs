/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace xAPI4Unity.Editor.Settings
{
    /// <summary>
    /// Interface for settings classes to implement cloning and equality check.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Clones the current settings instance.
        /// </summary>
        /// <typeparam name="T">The type of the clone.</typeparam>
        T Clone<T>();

        /// <summary>
        /// Compares the current settings instance with another settings object.
        /// </summary>
        /// <param name="s">The settings object to compare.</param>
        /// <returns>True if both settings are equal, otherwise false.</returns>
        bool Equals(ISettings s);
    }

    /// <summary>
    /// Holds configuration settings for xAPI4Unity.
    /// </summary>
    [Serializable]
    public class MainSettings : ISettings
    {
        /// <summary>
        /// A unique key used for saving and retrieving settings in EditorPrefs.
        /// </summary>
        private static string SettingsKey
        {
            get
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Application.dataPath));
                var shortHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return $"xAPI4Unity::{shortHash}";
            }
        }

        /// <summary>
        /// Singleton reference to the settings instance.
        /// Automatically loads settings if not initialized.
        /// </summary>
        private static MainSettings _instance;
        public static MainSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MainSettings();
                    _instance.Load(); // Load settings from EditorPrefs
                }
                return _instance;
            }
        }

        [SerializeReference] public FetcherSettings fetcher = new FetcherSettings();
        [SerializeReference] public LocalSourceSettings localSource = new LocalSourceSettings();
        [SerializeField] public string @namespace = DefaultValues.Namespace;
        [SerializeField] public bool isOpeningOnStartup = true;

        /// <summary>
        /// Gets the Assembly Definition (asmdef) filename based on the namespace.
        /// </summary>
        public string AsmDefFilename => @namespace + ".asmdef";

        /// <summary>
        /// Gets the meta filename of the asmdef file.
        /// </summary>
        public string AsmDefMetaFilename => AsmDefFilename + ".meta";

        /// <summary>
        /// Resets the settings by clearing stored values in EditorPrefs.
        /// </summary>
        public void Reset()
        {
            EditorPrefs.DeleteKey(SettingsKey);
            _instance = new MainSettings();
        }

        /// <inheritdoc />
        public T Clone<T>()
        {
            var s = new MainSettings
            {
                fetcher = fetcher.Clone<FetcherSettings>(),
                localSource = localSource.Clone<LocalSourceSettings>(),
                @namespace = @namespace,
                isOpeningOnStartup = isOpeningOnStartup
            };
            return (T)(object)s;
        }

        /// <inheritdoc />
        public bool Equals(ISettings so)
        {
            if (so == null) return false;
            var s = so as MainSettings;
            return s!.isOpeningOnStartup == isOpeningOnStartup &&
                   s.@namespace == @namespace &&
                   s.AsmDefFilename == AsmDefFilename &&
                   s.AsmDefMetaFilename == AsmDefMetaFilename &&
                   fetcher.Equals(s.fetcher) &&
                   localSource.Equals(s.localSource);
        }

        /// <summary>
        /// Loads the settings from Unity EditorPrefs or initializes default values if unavailable.
        /// </summary>
        public void Load()
        {
            var defaultJson = JsonUtility.ToJson(Instance, false); // Default settings as JSON
            var data = EditorPrefs.GetString(SettingsKey, defaultJson); // Retrieve settings from EditorPrefs

            try
            {
                // Try applying the JSON data to the current instance
                JsonUtility.FromJsonOverwrite(data, Instance);
            }
            catch
            {
                _instance = new MainSettings(); // Reinitialize on failure
            }

            // Ensure nested settings objects are not null
#if UNITY_2020_1_OR_NEWER
            Instance.fetcher ??= new FetcherSettings();
            Instance.localSource ??= new LocalSourceSettings();
#else
            if (Instance.fetcher == null) Instance.fetcher = new FetcherSettings();
            if (Instance.localSource == null) Instance.localSource = new LocalSourceSettings();
#endif
        }

        /// <summary>
        /// Saves the current settings to Unity EditorPrefs.
        /// </summary>
        public void Save()
        {
            var json = JsonUtility.ToJson(Instance, false); // Serialize settings to JSON
            EditorPrefs.SetString(SettingsKey, json); // Save to EditorPrefs
        }
    }
}
#endif