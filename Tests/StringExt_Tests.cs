/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using NUnit.Framework;
using xAPI4Unity.Editor.Parser;

namespace xAPI4Unity.Tests
{
    [TestFixture]
    public class StringExt_Tests
    {
        [TestCase("generic", "Generic")]
        [TestCase("seriousGames", "SeriousGames")]
        [TestCase("X", "X")]
        public void Capitalize_Uppercases_The_First_Letter(string value, string expected)
        {
            // Act / Assert
            Assert.AreEqual(expected, value.Capitalize());
        }

        [Test]
        public void Capitalize_Rejects_An_Empty_String()
        {
            // Act / Assert
            Assert.Throws<ArgumentException>(() => "".Capitalize());
        }

        [TestCase("Pressed Button", "pressed_Button")]
        [TestCase("Größe (cm)", "groesse_cm")]
        [TestCase("Ärger", "aerger")]
        [TestCase("v1.2,3", "v123")]
        [TestCase("3d", "_3d")]
        public void ClearName_Makes_A_Valid_Lowercase_Identifier(string value, string expected)
        {
            // Act / Assert
            Assert.AreEqual(expected, value.ClearName());
        }

        [TestCase("")]
        [TestCase("()")]
        [TestCase(".,")]
        public void ClearName_Rejects_A_Name_Without_Usable_Characters(string value)
        {
            // Act / Assert
            var e = Assert.Throws<ArgumentException>(() => value.ClearName());
            StringAssert.Contains("no characters that can be used in a C# name", e.Message);
        }

        [TestCase("say \"hi\"", "say \\\"hi\\\"")]
        [TestCase("C:\\temp", "C:\\\\temp")]
        [TestCase("one\ntwo", "one\\ntwo")]
        [TestCase("one\r\ntwo", "one\\r\\ntwo")]
        [TestCase("a\tb", "a\\tb")]
        [TestCase("gedrückt", "gedrückt")]
        public void ClearString_Escapes_For_A_CSharp_String_Literal(string value, string expected)
        {
            // Act / Assert
            Assert.AreEqual(expected, value.ClearString());
        }
    }
}
#endif
