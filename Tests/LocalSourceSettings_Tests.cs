/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using NUnit.Framework;
using xAPI4Unity.Editor;
using xAPI4Unity.Editor.Settings;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for the source choice, the branch, tag or commit and the archive URLs of <see cref="LocalSourceSettings"/>.
    /// </summary>
    [TestFixture]
    public class LocalSourceSettings_Tests
    {
        /// <summary>
        /// Settings saved before the official releases switch to them if they used the official or the old GitLab repository.
        /// </summary>
        [TestCase("https://github.com/OmiLAXR-Ecosystem/xapi.git", "https://github.com/OmiLAXR-Ecosystem/xapi")]
        [TestCase("git@gitlab.com:learntech-rwth/xapi.git", "https://gitlab.com/learntech-rwth/xapi/")]
        [TestCase(" HTTPS://GitLab.com/learntech-rwth/xapi ", "https://gitlab.com/learntech-rwth/xapi")]
        public void ResolveSource_Switches_Known_Repositories_To_Official(string repoUrl, string downloadUrl)
        {
            // Arrange
            var settings = new LocalSourceSettings { repoUrl = repoUrl, repoDownloadUrl = downloadUrl, selectedOptionIndex = 0 };

            // Act
            var changed = settings.ResolveSource();

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(DefinitionsSource.Official, settings.Source);
            Assert.AreEqual(repoUrl, settings.repoUrl);
        }

        /// <summary>
        /// A git clone stays custom, because the official releases only come as a zip.
        /// </summary>
        [Test]
        public void ResolveSource_Keeps_A_Git_Clone_Custom()
        {
            // Arrange
            var settings = new LocalSourceSettings { repoUrl = "https://github.com/OmiLAXR-Ecosystem/xapi.git", selectedOptionIndex = 2 };

            // Act
            settings.ResolveSource();

            // Assert
            Assert.AreEqual(DefinitionsSource.Custom, settings.Source);
        }

        /// <summary>
        /// A fork stays custom and keeps its URLs and branch.
        /// </summary>
        [Test]
        public void ResolveSource_Keeps_A_Fork_Custom()
        {
            // Arrange
            var settings = new LocalSourceSettings
            {
                repoUrl = "git@gitlab.com:someone/xapi.git",
                repoDownloadUrl = "https://gitlab.com/someone/xapi",
                repoBranch = "develop"
            };

            // Act
            settings.ResolveSource();

            // Assert
            Assert.AreEqual(DefinitionsSource.Custom, settings.Source);
            Assert.AreEqual("https://gitlab.com/someone/xapi", settings.repoDownloadUrl);
            Assert.AreEqual("develop", settings.repoBranch);
        }

        /// <summary>
        /// A source chosen in the wizard is never changed again.
        /// </summary>
        [Test]
        public void ResolveSource_Keeps_A_Decided_Source()
        {
            // Arrange
            var settings = new LocalSourceSettings { Source = DefinitionsSource.Custom };

            // Act
            var changed = settings.ResolveSource();

            // Assert
            Assert.IsFalse(changed);
            Assert.AreEqual(DefinitionsSource.Custom, settings.Source);
        }

        /// <summary>
        /// New settings point the custom repository to the old Learning Technologies repository as an example.
        /// </summary>
        [Test]
        public void New_Settings_Use_The_Example_Repository_And_The_Official_Source()
        {
            // Arrange
            var settings = new LocalSourceSettings();

            // Act
            settings.ResolveSource();

            // Assert
            Assert.AreEqual(DefinitionsSource.Official, settings.Source);
            Assert.AreEqual("https://gitlab.com/learntech-rwth/xapi.git", settings.repoUrl);
            Assert.AreEqual("https://gitlab.com/learntech-rwth/xapi", settings.repoDownloadUrl);
            Assert.AreEqual("master", settings.repoBranch);
        }

        /// <summary>
        /// Reset puts the URLs and the branch back to the example repository.
        /// </summary>
        [Test]
        public void ResetRepository_Restores_The_Example_Repository()
        {
            // Arrange
            var settings = new LocalSourceSettings
            {
                repoUrl = "git@github.com:someone/xapi.git",
                repoDownloadUrl = "https://github.com/someone/xapi",
                repoBranch = "1234567"
            };

            // Act
            settings.ResetRepository();

            // Assert
            Assert.AreEqual(new LocalSourceSettings().repoUrl, settings.repoUrl);
            Assert.AreEqual(new LocalSourceSettings().repoDownloadUrl, settings.repoDownloadUrl);
            Assert.AreEqual(new LocalSourceSettings().repoBranch, settings.repoBranch);
        }

        /// <summary>
        /// GitHub archives accept a branch, a tag, a commit or HEAD under '/archive/'.
        /// </summary>
        [TestCase("main", "https://github.com/someone/xapi/archive/main.zip")]
        [TestCase("86211d9", "https://github.com/someone/xapi/archive/86211d9.zip")]
        [TestCase("", "https://github.com/someone/xapi/archive/HEAD.zip")]
        [TestCase("  ", "https://github.com/someone/xapi/archive/HEAD.zip")]
        public void GetDownloadUrl_Builds_GitHub_Archive_Urls(string gitRef, string expected)
        {
            // Arrange
            var settings = new LocalSourceSettings { repoDownloadUrl = "https://github.com/someone/xapi/", repoBranch = gitRef };

            // Act
            var url = settings.GetDownloadUrl();

            // Assert
            Assert.AreEqual(expected, url);
        }

        /// <summary>
        /// GitLab archives name the ref in the path and in the file name, with slashes replaced.
        /// </summary>
        [TestCase("master", "https://gitlab.com/learntech-rwth/xapi/-/archive/master/xapi-master.zip")]
        [TestCase("feature/x", "https://gitlab.com/learntech-rwth/xapi/-/archive/feature/x/xapi-feature-x.zip")]
        [TestCase("", "https://gitlab.com/learntech-rwth/xapi/-/archive/HEAD/xapi-HEAD.zip")]
        public void GetDownloadUrl_Builds_GitLab_Archive_Urls(string gitRef, string expected)
        {
            // Arrange
            var settings = new LocalSourceSettings { repoDownloadUrl = "https://gitlab.com/learntech-rwth/xapi", repoBranch = gitRef };

            // Act
            var url = settings.GetDownloadUrl();

            // Assert
            Assert.AreEqual(expected, url);
        }

        /// <summary>
        /// Branches, tags and commits are valid. Text that could add git options or break a URL is not.
        /// </summary>
        [TestCase("main", true)]
        [TestCase("v2.1.0", true)]
        [TestCase("feature/new-verbs", true)]
        [TestCase("86211d9b0cc61b08a3ac63e52fee9a21d2d70db3", true)]
        [TestCase("--upload-pack=x", false)]
        [TestCase("a b", false)]
        [TestCase("a..b", false)]
        [TestCase("a\"b", false)]
        [TestCase("branch/", false)]
        public void IsValidRef_Accepts_Only_Safe_Refs(string gitRef, bool expected)
        {
            // Arrange, Act
            var valid = LocalSourceSettings.IsValidRef(gitRef);

            // Assert
            Assert.AreEqual(expected, valid);
        }

        /// <summary>
        /// Commit hashes are told apart from branch names, and HEAD from everything else.
        /// </summary>
        [Test]
        public void GitRef_Tells_Commits_And_Head_Apart()
        {
            // Arrange
            var commit = new LocalSourceSettings { repoBranch = "86211d9" };
            var branch = new LocalSourceSettings { repoBranch = "main" };
            var head = new LocalSourceSettings { repoBranch = "" };

            // Act, Assert
            Assert.IsTrue(commit.IsCommit);
            Assert.IsFalse(branch.IsCommit);
            Assert.IsTrue(head.IsHead);
            Assert.AreEqual("HEAD", head.GitRef);
            Assert.IsFalse(branch.IsHead);
        }
    }
}
#endif
