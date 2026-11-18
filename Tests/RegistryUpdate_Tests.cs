/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using xAPI4Unity.Editor;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Tests
{
    [TestFixture]
    public class RegistryUpdate_Tests
    {
        private const string Json = "{\"name\":{\"en-US\":\"x\"},\"description\":{\"en-US\":\"y\"}}";
        private string _directory;
        private CollectingGeneratorLog _log;
        private RegistryUpdate _update;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "xAPI4Unity.Tests." + Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
            _log = new CollectingGeneratorLog();
            _update = new RegistryUpdate(new RegistryGenerator(new FakeTemplateSource(), _log),
                new TypeNameResolver(new Aliases()), _log);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private RegistryPaths Paths(string source = "xapi") =>
            new RegistryPaths(Path.Combine(_directory, source), Path.Combine(_directory, "Assets"), "My.Registry");

        private void WriteFile(string relativePath, string content = Json)
        {
            var path = Path.Combine(_directory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }

        [Test]
        public void RegistryPaths_Puts_The_Config_Next_To_The_Source_Folder()
        {
            // Act
            var paths = new RegistryPaths("/project/Assets/xapi/", "/project/Assets", "xAPI.Registry");

            // Assert
            Assert.AreEqual("/project/Assets/xapi.config.json", paths.ConfigFile);
            Assert.AreEqual(Path.Combine("/project/Assets/xapi/", "definitions"), paths.DefinitionsFolder);
            Assert.AreEqual(Path.Combine("/project/Assets", "xAPI.Registry"), paths.RegistryFolder);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void RegistryPaths_Without_A_Source_Folder_Has_No_Definitions_Or_Config(string source)
        {
            // Act
            var paths = new RegistryPaths(source, "/project/Assets", "xAPI.Registry");
            var status = new ProjectStatus(paths);

            // Assert
            Assert.IsNull(paths.DefinitionsFolder);
            Assert.IsNull(paths.ConfigFile);
            Assert.IsFalse(status.HasDefinitions);
            Assert.IsFalse(status.HasConfig);
            Assert.IsFalse(status.HasMergeReports);
        }

        [Test]
        public void ProjectStatus_Reports_What_Exists()
        {
            // Arrange
            WriteFile("xapi/definitions/demo/verbs/pressed.json");
            WriteFile("xapi.config.json", "{}");
            WriteFile("xapi/2026-11-19-xapi-report.json", "{}");
            WriteFile("Assets/My.Registry/Core/xAPI_Verb.cs", "// core");
            WriteFile("Assets/My.Registry/Definitions/xAPI_Verbs_Demo.cs", "// verbs");

            // Act
            var status = new ProjectStatus(Paths());

            // Assert
            Assert.IsTrue(status.HasDefinitions);
            Assert.IsTrue(status.HasConfig);
            Assert.IsTrue(status.HasMergeReports);
            Assert.IsTrue(status.HasRegistry);
            Assert.AreEqual(2, status.RegistryFileCount);
        }

        [Test]
        public void ReadConfig_Returns_Null_With_A_Warning_Without_A_Config()
        {
            // Act
            var config = _update.ReadConfig(Paths());

            // Assert
            Assert.IsNull(config);
            StringAssert.Contains("Default types will be applied", _log.Warnings.Single());
        }

        [Test]
        public void CreateOptions_Without_A_Config_Uses_Defaults()
        {
            // Act
            var options = _update.CreateOptions("My.Registry", null);

            // Assert
            Assert.AreEqual("My.Registry", options.Namespace);
            Assert.IsNull(options.TypeSchema);
            CollectionAssert.IsEmpty(options.AsmDefReferences);
            CollectionAssert.IsEmpty(options.IgnoredContexts);
        }

        [Test]
        public void Run_Applies_The_Config_Next_To_The_Source_Folder()
        {
            // Arrange
            WriteFile("xapi/definitions/demo/extensions/result/score.json");
            WriteFile("xapi/definitions/skip/verbs/hidden.json");
            WriteFile("xapi.config.json",
                "{ \"types\": { \"demo/extensions/result/score\": \"float\" }, " +
                "\"references\": [\"OmiLAXR\"], \"ignoredContexts\": [\"skip\"] }");
            var writer = new InMemoryRegistryWriter();

            // Act
            var ran = _update.Run(Paths(), writer);

            // Assert
            Assert.IsTrue(ran);
            var registry = writer.Written.Single();
            StringAssert.Contains("score(System.Single value)", registry.GetContent("Definitions/xAPI_Extensions_Result_Demo.cs"));
            StringAssert.Contains("\"OmiLAXR\"", registry.GetContent("My.Registry.asmdef"));
            Assert.IsFalse(registry.Paths.Any(p => p.Contains("Skip")));
        }

        [Test]
        public void Run_Stops_With_An_Error_Without_Definitions()
        {
            // Arrange
            var writer = new InMemoryRegistryWriter();

            // Act
            var ran = _update.Run(Paths("missing"), writer);

            // Assert
            Assert.IsFalse(ran);
            CollectionAssert.IsEmpty(writer.Written);
            StringAssert.Contains("doesn't exist", _log.Errors.Single());
        }
    }
}
#endif
