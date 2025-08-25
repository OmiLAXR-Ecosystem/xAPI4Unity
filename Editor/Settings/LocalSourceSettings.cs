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
        public string GetDownloadUrl() => $"{repoDownloadUrl}/-/archive/{repoBranch}/{DownloadFilename}.zip";
        
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
    }
}