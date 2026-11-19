/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using xAPI4Unity.Editor;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests for <see cref="MergeReport"/>.
    /// </summary>
    [TestFixture]
    public class MergeReport_Tests
    {
        /// <summary>
        /// The report counts copied, skipped and conflicting files and lists each group.
        /// </summary>
        [Test]
        public void MergeReport_Counts_And_Groups_The_Results()
        {
            // Arrange
            var report = new MergeReport("upstream");

            // Act
            report.AddCopy("a.json");
            report.AddCopy("b.json");
            report.AddSkip("c.json");
            report.AddConflict("d.json");

            // Assert
            Assert.AreEqual("upstream", report.source);
            Assert.AreEqual(2, report.copied);
            Assert.AreEqual(1, report.skipped);
            Assert.AreEqual(1, report.conflicts);
            CollectionAssert.AreEqual(new[] { "a.json", "b.json" }, report.CopyResults.Select(r => r.path));
            CollectionAssert.AreEqual(new[] { "c.json" }, report.SkipResults.Select(r => r.path));
            CollectionAssert.AreEqual(new[] { "d.json" }, report.ConflictResults.Select(r => r.path));
        }

        /// <summary>
        /// Updated and removed files of a release update are counted and listed apart from the others.
        /// </summary>
        [Test]
        public void MergeReport_Counts_Updated_And_Removed_Files()
        {
            // Arrange
            var report = new MergeReport("OmiLAXR-Ecosystem/xapi v2.1.0");

            // Act
            report.AddUpdate("a.json");
            report.AddRemove("b.json");
            report.AddRemove("c.json");

            // Assert
            Assert.AreEqual(1, report.updated);
            Assert.AreEqual(2, report.removed);
            Assert.AreEqual(0, report.copied + report.skipped + report.conflicts);
            CollectionAssert.AreEqual(new[] { "a.json" }, report.UpdateResults.Select(r => r.path));
            CollectionAssert.AreEqual(new[] { "b.json", "c.json" }, report.RemoveResults.Select(r => r.path));
        }
    }
}
#endif
