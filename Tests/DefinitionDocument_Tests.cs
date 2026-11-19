/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using xAPI4Unity.Editor;
using xAPI4Unity.Editor.Parser.Code;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for <see cref="DefinitionDocument"/>, the JSON model of the definitions editor.
    /// </summary>
    [TestFixture]
    public class DefinitionDocument_Tests
    {
        /// <summary>
        /// Serializing keeps date-like strings, decimals and umlauts as they were.
        /// </summary>
        [Test]
        public void Serialize_Keeps_Dates_Decimals_And_Umlauts()
        {
            // Arrange
            const string json = "{ \"when\": \"2024-01-01T00:00:00Z\", \"raw\": 1.50, \"name\": { \"de-DE\": \"Größe\" } }";

            // Act
            var serialized = DefinitionDocument.Serialize(DefinitionDocument.Parse(json));

            // Assert
            StringAssert.Contains("\"2024-01-01T00:00:00Z\"", serialized);
            StringAssert.Contains("1.50", serialized);
            StringAssert.Contains("Größe", serialized);
        }

        /// <summary>
        /// Serialized JSON uses four spaces and LF line breaks.
        /// </summary>
        [Test]
        public void Serialize_Indents_With_Four_Spaces_And_LF()
        {
            // Arrange
            var document = DefinitionDocument.Parse("{\"name\":{\"en-US\":\"x\"}}");

            // Act
            var serialized = DefinitionDocument.Serialize(document);

            // Assert
            Assert.AreEqual("{\n    \"name\": {\n        \"en-US\": \"x\"\n    }\n}", serialized);
        }

        /// <summary>
        /// Parse rejects a JSON array, because a definition is an object.
        /// </summary>
        [Test]
        public void Parse_Rejects_A_Json_Array()
        {
            // Arrange
            const string json = "[1, 2]";

            // Act / Assert
            Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => DefinitionDocument.Parse(json));
        }

        /// <summary>
        /// GetKey removes the definitions root and the .json extension.
        /// </summary>
        [Test]
        public void GetKey_Strips_The_Root_And_The_Extension()
        {
            // Arrange
            const string root = "C:\\project\\Assets\\xapi\\definitions";
            const string file = "C:\\project\\Assets\\xapi\\definitions\\media\\extensions\\activity\\duration.json";

            // Act
            var key = DefinitionDocument.GetKey(root, file);

            // Assert
            Assert.AreEqual("media/extensions/activity/duration", key);
        }

        /// <summary>
        /// GetIri builds the registry IRI and replaces spaces with underscores.
        /// </summary>
        [Test]
        public void GetIri_Replaces_Spaces_With_Underscores()
        {
            // Arrange
            const string key = "observation/activities/Schere ab";

            // Act
            var iri = DefinitionDocument.GetIri(key);

            // Assert
            Assert.AreEqual(CodeGenerator.XAPI_BASE_URI + "definitions/observation/activities/Schere_ab", iri);
        }

        /// <summary>
        /// GetKind and GetExtensionType read the kind and the extension type from the folders of a key.
        /// The kind is passed as text, because <see cref="DefinitionKind"/> is internal and a public test method cannot take it.
        /// </summary>
        [TestCase("generic/verbs/accepted", "Verb", null)]
        [TestCase("media/activities/video", "Activity", null)]
        [TestCase("media/extensions/result/progress", "Extension", "result")]
        [TestCase("media/other/thing", "Unknown", null)]
        [TestCase("media/verbs", "Unknown", null)]
        public void GetKind_And_GetExtensionType_Read_The_Folders(string key, string kind, string extensionType)
        {
            // Arrange / Act
            var actualKind = DefinitionDocument.GetKind(key);
            var actualType = DefinitionDocument.GetExtensionType(key);

            // Assert
            Assert.AreEqual(kind, actualKind.ToString());
            Assert.AreEqual(extensionType, actualType);
        }

        /// <summary>
        /// Renaming a language keeps the order of the languages.
        /// </summary>
        [Test]
        public void RenameLanguage_Keeps_The_Order()
        {
            // Arrange
            var document = DefinitionDocument.Parse("{\"name\":{\"en-US\":\"a\",\"xx-XX\":\"b\",\"fr-FR\":\"c\"}}");

            // Act
            var renamed = DefinitionDocument.RenameLanguage(document, "name", "xx-XX", "de-DE");

            // Assert
            Assert.IsTrue(renamed);
            CollectionAssert.AreEqual(new[] { "en-US", "de-DE", "fr-FR" },
                DefinitionDocument.GetLanguageMap(document, "name").Select(p => p.Key));
            Assert.AreEqual("b", document["name"]["de-DE"].ToString());
        }

        /// <summary>
        /// Renaming to a language that is already there is refused.
        /// </summary>
        [Test]
        public void RenameLanguage_Refuses_A_Taken_Code()
        {
            // Arrange
            var document = DefinitionDocument.Parse("{\"name\":{\"en-US\":\"a\",\"de-DE\":\"b\"}}");

            // Act
            var renamed = DefinitionDocument.RenameLanguage(document, "name", "de-DE", "en-US");

            // Assert
            Assert.IsFalse(renamed);
            Assert.AreEqual("b", document["name"]["de-DE"].ToString());
        }

        /// <summary>
        /// AddLanguage adds the first common language that is missing.
        /// </summary>
        [Test]
        public void AddLanguage_Picks_The_First_Missing_Common_Language()
        {
            // Arrange
            var document = DefinitionDocument.Parse("{\"description\":{\"en-US\":\"a\"}}");

            // Act
            var added = DefinitionDocument.AddLanguage(document, "description");

            // Assert
            Assert.AreEqual("de-DE", added);
            Assert.AreEqual("", document["description"]["de-DE"].ToString());
        }

        /// <summary>
        /// Setting an empty exactMatch list removes the field.
        /// </summary>
        [Test]
        public void SetExactMatches_Removes_The_Field_When_Empty()
        {
            // Arrange
            var document = DefinitionDocument.Parse("{\"exactMatch\":[\"https://example.org/a\"]}");

            // Act
            DefinitionDocument.SetExactMatches(document, new string[0]);

            // Assert
            Assert.IsFalse(document.ContainsKey("exactMatch"));
        }

        /// <summary>
        /// GetOtherFields lists the fields the editor does not know, in file order.
        /// </summary>
        [Test]
        public void GetOtherFields_Lists_Unknown_Fields_In_File_Order()
        {
            // Arrange
            var document = DefinitionDocument.Parse("{\"name\":{},\"choices\":[],\"description\":{},\"rules\":{}}");

            // Act
            var others = DefinitionDocument.GetOtherFields(document);

            // Assert
            CollectionAssert.AreEqual(new[] { "choices", "rules" }, others);
        }

        /// <summary>
        /// A new template gets an English name derived from the file name.
        /// </summary>
        [Test]
        public void CreateTemplate_Derives_The_English_Name_From_The_File_Name()
        {
            // Arrange / Act
            var document = DefinitionDocument.CreateTemplate("pressedButton");

            // Assert
            Assert.AreEqual("pressed button", document["name"]["en-US"].ToString());
            Assert.AreEqual("", document["description"]["en-US"].ToString());
        }
    }
}
#endif
