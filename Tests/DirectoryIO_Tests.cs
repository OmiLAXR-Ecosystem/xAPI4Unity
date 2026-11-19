/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for the disk implementations of the generator interfaces, in a temporary folder.
    /// </summary>
    [TestFixture]
    public class DirectoryIO_Tests
    {
        /// <summary>
        /// A minimal valid definition.
        /// </summary>
        private const string Json = "{\"name\":{\"en-US\":\"x\"},\"description\":{\"en-US\":\"y\"}}";
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
        /// Writes a file below the temporary folder and creates its folders.
        /// </summary>
        private void WriteFile(string relativePath, string content = Json)
        {
            var path = Path.Combine(_directory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Only .json files are read, with paths relative to the root, in ordinal order.
        /// </summary>
        [Test]
        public void DirectoryDefinitionSource_Reads_Relative_Paths_In_Ordinal_Order()
        {
            // Arrange
            WriteFile("media/verbs/played.json");
            WriteFile("generic/verbs/accepted.json");
            WriteFile("generic/extensions/result/score.json");
            WriteFile("generic/verbs/readme.txt", "not a definition");

            // Act
            var definitions = new DirectoryDefinitionSource(_directory).ReadDefinitions();

            // Assert
            CollectionAssert.AreEqual(
                new[] { "generic/extensions/result/score.json", "generic/verbs/accepted.json", "media/verbs/played.json" },
                definitions.Select(d => d.Path));
            Assert.AreEqual("x", definitions[0].Content.Names["en-US"]);
        }

        /// <summary>
        /// A trailing separator on the root path does not change the relative paths.
        /// </summary>
        [Test]
        public void DirectoryDefinitionSource_Accepts_A_Trailing_Separator()
        {
            // Arrange
            WriteFile("generic/verbs/accepted.json");

            // Act
            var definitions = new DirectoryDefinitionSource(_directory + Path.DirectorySeparatorChar).ReadDefinitions();

            // Assert
            Assert.AreEqual("generic/verbs/accepted.json", definitions.Single().Path);
        }

        /// <summary>
        /// Files whose name starts with a dot are skipped.
        /// </summary>
        [Test]
        public void DirectoryDefinitionSource_Skips_Hidden_Files()
        {
            // Arrange
            WriteFile("generic/verbs/accepted.json");
            WriteFile("generic/verbs/.draft.json");

            // Act
            var definitions = new DirectoryDefinitionSource(_directory).ReadDefinitions();

            // Assert
            Assert.AreEqual(1, definitions.Count);
        }

        /// <summary>
        /// A missing folder throws DirectoryNotFoundException.
        /// </summary>
        [Test]
        public void DirectoryDefinitionSource_Throws_For_A_Missing_Folder()
        {
            // Arrange
            var source = new DirectoryDefinitionSource(Path.Combine(_directory, "missing"));

            // Act / Assert
            Assert.Throws<DirectoryNotFoundException>(() => source.ReadDefinitions());
        }

        /// <summary>
        /// The writer deletes files of an earlier registry and writes the new ones in their folders.
        /// </summary>
        [Test]
        public void DirectoryRegistryWriter_Replaces_The_Folder()
        {
            // Arrange
            var root = Path.Combine(_directory, "My.Registry");
            Directory.CreateDirectory(Path.Combine(root, "Definitions"));
            File.WriteAllText(Path.Combine(root, "Definitions", "Stale.cs"), "old");
            var registry = new GeneratedRegistry("My.Registry");
            registry.Add("Core/xAPI_Verb.cs", "core");
            registry.Add("My.Registry.asmdef", "{}");

            // Act
            new DirectoryRegistryWriter(root).Write(registry);

            // Assert
            Assert.IsFalse(File.Exists(Path.Combine(root, "Definitions", "Stale.cs")));
            Assert.AreEqual("core", File.ReadAllText(Path.Combine(root, "Core", "xAPI_Verb.cs")));
            Assert.AreEqual("{}", File.ReadAllText(Path.Combine(root, "My.Registry.asmdef")));
        }

        /// <summary>
        /// The template source reads <c>&lt;name&gt;.tmpl</c> from its folder.
        /// </summary>
        [Test]
        public void DirectoryCoreTemplateSource_Reads_The_Tmpl_File()
        {
            // Arrange
            WriteFile("xAPI_Verb.cs.tmpl", "namespace xAPI.Registry {}");

            // Act
            var text = new DirectoryCoreTemplateSource(_directory).Read("xAPI_Verb.cs");

            // Assert
            Assert.AreEqual("namespace xAPI.Registry {}", text);
        }

        /// <summary>
        /// The package ships a template for every core file.
        /// </summary>
        [Test]
        public void DirectoryCoreTemplateSource_ForPackage_Has_Every_Core_Template()
        {
            // Arrange
            var templates = DirectoryCoreTemplateSource.ForPackage();

            // Act
            var missing = CoreFiles.Names
                .Where(name => !File.Exists(Path.Combine(templates.Folder, name + ".tmpl")))
                .ToArray();

            // Assert
            CollectionAssert.IsEmpty(missing, "Missing templates in " + templates.Folder);
        }
    }
}
#endif
