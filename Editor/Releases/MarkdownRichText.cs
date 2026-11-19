/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace xAPI4Unity.Editor.Releases
{
    /// <summary>
    /// Turns the Markdown of release notes into Unity rich text for a <c>Label</c>.
    /// Covers headings, lists, quotes, code, bold, italic, links and rules. Emoji are removed,
    /// because the editor font has no glyphs for them.
    /// </summary>
    internal static class MarkdownRichText
    {
        /// <summary>
        /// Font sizes of the heading levels 1 to 3. Deeper levels use the last one.
        /// </summary>
        private static readonly int[] HeadingSizes = { 15, 13, 12 };

        /// <summary>
        /// <c># Heading</c> up to level 6, with optional closing hashes.
        /// </summary>
        private static readonly Regex Heading = new Regex(@"^(#{1,6})\s+(.*?)\s*#*\s*$");
        /// <summary>
        /// <c>- item</c>, <c>* item</c> or <c>+ item</c>, with its indentation.
        /// </summary>
        private static readonly Regex Bullet = new Regex(@"^(\s*)[-*+]\s+(.*)$");
        /// <summary>
        /// <c>1. item</c> or <c>1) item</c>, with its indentation.
        /// </summary>
        private static readonly Regex Numbered = new Regex(@"^(\s*)(\d+)[.)]\s+(.*)$");
        /// <summary>
        /// <c>&gt; quote</c>.
        /// </summary>
        private static readonly Regex Quote = new Regex(@"^\s*>\s?(.*)$");
        /// <summary>
        /// A horizontal rule: three or more <c>-</c>, <c>*</c> or <c>_</c>.
        /// </summary>
        private static readonly Regex Rule = new Regex(@"^\s*([-*_])(\s*\1){2,}\s*$");
        /// <summary>
        /// Start or end of a fenced code block.
        /// </summary>
        private static readonly Regex Fence = new Regex(@"^\s*(```|~~~)");
        /// <summary>
        /// <c>![alt](url)</c>, replaced by its alt text.
        /// </summary>
        private static readonly Regex Image = new Regex(@"!\[([^\]]*)\]\([^)]*\)");
        /// <summary>
        /// <c>[text](url)</c>, replaced by its underlined text.
        /// </summary>
        private static readonly Regex Link = new Regex(@"\[([^\]]+)\]\([^)]*\)");
        /// <summary>
        /// <c>`code`</c>.
        /// </summary>
        private static readonly Regex InlineCode = new Regex(@"`([^`]+)`");
        /// <summary>
        /// <c>**bold**</c> or <c>__bold__</c>.
        /// </summary>
        private static readonly Regex Bold = new Regex(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1");
        /// <summary>
        /// <c>*italic*</c> or <c>_italic_</c>, but not underscores inside words such as <c>snake_case</c>.
        /// </summary>
        private static readonly Regex Italic = new Regex(@"(?<![\w*])\*(?=\S)(.+?)(?<=\S)\*(?![\w*])|(?<![\w_])_(?=\S)(.+?)(?<=\S)_(?![\w_])");
        /// <summary>
        /// <c>~~strikethrough~~</c>.
        /// </summary>
        private static readonly Regex Strike = new Regex(@"~~(?=\S)(.+?)(?<=\S)~~");
        /// <summary>
        /// HTML comments, which release-please and templates leave in release notes.
        /// </summary>
        private static readonly Regex HtmlComment = new Regex(@"<!--.*?-->", RegexOptions.Singleline);

        /// <summary>
        /// Converts Markdown to rich text.
        /// </summary>
        /// <param name="markdown">The Markdown text. Null gives an empty string.</param>
        /// <param name="codeColor">Color of inline code and code blocks, as <c>#RRGGBB</c>.</param>
        /// <param name="extendedTags">Use <c>&lt;u&gt;</c> for links and <c>&lt;s&gt;</c> for strikethrough. UI Toolkit before
        /// Unity 2021.2 does not know these tags and would show them as text.</param>
        public static string Convert(string markdown, string codeColor = "#527FC0", bool extendedTags = true)
        {
            if (string.IsNullOrEmpty(markdown))
                return "";

            var text = HtmlComment.Replace(markdown.Replace("\r\n", "\n").Replace('\r', '\n'), "");
            var lines = new List<string>();
            var inCode = false;

            foreach (var rawLine in text.Split('\n'))
            {
                if (Fence.IsMatch(rawLine))
                {
                    inCode = !inCode;
                    continue;
                }

                if (inCode)
                {
                    lines.Add($"<color={codeColor}>{Escape(rawLine)}</color>");
                    continue;
                }

                var line = StripEmoji(rawLine).TrimEnd();
                if (line.Trim().Length == 0)
                {
                    // Keep at most one empty line in a row
                    if (lines.Count > 0 && lines[lines.Count - 1].Length > 0)
                        lines.Add("");
                    continue;
                }

                lines.Add(ConvertLine(line, codeColor, extendedTags));
            }

            // Drop trailing empty lines
            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                lines.RemoveAt(lines.Count - 1);
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Converts one line outside a code block.
        /// </summary>
        private static string ConvertLine(string line, string codeColor, bool extendedTags)
        {
            if (Rule.IsMatch(line))
                return "────────────────────";

            var match = Heading.Match(line);
            if (match.Success)
            {
                var level = match.Groups[1].Value.Length;
                var size = HeadingSizes[System.Math.Min(level, HeadingSizes.Length) - 1];
                return $"<size={size}><b>{Inline(match.Groups[2].Value, codeColor, extendedTags)}</b></size>";
            }

            match = Bullet.Match(line);
            if (match.Success)
                return Indent(match.Groups[1].Value) + "•  " + Inline(match.Groups[2].Value, codeColor, extendedTags);

            match = Numbered.Match(line);
            if (match.Success)
                return Indent(match.Groups[1].Value) + match.Groups[2].Value + ".  " + Inline(match.Groups[3].Value, codeColor, extendedTags);

            match = Quote.Match(line);
            if (match.Success)
                return "│ <i>" + Inline(match.Groups[1].Value, codeColor, extendedTags) + "</i>";

            return Inline(line.Trim(), codeColor, extendedTags);
        }

        /// <summary>
        /// Converts inline Markdown: code, images, links, bold, italic and strikethrough.
        /// </summary>
        private static string Inline(string text, string codeColor, bool extendedTags)
        {
            // Code spans first, so their content is not formatted
            var codes = new List<string>();
            text = InlineCode.Replace(text, m =>
            {
                codes.Add($"<color={codeColor}>{Escape(m.Groups[1].Value)}</color>");
                return "\u0001" + (codes.Count - 1) + "\u0002";
            });

            text = Escape(text);
            text = Image.Replace(text, "$1");
            text = Link.Replace(text, extendedTags ? "<u>$1</u>" : "$1");
            text = Bold.Replace(text, "<b>$2</b>");
            text = Italic.Replace(text, m => "<i>" + (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) + "</i>");
            text = Strike.Replace(text, extendedTags ? "<s>$1</s>" : "$1");

            return Regex.Replace(text, "\u0001(\\d+)\u0002", m => codes[int.Parse(m.Groups[1].Value)]);
        }

        /// <summary>
        /// Indents a nested list item by its leading whitespace, two spaces per level.
        /// </summary>
        private static string Indent(string whitespace)
        {
            var width = whitespace.Replace("\t", "  ").Length;
            return new string(' ', width / 2 * 4);
        }

        /// <summary>
        /// Replaces angle brackets, so text such as <c>List&lt;T&gt;</c> is not read as a rich text tag.
        /// </summary>
        private static string Escape(string text) => text.Replace('<', '‹').Replace('>', '›');

        /// <summary>
        /// Removes characters the editor font cannot show: emoji outside the basic plane, the symbol and dingbat
        /// blocks (such as ⚠), variation selectors and joiners. A space right after a removed character is removed too,
        /// so <c>🚀 **Features**</c> becomes <c>**Features**</c>.
        /// </summary>
        private static string StripEmoji(string text)
        {
            var builder = new StringBuilder(text.Length);
            var afterRemoved = false;
            foreach (var c in text)
            {
                if (IsEmoji(c))
                {
                    afterRemoved = true;
                    continue;
                }
                if (afterRemoved && c == ' ')
                {
                    afterRemoved = false;
                    continue;
                }
                afterRemoved = false;
                builder.Append(c);
            }
            return builder.ToString();
        }

        /// <summary>
        /// True for a character of an emoji: a surrogate, a symbol or dingbat, a variation selector, a joiner or a keycap.
        /// </summary>
        private static bool IsEmoji(char c)
            => char.IsSurrogate(c) || (c >= '☀' && c <= '➿') || c == '️' || c == '︎' || c == '‍' || c == '⃣';
    }
}
