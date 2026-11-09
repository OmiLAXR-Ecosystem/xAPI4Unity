/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xAPI4Unity.Editor;

namespace xAPI4Unity.Tests
{
    [TestFixture]
    public class LogCollector_Tests
    {
        [Test]
        public void Run_Collects_Errors_And_Warnings()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "boom");

            // Act
            var log = LogCollector.Run(() =>
            {
                Debug.LogError("boom");
                Debug.LogWarning("careful");
                Debug.Log("fine");
            });

            // Assert
            CollectionAssert.AreEqual(new[] { "boom" }, log.Errors);
            CollectionAssert.AreEqual(new[] { "careful" }, log.Warnings);
        }

        [Test]
        public void Run_Turns_An_Exception_Into_An_Error()
        {
            // Arrange
            LogAssert.Expect(LogType.Exception, new Regex("broken"));

            // Act
            var log = LogCollector.Run(() => throw new InvalidOperationException("broken"));

            // Assert
            CollectionAssert.AreEqual(new[] { "broken" }, log.Errors);
        }

        [Test]
        public void Run_Stops_Collecting_Afterwards()
        {
            // Arrange
            var log = LogCollector.Run(() => { });

            // Act
            Debug.LogWarning("later");

            // Assert
            Assert.IsEmpty(log.Warnings);
        }
    }
}
#endif
