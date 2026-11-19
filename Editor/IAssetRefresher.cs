/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using UnityEditor;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Tells Unity that files below <c>Assets/</c> changed. Code that writes files depends on this
    /// interface instead of <see cref="AssetDatabase.Refresh()"/>, so tests do not trigger an import.
    /// </summary>
    internal interface IAssetRefresher
    {
        /// <summary>
        /// Imports changed files, so they show up in the Project window.
        /// </summary>
        void Refresh();
    }

    /// <summary>
    /// Calls <see cref="AssetDatabase.Refresh()"/>.
    /// </summary>
    internal sealed class UnityAssetRefresher : IAssetRefresher
    {
        /// <summary>
        /// Shared instance. The class has no state.
        /// </summary>
        public static readonly UnityAssetRefresher Instance = new UnityAssetRefresher();

        /// <summary>
        /// Use <see cref="Instance"/>.
        /// </summary>
        private UnityAssetRefresher() { }

        /// <inheritdoc/>
        public void Refresh() => AssetDatabase.Refresh();
    }
}
#endif
