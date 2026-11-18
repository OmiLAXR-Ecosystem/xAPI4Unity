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
    [TestFixture]
    public class ConfigFile_Tests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "xAPI4Unity.Tests." + Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private sealed class CountingRefresher : IAssetRefresher
        {
            public int Count;
            public void Refresh() => Count++;
        }

        private sealed class FixedAnswerDialogs : IEditorDialogs
        {
            private readonly bool _answer;
            public int Asked;
            public FixedAnswerDialogs(bool answer) => _answer = answer;

            public bool Confirm(string title, string message, string ok, string cancel)
            {
                Asked++;
                return _answer;
            }
        }

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
