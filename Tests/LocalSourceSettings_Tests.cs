/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using NUnit.Framework;
using xAPI4Unity.Editor.Settings;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for the migration of the old GitLab repository in <see cref="LocalSourceSettings"/>.
    /// </summary>
    [TestFixture]
    public class LocalSourceSettings_Tests
    {
        /// <summary>
        /// Git URL of the current default repository.
        /// </summary>
        private const string GitHubRepoUrl = "https://github.com/OmiLAXR-Ecosystem/xapi.git";
        /// <summary>
        /// Web URL of the current default repository.
        /// </summary>
        private const string GitHubDownloadUrl = "https://github.com/OmiLAXR-Ecosystem/xapi";

        /// <summary>
        /// The GitLab URLs and the master branch are replaced with the GitHub defaults.
        /// </summary>
        [Test]
        public void MigrateLegacyRepository_Replaces_GitLab_Defaults()
        {
            // Arrange
            var settings = new LocalSourceSettings
            {
                repoUrl = "git@gitlab.com:learntech-rwth/xapi.git",
                repoDownloadUrl = "https://gitlab.com/learntech-rwth/xapi",
                repoBranch = "master"
            };

            // Act
            var changed = settings.MigrateLegacyRepository();

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(GitHubRepoUrl, settings.repoUrl);
            Assert.AreEqual(GitHubDownloadUrl, settings.repoDownloadUrl);
            Assert.AreEqual("main", settings.repoBranch);
        }

        /// <summary>
        /// GitLab URLs with a trailing slash, .git or other casing are recognized.
        /// </summary>
        [TestCase("https://gitlab.com/learntech-rwth/xapi/")]
        [TestCase("https://gitlab.com/learntech-rwth/xapi.git")]
        [TestCase(" HTTPS://GitLab.com/learntech-rwth/xapi ")]
        public void MigrateLegacyRepository_Accepts_Url_Variants(string legacyUrl)
        {
            // Arrange
            var settings = new LocalSourceSettings { repoUrl = legacyUrl, repoDownloadUrl = legacyUrl };

            // Act
            settings.MigrateLegacyRepository();

            // Assert
            Assert.AreEqual(GitHubRepoUrl, settings.repoUrl);
            Assert.AreEqual(GitHubDownloadUrl, settings.repoDownloadUrl);
        }

        /// <summary>
        /// A fork keeps its URLs and branch.
        /// </summary>
        [Test]
        public void MigrateLegacyRepository_Keeps_Forks()
        {
            // Arrange
            var settings = new LocalSourceSettings
            {
                repoUrl = "git@gitlab.com:someone/xapi.git",
                repoDownloadUrl = "https://gitlab.com/someone/xapi",
                repoBranch = "master"
            };

            // Act
            var changed = settings.MigrateLegacyRepository();

            // Assert
            Assert.IsFalse(changed);
            Assert.AreEqual("git@gitlab.com:someone/xapi.git", settings.repoUrl);
            Assert.AreEqual("https://gitlab.com/someone/xapi", settings.repoDownloadUrl);
            Assert.AreEqual("master", settings.repoBranch);
        }

        /// <summary>
        /// Current defaults are left as they are.
        /// </summary>
        [Test]
        public void MigrateLegacyRepository_Changes_Nothing_For_Current_Defaults()
        {
            // Arrange
            var settings = new LocalSourceSettings();

            // Act
            var changed = settings.MigrateLegacyRepository();

            // Assert
            Assert.IsFalse(changed);
            Assert.AreEqual(GitHubRepoUrl, settings.repoUrl);
            Assert.AreEqual(GitHubDownloadUrl, settings.repoDownloadUrl);
            Assert.AreEqual("main", settings.repoBranch);
        }
    }
}
#endif
