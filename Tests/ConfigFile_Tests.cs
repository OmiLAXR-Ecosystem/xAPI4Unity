/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
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

        [Test]
        public void Create_Writes_A_Readable_Default_Config()
        {
            // Arrange
            var path = Path.Combine(_directory, "xapi.config.json");

            // Act
            ConfigFile.Create(path);

            // Assert
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
            ConfigFile.Create(path);

            // Assert
            CollectionAssert.IsNotEmpty(ConfigFile.Read(path).ignoredContexts);
        }
    }
}
#endif
