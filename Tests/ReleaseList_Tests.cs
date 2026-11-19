/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using xAPI4Unity.Editor.Releases;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for reading the GitHub releases API with <see cref="ReleaseList"/>.
    /// </summary>
    [TestFixture]
    public class ReleaseList_Tests
    {
        /// <summary>
        /// A shortened answer of the GitHub releases API, not ordered by version, with a draft, a pre-release and a tag that is no version.
        /// </summary>
        private const string Json = @"[
  { ""tag_name"": ""v2.0.0"", ""name"": ""v2.0.0"", ""body"": ""# xapi v2.0.0"", ""html_url"": ""https://github.com/OmiLAXR-Ecosystem/xapi/releases/tag/v2.0.0"",
    ""published_at"": ""2026-11-18T23:36:57Z"", ""prerelease"": false, ""draft"": false, ""assets"": [], ""author"": { ""login"": ""x"" } },
  { ""tag_name"": ""v2.2.0-beta.1"", ""name"": ""beta"", ""body"": """", ""prerelease"": true, ""draft"": false },
  { ""tag_name"": ""v2.1.0"", ""name"": ""v2.1.0"", ""body"": ""# xapi v2.1.0"", ""published_at"": ""2026-11-19T01:44:17Z"", ""prerelease"": false, ""draft"": false },
  { ""tag_name"": ""v9.0.0"", ""name"": ""draft"", ""prerelease"": false, ""draft"": true },
  { ""tag_name"": ""nightly"", ""name"": ""nightly"", ""prerelease"": false, ""draft"": false }
]";

        /// <summary>
        /// Drafts and tags without a version are dropped, the rest is ordered newest first.
        /// </summary>
        [Test]
        public void Parse_Drops_Drafts_And_Orders_By_Version()
        {
            // Arrange, Act
            var list = ReleaseList.Parse(Json);

            // Assert
            CollectionAssert.AreEqual(new[] { "v2.2.0-beta.1", "v2.1.0", "v2.0.0" }, list.All.Select(r => r.tag_name));
            Assert.AreEqual("# xapi v2.1.0", list.Find("v2.1.0").body);
            Assert.AreEqual("2026-11-19", list.Find("v2.1.0").PublishedDate);
        }

        /// <summary>
        /// The latest release is the newest stable one unless pre-releases are asked for.
        /// </summary>
        [Test]
        public void Latest_Skips_PreReleases_Unless_Asked()
        {
            // Arrange
            var list = ReleaseList.Parse(Json);

            // Act, Assert
            Assert.AreEqual("v2.1.0", list.Latest().tag_name);
            Assert.AreEqual("v2.2.0-beta.1", list.Latest(true).tag_name);
        }

        /// <summary>
        /// A release is found by its tag or by its version without 'v'.
        /// </summary>
        [TestCase("v2.0.0")]
        [TestCase("2.0.0")]
        [TestCase("2.0")]
        public void Find_Accepts_Tags_And_Versions(string text)
        {
            // Arrange
            var list = ReleaseList.Parse(Json);

            // Act
            var release = list.Find(text);

            // Assert
            Assert.IsNotNull(release);
            Assert.AreEqual("v2.0.0", release.tag_name);
        }

        /// <summary>
        /// An update is offered only if the latest stable release is newer than the installed version.
        /// </summary>
        [TestCase("2.0.0", "v2.1.0")]
        [TestCase("2.1.0", null)]
        [TestCase("3.0.0", null)]
        [TestCase(null, null)]
        [TestCase("unknown", null)]
        public void UpdateFor_Offers_Only_Newer_Releases(string installed, string expected)
        {
            // Arrange
            var list = ReleaseList.Parse(Json);

            // Act
            var update = list.UpdateFor(installed);

            // Assert
            Assert.AreEqual(expected, update?.tag_name);
        }

        /// <summary>
        /// An error object of the API, such as the rate limit message, is not a list of releases.
        /// </summary>
        [Test]
        public void Parse_Rejects_Error_Objects()
        {
            // Arrange
            const string error = "{\"message\":\"API rate limit exceeded\"}";

            // Act, Assert
            Assert.Throws<ArgumentException>(() => ReleaseList.Parse(error));
        }
    }
}
#endif
