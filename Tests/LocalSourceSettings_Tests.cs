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
    [TestFixture]
    public class LocalSourceSettings_Tests
    {
        private const string GitHubRepoUrl = "https://github.com/OmiLAXR-Ecosystem/xapi.git";
        private const string GitHubDownloadUrl = "https://github.com/OmiLAXR-Ecosystem/xapi";

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
