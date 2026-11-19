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
        /// Owner and name of the official definitions repository on GitHub.
        /// </summary>
        public const string OfficialRepository = "OmiLAXR-Ecosystem/xapi";
        /// <summary>
        /// Web URL of the official definitions repository.
        /// </summary>
        public const string OfficialRepoUrl = "https://github.com/" + OfficialRepository;
        /// <summary>
        /// Git URL of the official definitions repository.
        /// </summary>
        public const string OfficialGitUrl = OfficialRepoUrl + ".git";
        /// <summary>
        /// Web page with the releases of the official definitions.
        /// </summary>
        public const string OfficialReleasesUrl = OfficialRepoUrl + "/releases";
        /// <summary>
        /// GitHub API endpoint that lists the releases of the official definitions.
        /// </summary>
        public const string OfficialReleasesApiUrl = "https://api.github.com/repos/" + OfficialRepository + "/releases?per_page=100";
        /// <summary>
        /// Zip archive of a release of the official definitions.
        /// </summary>
        /// <param name="tag">Tag of the release, such as <c>v2.1.0</c>.</param>
        public static string OfficialArchiveUrl(string tag) => $"{OfficialRepoUrl}/archive/refs/tags/{Uri.EscapeDataString(tag)}.zip";
        /// <summary>
        /// Git URL of the example custom repository: the definitions as they were kept by Learning Technologies at RWTH Aachen before the move to GitHub.
        /// </summary>
        public const string RepoUrl = "https://gitlab.com/learntech-rwth/xapi.git";
        /// <summary>
        /// Web URL of the example custom repository, used to build the zip download URL.
        /// </summary>
        public const string RepoDownloadUrl = "https://gitlab.com/learntech-rwth/xapi";
        /// <summary>
        /// Branch of the example custom repository.
        /// </summary>
        public const string RepoBranch = "master";
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