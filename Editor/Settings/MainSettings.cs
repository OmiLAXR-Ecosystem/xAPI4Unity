/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
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
        /// Where <see cref="Load()"/>, <see cref="Save()"/> and <see cref="Reset"/> keep the settings.
        /// </summary>
        internal static ISettingsStore DefaultStore => EditorPrefsSettingsStore.Instance;

        /// <summary>
        /// A unique key used for saving and retrieving settings in EditorPrefs.
        /// </summary>
        internal static string SettingsKey
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
        /// <summary>
        /// The settings of this project, loaded from EditorPrefs on first use.
        /// </summary>
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

        /// <summary>
        /// Settings of the file watcher.
        /// </summary>
        [SerializeReference] public FetcherSettings fetcher = new FetcherSettings();
        /// <summary>
        /// Where the definitions come from.
        /// </summary>
        [SerializeReference] public LocalSourceSettings localSource = new LocalSourceSettings();
        /// <summary>
        /// Namespace of the generated registry, also its folder in <c>Assets</c>.
        /// </summary>
        [SerializeField] public string @namespace = DefaultValues.Namespace;
        /// <summary>
        /// Open the wizard on editor start when the definitions or the registry are missing.
        /// </summary>
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
            DefaultStore.DeleteKey(SettingsKey);
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
        public void Load() => Load(DefaultStore);

        /// <summary>
        /// Loads the settings from a store into this instance. Keeps the current values
        /// if the store has none, and falls back to defaults if the stored JSON is broken.
        /// </summary>
        internal void Load(ISettingsStore store)
        {
            var defaultJson = JsonUtility.ToJson(this, false); // Current values as JSON
            var data = store.GetString(SettingsKey, defaultJson);

            try
            {
                JsonUtility.FromJsonOverwrite(data, this);
            }
            catch
            {
                // Broken JSON: start over with defaults
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new MainSettings(), false), this);
            }

            // Ensure nested settings objects are not null
#if UNITY_2020_1_OR_NEWER
            fetcher ??= new FetcherSettings();
            localSource ??= new LocalSourceSettings();
#else
            if (fetcher == null) fetcher = new FetcherSettings();
            if (localSource == null) localSource = new LocalSourceSettings();
#endif

            // Projects set up before 2.3.0 saved the old GitLab repository
            if (localSource.MigrateLegacyRepository())
                Save(store);
        }

        /// <summary>
        /// Saves the current settings to Unity EditorPrefs.
        /// </summary>
        public void Save() => Save(DefaultStore);

        /// <summary>
        /// Saves this instance to a store.
        /// </summary>
        internal void Save(ISettingsStore store)
        {
            store.SetString(SettingsKey, JsonUtility.ToJson(this, false));
        }
    }
}
#endif