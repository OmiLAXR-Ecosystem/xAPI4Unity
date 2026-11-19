/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using xAPI4Unity.Editor;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for <see cref="ConfigFile"/>: creating, asking before overwriting and validating types configs.
    /// </summary>
    [TestFixture]
    public class ConfigFile_Tests
    {
        /// <summary>
        /// Temporary folder of a test, deleted afterwards.
        /// </summary>
        private string _directory;

        /// <summary>
        /// Creates the temporary folder.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "xAPI4Unity.Tests." + Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
        }

        /// <summary>
        /// Deletes the temporary folder.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        /// <summary>
        /// Counts the refreshes instead of importing assets.
        /// </summary>
        private sealed class CountingRefresher : IAssetRefresher
        {
            /// <summary>
            /// Number of refreshes.
            /// </summary>
            public int Count;
            /// <summary>
            /// Counts a refresh.
            /// </summary>
            public void Refresh() => Count++;
        }

        /// <summary>
        /// Answers every question with a fixed answer and counts the questions.
        /// </summary>
        private sealed class FixedAnswerDialogs : IEditorDialogs
        {
            /// <summary>
            /// The answer to every question.
            /// </summary>
            private readonly bool _answer;
            /// <summary>
            /// Number of questions.
            /// </summary>
            public int Asked;
            /// <summary>
            /// Creates dialogs that always give the answer.
            /// </summary>
            public FixedAnswerDialogs(bool answer) => _answer = answer;

            /// <summary>
            /// Counts the question and returns the fixed answer.
            /// </summary>
            public bool Confirm(string title, string message, string ok, string cancel)
            {
                Asked++;
                return _answer;
            }
        }

        /// <summary>
        /// A created config can be read back, ignores the default contexts, has no types and refreshes the assets once.
        /// </summary>
        [Test]
        public void Create_Writes_A_Readable_Default_Config()
        {
            // Arrange
            var path = Path.Combine(_directory, "xapi.config.json");
            var refresher = new CountingRefresher();

            // Act
            ConfigFile.Create(path, refresher);

            // Assert
            Assert.AreEqual(1, refresher.Count);
            var config = ConfigFile.Read(path);
            Assert.IsNotNull(config);
            CollectionAssert.Contains(config.ignoredContexts, "lms");
            Assert.IsEmpty(config.Types);
        }

        /// <summary>
        /// Create replaces an existing file.
        /// </summary>
        [Test]
        public void Create_Overwrites_An_Existing_File()
        {
            // Arrange
            var path = Path.Combine(_directory, "custom.config.json");
            File.WriteAllText(path, "{ \"ignoredContexts\": [] }");

            // Act
            ConfigFile.Create(path, new CountingRefresher());

            // Assert
            CollectionAssert.IsNotEmpty(ConfigFile.Read(path).ignoredContexts);
        }

        /// <summary>
        /// CreateAsking writes a missing file without asking.
        /// </summary>
        [Test]
        public void CreateAsking_Writes_A_New_File_Without_Asking()
        {
            // Arrange
            var path = Path.Combine(_directory, "xapi.config.json");
            var dialogs = new FixedAnswerDialogs(false);

            // Act
            var written = ConfigFile.CreateAsking(path, dialogs, new CountingRefresher());

            // Assert
            Assert.IsTrue(written);
            Assert.AreEqual(0, dialogs.Asked);
            Assert.IsTrue(File.Exists(path));
        }

        /// <summary>
        /// CreateAsking asks before overwriting and only writes and refreshes if the user agrees.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void CreateAsking_Overwrites_An_Existing_File_Only_If_Confirmed(bool confirm)
        {
            // Arrange
            var path = Path.Combine(_directory, "xapi.config.json");
            File.WriteAllText(path, "{ \"ignoredContexts\": [] }");
            var dialogs = new FixedAnswerDialogs(confirm);
            var refresher = new CountingRefresher();

            // Act
            var written = ConfigFile.CreateAsking(path, dialogs, refresher);

            // Assert
            Assert.AreEqual(1, dialogs.Asked);
            Assert.AreEqual(confirm, written);
            Assert.AreEqual(confirm ? 1 : 0, refresher.Count);
            Assert.AreEqual(confirm, ConfigFile.Read(path).ignoredContexts.Length > 0);
        }

        /// <summary>
        /// Validate counts the types and logs nothing when all of them resolve.
        /// </summary>
        [Test]
        public void Validate_Counts_Resolvable_Types()
        {
            // Arrange
            var path = Path.Combine(_directory, "xapi.config.json");
            File.WriteAllText(path, "{ \"types\": { \"speed\": \"System.Single\", \"label\": \"System.String\" } }");
            var count = 0;

            // Act
            var log = LogCollector.Run(() => count = ConfigFile.Validate(path));

            // Assert
            Assert.AreEqual(2, count);
            Assert.IsEmpty(log.Errors);
        }

        /// <summary>
        /// Validate logs an error for a type that does not resolve.
        /// </summary>
        [Test]
        public void Validate_Reports_Unresolvable_Types()
        {
            // Arrange
            var path = Path.Combine(_directory, "xapi.config.json");
            File.WriteAllText(path, "{ \"types\": { \"speed\": \"NoSuchTypeAnywhere\" } }");
            // The parser logs the problem as an error, which is what the wizard shows
            LogAssert.ignoreFailingMessages = true;

            // Act
            var log = LogCollector.Run(() => ConfigFile.Validate(path));

            // Assert
            Assert.IsNotEmpty(log.Errors);
        }

        /// <summary>
        /// Validate logs an error for a missing file.
        /// </summary>
        [Test]
        public void Validate_Reports_A_Missing_File()
        {
            // Arrange
            var path = Path.Combine(_directory, "missing.config.json");
            LogAssert.ignoreFailingMessages = true;

            // Act
            var log = LogCollector.Run(() => ConfigFile.Validate(path));

            // Assert
            Assert.IsNotEmpty(log.Errors);
        }
    }
}
#endif
