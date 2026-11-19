/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using NUnit.Framework;
using xAPI4Unity.Editor.Releases;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for turning release notes into rich text with <see cref="MarkdownRichText"/>.
    /// </summary>
    [TestFixture]
    public class MarkdownRichText_Tests
    {
        /// <summary>
        /// Color passed to the converter, so the expected strings are short.
        /// </summary>
        private const string Code = "#C0DE00";

        /// <summary>
        /// Converts with the test color.
        /// </summary>
        private static string Convert(string markdown, bool extendedTags = true) => MarkdownRichText.Convert(markdown, Code, extendedTags);

        /// <summary>
        /// Headings become bold text in a size per level.
        /// </summary>
        [TestCase("# Title", "<size=15><b>Title</b></size>")]
        [TestCase("## Section ##", "<size=13><b>Section</b></size>")]
        [TestCase("#### Deep", "<size=12><b>Deep</b></size>")]
        public void Convert_Turns_Headings_Into_Bold_Sizes(string markdown, string expected)
        {
            // Arrange, Act
            var text = Convert(markdown);

            // Assert
            Assert.AreEqual(expected, text);
        }

        /// <summary>
        /// Bullets become dots, nested bullets are indented, numbered items keep their number.
        /// </summary>
        [Test]
        public void Convert_Turns_Lists_Into_Bullets()
        {
            // Arrange
            const string markdown = "- one\n  * two\n1. first";

            // Act
            var text = Convert(markdown);

            // Assert
            Assert.AreEqual("•  one\n    •  two\n1.  first", text);
        }

        /// <summary>
        /// Bold, italic, code and links are formatted, but underscores inside words stay.
        /// </summary>
        [Test]
        public void Convert_Formats_Inline_Markdown()
        {
            // Arrange
            const string markdown = "**bold**, *italic*, _also_, `code`, [link](https://x.org) and snake_case_name";

            // Act
            var text = Convert(markdown);

            // Assert
            Assert.AreEqual($"<b>bold</b>, <i>italic</i>, <i>also</i>, <color={Code}>code</color>, <u>link</u> and snake_case_name", text);
        }

        /// <summary>
        /// Without extended tags, links are plain text, for UI Toolkit before Unity 2021.2.
        /// </summary>
        [Test]
        public void Convert_Without_Extended_Tags_Leaves_Links_Plain()
        {
            // Arrange, Act
            var text = Convert("[registry](https://x.org) ~~old~~", false);

            // Assert
            Assert.AreEqual("registry old", text);
        }

        /// <summary>
        /// Code spans are not formatted inside, and angle brackets cannot open a rich text tag.
        /// </summary>
        [Test]
        public void Convert_Escapes_Angle_Brackets_And_Keeps_Code_As_Is()
        {
            // Arrange, Act
            var text = Convert("`**List<T>**` and <b>raw</b>");

            // Assert
            Assert.AreEqual($"<color={Code}>**List‹T›**</color> and ‹b›raw‹/b›", text);
        }

        /// <summary>
        /// Emoji are removed together with the space after them, because the editor font has no glyphs for them.
        /// </summary>
        [Test]
        public void Convert_Removes_Emoji()
        {
            // Arrange
            const string markdown = "🚀 **Features**\n⚠️ **Breaking Changes**";

            // Act
            var text = Convert(markdown);

            // Assert
            Assert.AreEqual("<b>Features</b>\n<b>Breaking Changes</b>", text);
        }

        /// <summary>
        /// Fenced code keeps its lines, runs of empty lines collapse to one, and HTML comments disappear.
        /// </summary>
        [Test]
        public void Convert_Keeps_Code_Blocks_And_Collapses_Empty_Lines()
        {
            // Arrange
            const string markdown = "Intro\r\n\r\n\r\n```json\r\n{ \"a\": 1 }\r\n```\r\n<!-- hidden -->\r\n\r\n";

            // Act
            var text = Convert(markdown);

            // Assert
            Assert.AreEqual($"Intro\n\n<color={Code}>{{ \"a\": 1 }}</color>", text);
        }

        /// <summary>
        /// Empty input gives an empty string.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        public void Convert_Handles_Empty_Input(string markdown)
        {
            // Arrange, Act
            var text = Convert(markdown);

            // Assert
            Assert.AreEqual("", text);
        }
    }
}
#endif
