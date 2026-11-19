/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using UnityEditor;

namespace xAPI4Unity.Editor.Settings
{
    /// <summary>
    /// Stores settings in Unity's <see cref="EditorPrefs"/> (per user and machine).
    /// </summary>
    internal sealed class EditorPrefsSettingsStore : ISettingsStore
    {
        /// <summary>
        /// Shared instance. The class has no state.
        /// </summary>
        public static readonly EditorPrefsSettingsStore Instance = new EditorPrefsSettingsStore();

        /// <summary>
        /// Use <see cref="Instance"/>.
        /// </summary>
        private EditorPrefsSettingsStore() { }

        /// <inheritdoc/>
        public string GetString(string key, string defaultValue) => EditorPrefs.GetString(key, defaultValue);

        /// <inheritdoc/>
        public void SetString(string key, string value) => EditorPrefs.SetString(key, value);

        /// <inheritdoc/>
        public void DeleteKey(string key) => EditorPrefs.DeleteKey(key);
    }
}
#endif
