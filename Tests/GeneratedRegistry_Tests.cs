/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using xAPI4Unity.Editor.Parser;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for <see cref="GeneratedRegistry"/>.
    /// </summary>
    [TestFixture]
    public class GeneratedRegistry_Tests
    {
        /// <summary>
        /// Adding a path again replaces its content and keeps its position.
        /// </summary>
        [Test]
        public void Add_Replaces_A_File_And_Keeps_Its_Position()
        {
            // Arrange
            var registry = new GeneratedRegistry("R");
            registry.Add("a.cs", "1");
            registry.Add("b.cs", "2");

            // Act
            registry.Add("a.cs", "3");

            // Assert
            CollectionAssert.AreEqual(new[] { "a.cs", "b.cs" }, registry.Paths);
            Assert.AreEqual("3", registry.GetContent("a.cs"));
        }

        /// <summary>
        /// Backslashes in paths become forward slashes.
        /// </summary>
        [Test]
        public void Add_Uses_Forward_Slashes()
        {
            // Arrange
            var registry = new GeneratedRegistry("R");

            // Act
            registry.Add("Core\\xAPI_Verb.cs", "x");

            // Assert
            Assert.IsTrue(registry.Contains("Core/xAPI_Verb.cs"));
        }

        /// <summary>
        /// Reading an unknown path throws KeyNotFoundException.
        /// </summary>
        [Test]
        public void GetContent_Throws_For_An_Unknown_Path()
        {
            // Arrange
            var registry = new GeneratedRegistry("R");

            // Act / Assert
            Assert.Throws<KeyNotFoundException>(() => registry.GetContent("missing.cs"));
        }
    }
}
#endif
