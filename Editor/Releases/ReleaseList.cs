/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace xAPI4Unity.Editor.Releases
{
    /// <summary>
    /// One GitHub release, with the fields of the GitHub releases API that xAPI4Unity uses.
    /// The field names follow the API, so <see cref="JsonUtility"/> can read them.
    /// </summary>
    [Serializable]
    internal sealed class ReleaseInfo
    {
        /// <summary>
        /// Tag of the release, such as <c>v2.1.0</c>.
        /// </summary>
        public string tag_name;
        /// <summary>
        /// Title of the release.
        /// </summary>
        public string name;
        /// <summary>
        /// Release notes as Markdown.
        /// </summary>
        public string body;
        /// <summary>
        /// Web page of the release.
        /// </summary>
        public string html_url;
        /// <summary>
        /// Publication time as ISO 8601.
        /// </summary>
        public string published_at;
        /// <summary>
        /// True for a pre-release.
        /// </summary>
        public bool prerelease;
        /// <summary>
        /// True for an unpublished draft.
        /// </summary>
        public bool draft;

        /// <summary>
        /// Version parsed from <see cref="tag_name"/>. Only valid for releases in a <see cref="ReleaseList"/>.
        /// </summary>
        public ReleaseVersion Version
        {
            get
            {
                ReleaseVersion.TryParse(tag_name, out var version);
                return version;
            }
        }

        /// <summary>
        /// True for a pre-release, by the GitHub flag or by the tag.
        /// </summary>
        public bool IsPreRelease => prerelease || Version.IsPreRelease;

        /// <summary>
        /// Publication date as <c>yyyy-MM-dd</c>, empty if unknown.
        /// </summary>
        public string PublishedDate => !string.IsNullOrEmpty(published_at) && published_at.Length >= 10 ? published_at.Substring(0, 10) : "";
    }

    /// <summary>
    /// The published releases of a repository, newest version first.
    /// </summary>
    internal sealed class ReleaseList
    {
        /// <summary>
        /// Wraps the JSON array of the API, because <see cref="JsonUtility"/> cannot read a top-level array.
        /// </summary>
        [Serializable]
        private sealed class Wrapper
        {
            /// <summary>
            /// The releases of the array.
            /// </summary>
            public List<ReleaseInfo> items = new List<ReleaseInfo>();
        }

        /// <summary>
        /// Published releases with a version tag, newest version first.
        /// </summary>
        public IReadOnlyList<ReleaseInfo> All { get; }

        /// <summary>
        /// Creates a list from releases. Drafts and releases without a version tag are dropped.
        /// </summary>
        public ReleaseList(IEnumerable<ReleaseInfo> releases)
        {
            All = (releases ?? Enumerable.Empty<ReleaseInfo>())
                .Where(r => r != null && !r.draft && ReleaseVersion.TryParse(r.tag_name, out _))
                .OrderByDescending(r => r.Version)
                .ToList();
        }

        /// <summary>
        /// Reads the JSON array of the GitHub releases API (<c>GET /repos/{owner}/{repo}/releases</c>).
        /// </summary>
        /// <exception cref="ArgumentException">The JSON is not an array of releases.</exception>
        public static ReleaseList Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("["))
                throw new ArgumentException("Expected a JSON array of releases.", nameof(json));
            var wrapper = JsonUtility.FromJson<Wrapper>("{\"items\":" + json + "}");
            return new ReleaseList(wrapper?.items);
        }

        /// <summary>
        /// The newest stable release, or the newest pre-release if asked for. Null if there is none.
        /// </summary>
        public ReleaseInfo Latest(bool includePreReleases = false)
            => All.FirstOrDefault(r => includePreReleases || !r.IsPreRelease);

        /// <summary>
        /// The release with this tag or version (<c>v2.1.0</c> and <c>2.1.0</c> match the same release). Null if there is none.
        /// </summary>
        public ReleaseInfo Find(string tagOrVersion)
        {
            if (string.IsNullOrEmpty(tagOrVersion))
                return null;
            var exact = All.FirstOrDefault(r => r.tag_name == tagOrVersion);
            if (exact != null)
                return exact;
            return ReleaseVersion.TryParse(tagOrVersion, out var version)
                ? All.FirstOrDefault(r => r.Version.Equals(version))
                : null;
        }

        /// <summary>
        /// The newest stable release if it is newer than the installed version, otherwise null.
        /// Also null if the installed version is unknown.
        /// </summary>
        public ReleaseInfo UpdateFor(string installedVersion)
        {
            var latest = Latest();
            if (latest == null || !ReleaseVersion.TryParse(installedVersion, out var installed))
                return null;
            return latest.Version.CompareTo(installed) > 0 ? latest : null;
        }
    }
}
