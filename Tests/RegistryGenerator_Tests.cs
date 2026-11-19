/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Types;
using static xAPI4Unity.Tests.GeneratorFakes;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for <see cref="RegistryGenerator"/> with in-memory fakes, so nothing is written to <c>Assets</c>.
    /// </summary>
    [TestFixture]
    public class RegistryGenerator_Tests
    {
        /// <summary>
        /// Template source of the generator.
        /// </summary>
        private FakeTemplateSource _templates;
        /// <summary>
        /// Log of the generator.
        /// </summary>
        private CollectingGeneratorLog _log;
        /// <summary>
        /// The generator under test.
        /// </summary>
        private RegistryGenerator _generator;

        /// <summary>
        /// Creates the generator with fake templates and a collecting log.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _templates = new FakeTemplateSource();
            _log = new CollectingGeneratorLog();
            _generator = new RegistryGenerator(_templates, _log);
        }

        /// <summary>
        /// Builds a registry from the given definitions.
        /// </summary>
        private GeneratedRegistry Build(GenerationOptions options, params Editor.Parser.IO.DefinitionFile[] definitions) =>
            _generator.Build(options, definitions);

        /// <summary>
        /// A registry has every core file, the definition classes, the context lists and the .asmdef with its .meta.
        /// </summary>
        [Test]
        public void Build_Creates_Core_Definitions_And_AsmDef()
        {
            // Arrange
            var options = new GenerationOptions("My.Registry");

            // Act
            var registry = Build(options, Definition("generic/verbs/accepted.json"));

            // Assert
            foreach (var core in CoreFiles.Names)
                Assert.IsTrue(registry.Contains("Core/" + core), core);
            Assert.IsTrue(registry.Contains("Definitions/xAPI_Verbs_Generic.cs"));
            Assert.IsTrue(registry.Contains("Definitions/xAPI_Context_Generic.cs"));
            Assert.IsTrue(registry.Contains("Definitions/xAPI_Definitions.cs"));
            Assert.IsTrue(registry.Contains("Definitions/xAPI_Contexts.cs"));
            Assert.IsTrue(registry.Contains("My.Registry.asmdef"));
            Assert.AreEqual(CoreFiles.AsmDefMeta, registry.GetContent("My.Registry.asmdef.meta"));
        }

        /// <summary>
        /// The template namespace is replaced in the core files, and every template is read.
        /// </summary>
        [Test]
        public void Build_Replaces_The_Template_Namespace()
        {
            // Act
            var registry = Build(new GenerationOptions("My.Registry"));

            // Assert
            Assert.AreEqual("namespace My.Registry { // xAPI_Verb.cs }", registry.GetContent("Core/xAPI_Verb.cs"));
            CollectionAssert.AreEquivalent(CoreFiles.Names, _templates.Requested);
        }

        /// <summary>
        /// A verb becomes a property of its context's verbs class, with its description and URI as comment.
        /// </summary>
        [Test]
        public void Build_Puts_Each_Verb_With_Its_Uri_Into_The_Context_Class()
        {
            // Act
            var registry = Build(new GenerationOptions("My.Registry"),
                Definition("generic/verbs/accepted.json", "accepted", "Accepted something."));
            var verbs = registry.GetContent("Definitions/xAPI_Verbs_Generic.cs");

            // Assert
            StringAssert.Contains("namespace My.Registry", verbs);
            StringAssert.Contains("public sealed class xAPI_Verbs_Generic : xAPI_Verbs", verbs);
            StringAssert.Contains("public readonly xAPI_Verb accepted = new xAPI_Verb(", verbs);
            StringAssert.Contains("/// Accepted something.", verbs);
            StringAssert.Contains("URI: https://omilaxr-ecosystem.github.io/xapi/definitions/generic/verbs/accepted", verbs);
        }

        /// <summary>
        /// A category gets its own class.
        /// </summary>
        [Test]
        public void Build_Creates_A_Class_Per_Category()
        {
            // Act
            var registry = Build(new GenerationOptions("My.Registry"),
                Definition("generic/verbs/navigation/jumped.json"));

            // Assert
            Assert.IsTrue(registry.Contains("Definitions/xAPI_Verbs_Generic_Navigation.cs"));
            StringAssert.Contains("jumped", registry.GetContent("Definitions/xAPI_Verbs_Generic_Navigation.cs"));
        }

        /// <summary>
        /// Ignored contexts are left out and not counted.
        /// </summary>
        [Test]
        public void Build_Leaves_Out_Ignored_Contexts()
        {
            // Arrange
            var options = new GenerationOptions("My.Registry") { IgnoredContexts = new[] { "media" } };

            // Act
            var registry = Build(options,
                Definition("generic/verbs/accepted.json"),
                Definition("media/verbs/played.json"));

            // Assert
            Assert.AreEqual(1, registry.DefinitionCount);
            Assert.IsFalse(registry.Paths.Any(p => p.Contains("Media")));
            CollectionAssert.Contains(_log.Infos, "[xAPI4Unity]: Found 1 xAPI definitions");
        }

        /// <summary>
        /// The .asmdef has the namespace as name and lists the references.
        /// </summary>
        [Test]
        public void Build_Writes_The_References_Into_The_AsmDef()
        {
            // Arrange
            var options = new GenerationOptions("My.Registry") { AsmDefReferences = new[] { "OmiLAXR", "OmiLAXR.xAPI" } };

            // Act
            var asmDef = Build(options).GetContent("My.Registry.asmdef");

            // Assert
            StringAssert.Contains("\"name\": \"My.Registry\"", asmDef);
            StringAssert.Contains("\"OmiLAXR\",\n\"OmiLAXR.xAPI\"", asmDef);
        }

        /// <summary>
        /// Typed extensions get one method per type in the schema; the others take an object.
        /// </summary>
        [Test]
        public void Build_Types_Extension_Methods_From_The_Schema()
        {
            // Arrange
            var schema = TypeSchema.Parse(new Dictionary<string, string>
            {
                { "generic/extensions/result/score", "float | (int,int)[]" }
            });
            var options = new GenerationOptions("My.Registry") { TypeSchema = schema };

            // Act
            var registry = Build(options,
                Definition("generic/extensions/result/score.json"),
                Definition("generic/extensions/result/comment.json"));
            var extensions = registry.GetContent("Definitions/xAPI_Extensions_Result_Generic.cs");

            // Assert
            StringAssert.Contains("public xAPI_Extensions_Result_Generic score(System.Single value)", extensions);
            StringAssert.Contains("public xAPI_Extensions_Result_Generic score((System.Int32,System.Int32)[] value)", extensions);
            StringAssert.Contains("public xAPI_Extensions_Result_Generic comment(System.Object value)", extensions);
            Assert.IsTrue(_log.Infos.Any(m => m.Contains("Recognized types for 'generic/extensions/result/score'")));
        }

        /// <summary>
        /// Extensions in an unknown type folder are skipped with a warning.
        /// </summary>
        [Test]
        public void Build_Skips_Extensions_Of_An_Unknown_Type_With_A_Warning()
        {
            // Act
            var registry = Build(new GenerationOptions("My.Registry"),
                Definition("generic/extensions/statement/odd.json"));

            // Assert
            Assert.AreEqual(1, _log.Warnings.Count);
            StringAssert.Contains("Unknown extension type 'unknown' for extension 'odd'", _log.Warnings[0]);
            Assert.IsFalse(registry.Files.Any(f => f.Content.Contains(" odd(")));
        }

        /// <summary>
        /// Generate builds the registry, writes it once and logs the result.
        /// </summary>
        [Test]
        public void Generate_Writes_The_Built_Registry_Once()
        {
            // Arrange
            var source = new InMemoryDefinitionSource(Definition("generic/verbs/accepted.json"));
            var writer = new InMemoryRegistryWriter();

            // Act
            var registry = _generator.Generate(new GenerationOptions("My.Registry"), source, writer);

            // Assert
            Assert.AreEqual(1, writer.Written.Count);
            Assert.AreSame(registry, writer.Written[0]);
            Assert.AreEqual("[xAPI4Unity]: Created xAPI Registry with namespace My.Registry", _log.Infos.Last());
        }

        /// <summary>
        /// Returns a template with CRLF line breaks.
        /// </summary>
        private sealed class CrlfTemplateSource : Editor.Parser.IO.ICoreTemplateSource
        {
            /// <summary>
            /// Returns the same CRLF template for every file.
            /// </summary>
            public string Read(string fileName) => "namespace xAPI.Registry\r\n{\r\n}\r\n";
        }

        /// <summary>
        /// Core files use LF even if the template has CRLF.
        /// </summary>
        [Test]
        public void Build_Uses_LF_Even_For_CRLF_Templates()
        {
            // Arrange
            var generator = new RegistryGenerator(new CrlfTemplateSource(), _log);

            // Act
            var core = generator.Build(new GenerationOptions("My.Registry"), new Editor.Parser.IO.DefinitionFile[0])
                .GetContent("Core/xAPI_Verb.cs");

            // Assert
            Assert.AreEqual("namespace My.Registry\n{\n}\n", core);
        }

        /// <summary>
        /// No generated file contains a carriage return.
        /// </summary>
        [Test]
        public void Build_Writes_Generated_Code_With_LF_Only()
        {
            // Act
            var registry = Build(new GenerationOptions("My.Registry"),
                Definition("generic/verbs/accepted.json"),
                Definition("generic/extensions/result/score.json"));

            // Assert
            var withCr = registry.Files.Where(f => f.Content.Contains("\r")).Select(f => f.RelativePath).ToArray();
            CollectionAssert.IsEmpty(withCr);
        }

        /// <summary>
        /// A description with CRLF becomes one comment line per line.
        /// </summary>
        [Test]
        public void Build_Splits_A_CRLF_Description_Into_Comment_Lines()
        {
            // Act
            var registry = Build(new GenerationOptions("My.Registry"),
                Definition("generic/verbs/accepted.json", "accepted", "Line one\\r\\nLine two"));
            var verbs = registry.GetContent("Definitions/xAPI_Verbs_Generic.cs");

            // Assert
            StringAssert.Contains("/// Line one\n        /// Line two\n", verbs);
        }

        /// <summary>
        /// Options reject an empty or blank namespace.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void GenerationOptions_Rejects_An_Empty_Namespace(string @namespace)
        {
            // Act / Assert
            Assert.Throws<System.ArgumentException>(() => new GenerationOptions(@namespace));
        }
    }
}
#endif
