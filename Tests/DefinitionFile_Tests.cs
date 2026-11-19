/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using NUnit.Framework;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for the parser's <see cref="DefinitionFile"/> (not the editor's <c>xAPI4Unity.Editor.DefinitionFile</c>).
    /// </summary>
    [TestFixture]
    public class DefinitionFile_Tests
    {
        /// <summary>
        /// A definition with two names, a description and an exactMatch IRI.
        /// </summary>
        private const string Json = "{\"name\":{\"en-US\":\"Accepted\",\"de-DE\":\"Akzeptiert\"},\"description\":{\"en-US\":\"d\"},\"exactMatch\":[\"https://example.org/a\"]}";

        /// <summary>
        /// A verb file gives its context, kind, cleaned name and content.
        /// </summary>
        [Test]
        public void DefinitionFile_Reads_Context_Kind_And_Content()
        {
            // Act
            var file = new DefinitionFile("generic/verbs/accepted.json", Json);

            // Assert
            Assert.AreEqual("generic", file.Context);
            Assert.AreEqual(DefinitionType.Verb, file.DefinitionType);
            Assert.AreEqual("accepted", file.Name);
            Assert.IsFalse(file.HasCategory);
            Assert.AreEqual("Akzeptiert", file.Content.Names["de-DE"]);
            CollectionAssert.AreEqual(new[] { "https://example.org/a" }, file.Content.ExactMatch);
        }

        /// <summary>
        /// Backslashes in the path become forward slashes.
        /// </summary>
        [Test]
        public void DefinitionFile_Uses_Forward_Slashes()
        {
            // Act
            var file = new DefinitionFile("generic\\activities\\button.json", Json);

            // Assert
            Assert.AreEqual("generic/activities/button.json", file.Path);
            Assert.AreEqual(DefinitionType.Activity, file.DefinitionType);
        }

        /// <summary>
        /// Extension files give the extension type and, five folders deep, the category.
        /// </summary>
        [TestCase("generic/extensions/result/score.json", "Result", null)]
        [TestCase("generic/extensions/context/place.json", "Context", null)]
        [TestCase("generic/extensions/activity/size.json", "Activity", null)]
        [TestCase("generic/extensions/result/vr/height.json", "Result", "vr")]
        [TestCase("generic/extensions/statement/odd.json", "Unknown", null)]
        public void DefinitionFile_Reads_The_Extension_Type_And_Category(string path, string type, string category)
        {
            // Act
            var file = new DefinitionFile(path, Json);

            // Assert
            Assert.AreEqual(DefinitionType.Extension, file.DefinitionType);
            Assert.AreEqual(type, file.ExtensionType.ToString());
            Assert.AreEqual(category, file.Category);
        }

        /// <summary>
        /// A verb four folders deep has a category.
        /// </summary>
        [Test]
        public void DefinitionFile_Reads_The_Category_Of_A_Verb()
        {
            // Act
            var file = new DefinitionFile("generic/verbs/navigation/jumped.json", Json);

            // Assert
            Assert.IsTrue(file.HasCategory);
            Assert.AreEqual("navigation", file.Category);
        }

        /// <summary>
        /// The name is cleaned for C#: spaces to underscores, umlauts and ß replaced, first letter lowercase.
        /// </summary>
        [Test]
        public void DefinitionFile_Cleans_The_Name_For_Code()
        {
            // Act
            var file = new DefinitionFile("generic/activities/Große Schere.json", Json);

            // Assert
            Assert.AreEqual("grosse_Schere", file.Name);
        }

        /// <summary>
        /// A file name without usable characters throws, and the message names the file.
        /// </summary>
        [Test]
        public void DefinitionFile_Names_The_File_It_Cannot_Turn_Into_A_Name()
        {
            // Act
            var e = Assert.Throws<System.ArgumentException>(() => new DefinitionFile("generic/verbs/().json", Json));

            // Assert
            StringAssert.Contains("'generic/verbs/().json'", e.Message);
        }

        /// <summary>
        /// A folder other than verbs, activities or extensions gives an unknown kind.
        /// </summary>
        [Test]
        public void DefinitionFile_Marks_Other_Folders_As_Unknown()
        {
            // Act
            var file = new DefinitionFile("generic/other/thing.json", Json);

            // Assert
            Assert.AreEqual(DefinitionType.Unknown, file.DefinitionType);
        }
    }
}
#endif
