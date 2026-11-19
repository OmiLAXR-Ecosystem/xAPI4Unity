/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.Text.RegularExpressions;
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
        /// <summary>
        /// Value of <see cref="source"/> before it is decided, in settings saved by versions before the official releases.
        /// </summary>
        internal const int UnsetSource = -1;

        /// <summary>
        /// Source folder that holds the <c>definitions</c> folder.
        /// </summary>
        [SerializeField] public string path = DefaultValues.RepoPath;
        /// <summary>
        /// Where the definitions come from, a <see cref="DefinitionsSource"/>. <see cref="UnsetSource"/> until
        /// <see cref="ResolveSource"/> decides it for settings saved by older versions.
        /// </summary>
        [SerializeField] public int source = UnsetSource;
        /// <summary>
        /// Tag of the official release to install, such as <c>v2.1.0</c>. Empty for the latest release.
        /// </summary>
        [SerializeField] public string officialVersion = "";
        /// <summary>
        /// How the wizard gets a custom repository: 0 patch into <c>Assets</c>, 1 download a zip, 2 git clone.
        /// </summary>
        [SerializeField] public int selectedOptionIndex = 0;
        /// <summary>
        /// Git URL of the custom repository.
        /// </summary>
        [SerializeField] public string repoUrl = DefaultValues.RepoUrl;
        /// <summary>
        /// Web URL of the custom repository, used for the zip download.
        /// </summary>
        [SerializeField] public string repoDownloadUrl = DefaultValues.RepoDownloadUrl;
        /// <summary>
        /// Branch, tag or commit of the custom repository to clone or download. Empty or <c>HEAD</c> for the default branch.
        /// </summary>
        [SerializeField] public string repoBranch = DefaultValues.RepoBranch;
        /// <summary>
        /// Counter for the names of new branches (<see cref="GetNewBranchName"/>).
        /// </summary>
        [SerializeField] public int gitCloneAmount = 0;

        /// <summary>
        /// Characters allowed in <see cref="repoBranch"/>, so it is safe to pass to git and to put into a URL.
        /// </summary>
        private static readonly Regex GitRefPattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._/\-]*$");
        /// <summary>
        /// A full or abbreviated commit hash.
        /// </summary>
        private static readonly Regex CommitPattern = new Regex(@"^[0-9a-fA-F]{7,40}$");

        /// <summary>
        /// The source as enum. Official until <see cref="ResolveSource"/> decided it.
        /// </summary>
        public DefinitionsSource Source
        {
            get => source == (int)DefinitionsSource.Custom ? DefinitionsSource.Custom : DefinitionsSource.Official;
            set => source = (int)value;
        }

        /// <summary>
        /// True if the definitions come from an official release.
        /// </summary>
        public bool IsOfficial => Source == DefinitionsSource.Official;

        /// <summary>
        /// The branch, tag or commit to use: <see cref="repoBranch"/> trimmed, or <c>HEAD</c> if it is empty.
        /// </summary>
        public string GitRef => string.IsNullOrWhiteSpace(repoBranch) ? "HEAD" : repoBranch.Trim();

        /// <summary>
        /// True if <see cref="GitRef"/> is <c>HEAD</c>, the default branch of the repository.
        /// </summary>
        public bool IsHead => string.Equals(GitRef, "HEAD", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True if <see cref="GitRef"/> looks like a commit hash.
        /// </summary>
        public bool IsCommit => CommitPattern.IsMatch(GitRef);

        /// <summary>
        /// True if <see cref="GitRef"/> only has characters git allows and that are safe in a command line and a URL.
        /// </summary>
        public bool IsValidGitRef => IsValidRef(GitRef);

        /// <summary>
        /// True if the text only has characters git allows and that are safe in a command line and a URL.
        /// </summary>
        internal static bool IsValidRef(string gitRef)
            => !string.IsNullOrEmpty(gitRef) && GitRefPattern.IsMatch(gitRef) && !gitRef.Contains("..") && !gitRef.EndsWith("/") && !gitRef.EndsWith(".lock");

        /// <summary>
        /// Gets the download filename based on the branch, tag or commit.
        /// </summary>
        public string DownloadFilename => "xapi-" + GitRef.Replace('/', '-');

        /// <summary>
        /// Constructs the full download URL for the repository zip file of <see cref="GitRef"/>.
        /// </summary>
        public string GetDownloadUrl()
        {
            var url = (repoDownloadUrl ?? "").Trim().TrimEnd('/');
            // GitHub and GitLab use different archive URL schemes. Both accept a branch, a tag, a commit or HEAD.
            return url.Contains("github.com")
                ? $"{url}/archive/{GitRef}.zip"
                : $"{url}/-/archive/{GitRef}/{DownloadFilename}.zip";
        }

        /// <summary>
        /// Puts the custom repository back to the example repository and its branch.
        /// </summary>
        public void ResetRepository()
        {
            repoUrl = DefaultValues.RepoUrl;
            repoDownloadUrl = DefaultValues.RepoDownloadUrl;
            repoBranch = DefaultValues.RepoBranch;
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

        /// <summary>
        /// Repositories with the same definitions as the official releases, without <c>.git</c>: the official
        /// one and the GitLab repository the definitions came from before 2.3.0.
        /// </summary>
        private static readonly string[] OfficialOrLegacyRepoUrls =
        {
            DefaultValues.OfficialRepoUrl,
            "git@github.com:" + DefaultValues.OfficialRepository,
            "git@gitlab.com:learntech-rwth/xapi",
            "https://gitlab.com/learntech-rwth/xapi"
        };

        /// <summary>
        /// Decides <see cref="source"/> for settings saved before the official releases existed.
        /// A git clone and a fork stay custom, everything else switches to the official releases.
        /// Does nothing if the source is decided already.
        /// </summary>
        /// <returns>True if the source changed.</returns>
        internal bool ResolveSource()
        {
            if (source == (int)DefinitionsSource.Official || source == (int)DefinitionsSource.Custom)
                return false;
            var isClone = selectedOptionIndex == 2;
            var isFork = !IsOfficialOrLegacyRepoUrl(repoUrl) || !IsOfficialOrLegacyRepoUrl(repoDownloadUrl);
            Source = isClone || isFork ? DefinitionsSource.Custom : DefinitionsSource.Official;
            return true;
        }

        /// <summary>
        /// True if the URL is empty or points to the official or the old GitLab repository, ignoring case, a trailing <c>/</c> and <c>.git</c>.
        /// </summary>
        private static bool IsOfficialOrLegacyRepoUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;
            var normalized = url.Trim().TrimEnd('/');
            if (normalized.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - 4);
            return Array.Exists(OfficialOrLegacyRepoUrls, known => string.Equals(normalized, known, StringComparison.OrdinalIgnoreCase));
        }
    }
}
