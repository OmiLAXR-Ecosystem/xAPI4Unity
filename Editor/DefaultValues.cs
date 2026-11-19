/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Default values of the settings.
    /// </summary>
    internal static class DefaultValues
    {
        /// <summary>
        /// Regenerate the registry when a definition changes.
        /// </summary>
        public const bool WatcherEnabled = true;
        /// <summary>
        /// Git URL of the xAPI definitions repository.
        /// </summary>
        public const string RepoUrl = "https://github.com/OmiLAXR-Ecosystem/xapi.git";
        /// <summary>
        /// Web URL of the repository, used to build the zip download URL.
        /// </summary>
        public const string RepoDownloadUrl = "https://github.com/OmiLAXR-Ecosystem/xapi";
        /// <summary>
        /// Branch of the repository.
        /// </summary>
        public const string RepoBranch = "main";
        /// <summary>
        /// Namespace of the generated registry, also its folder in <c>Assets</c>.
        /// </summary>
        public const string Namespace = "xAPI.Registry";
        /// <summary>
        /// Default source folder: <c>Assets/xapi</c>.
        /// </summary>
        public static string RepoPath => Path.Combine(AssetRoot, "xapi");
        /// <summary>
        /// The user's home folder.
        /// </summary>
        public static string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        /// <summary>
        /// The project's <c>Assets</c> folder.
        /// </summary>
        public static string AssetRoot => Application.dataPath;
    }
}
#endif