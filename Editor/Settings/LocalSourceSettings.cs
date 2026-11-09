/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using UnityEngine;
using xAPI4Unity.Editor.Parser;

namespace xAPI4Unity.Editor.Settings
{
    /// <summary>
    /// Stores settings related to the local source of xAPI definitions.
    /// </summary>
    [Serializable]
    public class LocalSourceSettings : ISettings
    {
        [SerializeField] public string path = DefaultValues.RepoPath;
        [SerializeField] public int selectedOptionIndex = 0; // Selected source option
        [SerializeField] public string repoUrl = DefaultValues.RepoUrl;
        [SerializeField] public string repoDownloadUrl = DefaultValues.RepoDownloadUrl;
        [SerializeField] public string repoBranch = DefaultValues.RepoBranch;
        [SerializeField] public int gitCloneAmount = 0; // Counter for cloning operations

        /// <summary>
        /// Gets the download filename based on the branch.
        /// </summary>
        public string DownloadFilename => "xapi-" + repoBranch;

        /// <summary>
        /// Constructs the full download URL for the repository zip file.
        /// </summary>
        public string GetDownloadUrl()
        {
            var url = repoDownloadUrl.TrimEnd('/');
            // GitHub and GitLab use different archive URL schemes
            return url.Contains("github.com")
                ? $"{url}/archive/refs/heads/{repoBranch}.zip"
                : $"{url}/-/archive/{repoBranch}/{DownloadFilename}.zip";
        }
        
        /// <inheritdoc />
        public T Clone<T>()
        {
            return (T)(object)(new LocalSourceSettings() { path = path });
        }

        /// <inheritdoc />
        public bool Equals(ISettings s)
            => (s as LocalSourceSettings)?.path == path;

        /// <summary>
        /// Generates a unique branch name for cloning operations.
        /// </summary>
        /// <returns>A unique branch name string.</returns>
        public string GetNewBranchName() => $"xapi-4-unity-{Application.productName.ClearName()}-{gitCloneAmount++}";

        // Repository the definitions came from before 2.3.0, as SSH and HTTPS URL without '.git'
        private static readonly string[] LegacyRepoUrls =
        {
            "git@gitlab.com:learntech-rwth/xapi",
            "https://gitlab.com/learntech-rwth/xapi"
        };
        private const string LegacyRepoBranch = "master";

        /// <summary>
        /// Replaces the GitLab repository and its 'master' branch, saved by versions before 2.3.0, with the GitHub defaults.
        /// Other URLs, such as forks, are kept.
        /// </summary>
        /// <returns>True if a value changed.</returns>
        internal bool MigrateLegacyRepository()
        {
            var changed = false;
            if (IsLegacyRepoUrl(repoUrl))
            {
                repoUrl = DefaultValues.RepoUrl;
                changed = true;
            }
            if (IsLegacyRepoUrl(repoDownloadUrl))
            {
                repoDownloadUrl = DefaultValues.RepoDownloadUrl;
                changed = true;
            }
            // The GitHub repository has no 'master' branch
            if (repoDownloadUrl == DefaultValues.RepoDownloadUrl && repoBranch == LegacyRepoBranch)
            {
                repoBranch = DefaultValues.RepoBranch;
                changed = true;
            }
            return changed;
        }

        private static bool IsLegacyRepoUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            var normalized = url.Trim().TrimEnd('/');
            if (normalized.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - 4);
            return Array.Exists(LegacyRepoUrls, legacy => string.Equals(normalized, legacy, StringComparison.OrdinalIgnoreCase));
        }
    }
}