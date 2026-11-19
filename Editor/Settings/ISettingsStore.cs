/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Settings
{
    /// <summary>
    /// Key-value storage for settings. <see cref="MainSettings"/> depends on this interface
    /// instead of <c>EditorPrefs</c>, so tests can keep settings in memory.
    /// </summary>
    internal interface ISettingsStore
    {
        /// <summary>
        /// Returns the stored value, or <paramref name="defaultValue"/> if the key is not set.
        /// </summary>
        string GetString(string key, string defaultValue);

        /// <summary>
        /// Stores a value under the key.
        /// </summary>
        void SetString(string key, string value);

        /// <summary>
        /// Removes the key.
        /// </summary>
        void DeleteKey(string key);
    }
}
#endif
