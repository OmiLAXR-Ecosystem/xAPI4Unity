/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using xAPI4Unity.Editor;
using xAPI4Unity.Editor.Parser;
using xAPI4Unity.Editor.Parser.IO;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Generates a registry from the fixed definitions in <c>Tests/Fixtures~/Snapshot/xapi</c> and
    /// compares every generated file with <c>Tests/Fixtures~/Snapshot/expected</c>.
    /// The core files are left out: they are the templates with the namespace replaced,
    /// which <see cref="RegistryGenerator_Tests"/> covers.
    /// <para>
    /// After an intended change to the generated code, run the test once with the environment
    /// variable <c>XAPI4UNITY_UPDATE_SNAPSHOTS=1</c>. It rewrites <c>expected</c>; review and
    /// commit the diff. Unity ignores folders ending in <c>~</c>, so the expected <c>.cs</c>
    /// files are never compiled.
    /// </para>
    /// </summary>
    [TestFixture]
    public class RegistrySnapshot_Tests
    {
        private const string UpdateVariable = "XAPI4UNITY_UPDATE_SNAPSHOTS";
        private const string Namespace = "xAPI.Registry";

        private static string FixtureFolder
        {
            get
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(RegistryGenerator).Assembly);
                Assert.IsNotNull(package, "xAPI4Unity must be installed as a package.");
                return Path.Combine(package.resolvedPath, "Tests", "Fixtures~", "Snapshot");
            }
        }

        private static string ExpectedFolder => Path.Combine(FixtureFolder, "expected");

        private static GeneratedRegistry Generate()
        {
            var config = ConfigFile.Read(Path.Combine(FixtureFolder, "xapi.config.json"));
            var options = new GenerationOptions(Namespace)
            {
                AsmDefReferences = config.references,
                IgnoredContexts = config.ignoredContexts,
                TypeSchema = TypeSchema.Parse(config.Types, true, new TypeNameResolver(new Aliases()), config.Variables)
            };
            var generator = new RegistryGenerator(new FakeTemplateSource(), new CollectingGeneratorLog());
            var source = new DirectoryDefinitionSource(Path.Combine(FixtureFolder, "xapi", "definitions"));
            return generator.Build(options, source.ReadDefinitions());
        }

        private static Dictionary<string, string> Snapshot(GeneratedRegistry registry) =>
            registry.Files
                .Where(f => !f.RelativePath.StartsWith(RegistryGenerator.CoreFolder + "/", StringComparison.Ordinal))
                .ToDictionary(f => f.RelativePath, f => f.Content, StringComparer.Ordinal);

        private static Dictionary<string, string> ReadExpected()
        {
            var root = ExpectedFolder;
            if (!Directory.Exists(root))
                return new Dictionary<string, string>(StringComparer.Ordinal);
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .ToDictionary(
                    p => p.Substring(root.Length + 1).Replace('\\', '/'),
                    p => File.ReadAllText(p, Encoding.UTF8),
                    StringComparer.Ordinal);
        }

        private static void WriteExpected(Dictionary<string, string> files)
        {
            if (Directory.Exists(ExpectedFolder))
                Directory.Delete(ExpectedFolder, recursive: true);
            foreach (var file in files)
            {
                var path = Path.Combine(ExpectedFolder, file.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file.Value, new UTF8Encoding(false));
            }
        }

        /// <summary>
        /// Names the first line where two texts differ, so a failure is readable without a diff tool.
        /// </summary>
        private static string FirstDifference(string expected, string actual)
        {
            var e = expected.Split('\n');
            var a = actual.Split('\n');
            for (var i = 0; i < Math.Max(e.Length, a.Length); i++)
            {
                var el = i < e.Length ? e[i] : "<end of file>";
                var al = i < a.Length ? a[i] : "<end of file>";
                if (el != al)
                    return $"line {i + 1}\n  expected: {el}\n  actual:   {al}";
            }
            return "no difference";
        }

        [Test]
        public void Registry_Matches_The_Snapshot()
        {
            // Arrange
            var actual = Snapshot(Generate());
            if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
            {
                WriteExpected(actual);
                Assert.Pass($"Snapshot written to {ExpectedFolder}. Review and commit the diff.");
            }

            // Act
            var expected = ReadExpected();

            // Assert
            var missing = expected.Keys.Except(actual.Keys).OrderBy(k => k, StringComparer.Ordinal).ToArray();
            var extra = actual.Keys.Except(expected.Keys).OrderBy(k => k, StringComparer.Ordinal).ToArray();
            var changed = expected.Keys.Intersect(actual.Keys)
                .Where(k => expected[k] != actual[k])
                .OrderBy(k => k, StringComparer.Ordinal)
                .Select(k => $"{k}: {FirstDifference(expected[k], actual[k])}")
                .ToArray();

            var hint = $"If the change is intended, run the tests with {UpdateVariable}=1 and commit the new snapshot.";
            CollectionAssert.IsEmpty(missing, "Files no longer generated. " + hint);
            CollectionAssert.IsEmpty(extra, "Files generated but not in the snapshot. " + hint);
            CollectionAssert.IsEmpty(changed, "Files that differ from the snapshot. " + hint);
        }

        [Test]
        public void Registry_Leaves_Out_Ignored_Contexts_And_Keeps_The_Others()
        {
            // Act
            var paths = Snapshot(Generate()).Keys.ToArray();

            // Assert
            Assert.IsFalse(paths.Any(p => p.Contains("Ignored")), string.Join(", ", paths));
            CollectionAssert.Contains(paths, "Definitions/xAPI_Verbs_Demo.cs");
            CollectionAssert.Contains(paths, "Definitions/xAPI_Verbs_Other.cs");
        }
    }
}
#endif
