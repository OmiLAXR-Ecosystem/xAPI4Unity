/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using NUnit.Framework;
using xAPI4Unity.Editor.Releases;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for parsing and ordering <see cref="ReleaseVersion"/>.
    /// </summary>
    [TestFixture]
    public class ReleaseVersion_Tests
    {
        /// <summary>
        /// Tags with or without 'v', with two or three numbers and with a pre-release are read.
        /// </summary>
        [TestCase("v2.1.0", "2.1.0")]
        [TestCase("2.1.0", "2.1.0")]
        [TestCase("V2.1", "2.1.0")]
        [TestCase(" 2.1.0\n", "2.1.0")]
        [TestCase("v3.0.0-beta.2", "3.0.0-beta.2")]
        [TestCase("v1.2.3+build.5", "1.2.3")]
        public void TryParse_Reads_Tags(string text, string expected)
        {
            // Arrange, Act
            var ok = ReleaseVersion.TryParse(text, out var version);

            // Assert
            Assert.IsTrue(ok);
            Assert.AreEqual(expected, version.ToString());
        }

        /// <summary>
        /// Text that is not a version is rejected.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("latest")]
        [TestCase("v2")]
        [TestCase("release-2.1.0")]
        public void TryParse_Rejects_Other_Text(string text)
        {
            // Arrange, Act
            var ok = ReleaseVersion.TryParse(text, out _);

            // Assert
            Assert.IsFalse(ok);
        }

        /// <summary>
        /// Versions are ordered by number, numerically, and a stable release is newer than its pre-releases.
        /// </summary>
        [TestCase("2.1.0", "2.0.0")]
        [TestCase("2.10.0", "2.9.0")]
        [TestCase("3.0.0", "2.99.99")]
        [TestCase("2.1.0", "2.1.0-rc.1")]
        [TestCase("2.1.0-rc.2", "2.1.0-rc.1")]
        [TestCase("2.1.0-rc.10", "2.1.0-rc.2")]
        [TestCase("2.1.0-rc", "2.1.0-beta")]
        [TestCase("2.1.0-beta", "2.1.0-1")]
        public void CompareTo_Orders_Newer_Versions_After_Older_Ones(string newer, string older)
        {
            // Arrange
            ReleaseVersion.TryParse(newer, out var a);
            ReleaseVersion.TryParse(older, out var b);

            // Act, Assert
            Assert.Greater(a.CompareTo(b), 0);
            Assert.Less(b.CompareTo(a), 0);
        }

        /// <summary>
        /// A tag and the same version without 'v' are equal.
        /// </summary>
        [Test]
        public void Equals_Ignores_The_V_Prefix()
        {
            // Arrange
            ReleaseVersion.TryParse("v2.1.0", out var tag);
            ReleaseVersion.TryParse("2.1", out var version);

            // Act, Assert
            Assert.IsTrue(tag.Equals(version));
            Assert.AreEqual(tag.GetHashCode(), version.GetHashCode());
        }
    }
}
#endif
