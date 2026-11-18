/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using xAPI4Unity.Editor.Settings;

namespace xAPI4Unity.Tests
{
    [TestFixture]
    public class MainSettings_Tests
    {
        /// <summary>
        /// Keeps settings in memory instead of EditorPrefs, so tests do not touch the user's settings.
        /// </summary>
        private sealed class InMemorySettingsStore : ISettingsStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public int Writes;

            public string GetString(string key, string defaultValue) =>
                Values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value)
            {
                Writes++;
                Values[key] = value;
            }

            public void DeleteKey(string key) => Values.Remove(key);
        }

        [Test]
        public void Save_And_Load_Keep_The_Values()
        {
            // Arrange
            var store = new InMemorySettingsStore();
            var saved = new MainSettings { @namespace = "My.Registry", isOpeningOnStartup = false };
            saved.localSource.path = "/tmp/xapi";
            saved.fetcher.watcherEnabled = false;

            // Act
            saved.Save(store);
            var loaded = new MainSettings();
            loaded.Load(store);

            // Assert
            Assert.AreEqual("My.Registry", loaded.@namespace);
            Assert.IsFalse(loaded.isOpeningOnStartup);
            Assert.AreEqual("/tmp/xapi", loaded.localSource.path);
            Assert.IsFalse(loaded.fetcher.watcherEnabled);
            Assert.IsTrue(store.Values.ContainsKey(MainSettings.SettingsKey));
        }

        [Test]
        public void Load_Keeps_The_Current_Values_When_Nothing_Is_Stored()
        {
            // Arrange
            var settings = new MainSettings { @namespace = "Kept" };

            // Act
            settings.Load(new InMemorySettingsStore());

            // Assert
            Assert.AreEqual("Kept", settings.@namespace);
        }

        [Test]
        public void Load_Falls_Back_To_Defaults_For_Broken_Json()
        {
            // Arrange
            var store = new InMemorySettingsStore();
            store.Values[MainSettings.SettingsKey] = "{ this is not json";
            var settings = new MainSettings { @namespace = "Changed" };

            // Act
            settings.Load(store);

            // Assert
            Assert.AreEqual(new MainSettings().@namespace, settings.@namespace);
            Assert.IsNotNull(settings.localSource);
            Assert.IsNotNull(settings.fetcher);
        }

        [Test]
        public void Load_Migrates_The_GitLab_Repository_And_Saves_It()
        {
            // Arrange
            var store = new InMemorySettingsStore();
            var old = new MainSettings();
            old.localSource.repoUrl = "https://gitlab.com/learntech-rwth/xapi.git";
            old.localSource.repoBranch = "master";
            old.Save(store);
            store.Writes = 0;

            // Act
            var settings = new MainSettings();
            settings.Load(store);

            // Assert
            Assert.AreEqual("https://github.com/OmiLAXR-Ecosystem/xapi.git", settings.localSource.repoUrl);
            Assert.AreEqual("main", settings.localSource.repoBranch);
            Assert.AreEqual(1, store.Writes);
            StringAssert.Contains("OmiLAXR-Ecosystem", store.Values[MainSettings.SettingsKey]);
        }

        [Test]
        public void Load_Does_Not_Save_Current_Settings()
        {
            // Arrange
            var store = new InMemorySettingsStore();
            new MainSettings().Save(store);
            store.Writes = 0;

            // Act
            new MainSettings().Load(store);

            // Assert
            Assert.AreEqual(0, store.Writes);
        }
    }
}
#endif
