/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Settings;

namespace xAPI4Unity.Editor.Releases
{
    /// <summary>
    /// The releases of the official definitions (<see cref="DefaultValues.OfficialReleasesUrl"/>), loaded from the GitHub API.
    /// The list is cached in SessionState for the editor session, because GitHub allows 60 requests per hour without sign-in.
    /// </summary>
    internal static class OfficialReleases
    {
        /// <summary>
        /// SessionState key of the cached API response.
        /// </summary>
        private const string CacheKey = "xAPI4Unity.OfficialReleases.Json";
        /// <summary>
        /// SessionState key of the time of the cached response, as UTC ticks.
        /// </summary>
        private const string CacheTimeKey = "xAPI4Unity.OfficialReleases.Time";
        /// <summary>
        /// SessionState key set once the startup check ran in this editor session.
        /// </summary>
        private const string StartupCheckKey = "xAPI4Unity.OfficialReleases.StartupChecked";
        /// <summary>
        /// A cached list younger than this is used without asking GitHub again.
        /// </summary>
        private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The releases, or null if they were not loaded yet.
        /// </summary>
        private static ReleaseList _releases;
        /// <summary>
        /// True once <see cref="_releases"/> was read from the cache.
        /// </summary>
        private static bool _cacheRead;
        /// <summary>
        /// Callbacks of the running request.
        /// </summary>
        private static event Action Loaded;

        /// <summary>
        /// Raised on the main thread when a request starts or ends, so windows can redraw.
        /// </summary>
        public static event Action Changed;

        /// <summary>
        /// True while the list is loading.
        /// </summary>
        public static bool IsLoading { get; private set; }

        /// <summary>
        /// Error of the last request, or null if it succeeded.
        /// </summary>
        public static string Error { get; private set; }

        /// <summary>
        /// The releases, newest first, or null if they were never loaded in this editor session.
        /// </summary>
        public static ReleaseList Releases
        {
            get
            {
                if (!_cacheRead)
                {
                    _cacheRead = true;
                    var json = SessionState.GetString(CacheKey, null);
                    if (!string.IsNullOrEmpty(json))
                    {
                        try { _releases = ReleaseList.Parse(json); }
                        catch (ArgumentException) { _releases = null; }
                    }
                }
                return _releases;
            }
        }

        /// <summary>
        /// When the cached list was loaded, or null if there is none.
        /// </summary>
        public static DateTime? LoadedAt
        {
            get
            {
                var ticks = SessionState.GetString(CacheTimeKey, null);
                return long.TryParse(ticks, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? new DateTime(value, DateTimeKind.Utc)
                    : (DateTime?)null;
            }
        }

        /// <summary>
        /// Version installed in the source folder (its <c>version.txt</c>), or null if unknown.
        /// </summary>
        public static string InstalledVersion => ReleaseInstaller.ReadVersion(MainSettings.Instance.localSource.path);

        /// <summary>
        /// The newest release if the project uses the official releases and has an older one installed. Otherwise null.
        /// </summary>
        public static ReleaseInfo AvailableUpdate
        {
            get
            {
                var settings = MainSettings.Instance.localSource;
                return settings.IsOfficial ? Releases?.UpdateFor(InstalledVersion) : null;
            }
        }

        /// <summary>
        /// Loads the list from GitHub, unless the cached list is younger than <see cref="MaxAge"/>.
        /// </summary>
        /// <param name="force">Ask GitHub even if the cached list is fresh.</param>
        /// <param name="done">Called on the main thread once the list is loaded or failed. Also called right away when the cache is used.</param>
        public static void Refresh(bool force = false, Action done = null)
        {
            var loadedAt = LoadedAt;
            if (!force && Releases != null && loadedAt.HasValue && DateTime.UtcNow - loadedAt.Value < MaxAge)
            {
                done?.Invoke();
                return;
            }

            if (done != null)
                Loaded += done;
            if (IsLoading)
                return;

            IsLoading = true;
            Changed?.Invoke();
            WebRequests.GetText(DefaultValues.OfficialReleasesApiUrl, (json, error) =>
            {
                if (error == null)
                {
                    try
                    {
                        _releases = ReleaseList.Parse(json);
                        _cacheRead = true;
                        SessionState.SetString(CacheKey, json);
                        SessionState.SetString(CacheTimeKey, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
                    }
                    catch (ArgumentException e)
                    {
                        error = $"Unexpected answer from GitHub: {e.Message}";
                    }
                }
                Error = error;
                IsLoading = false;

                var callbacks = Loaded;
                Loaded = null;
                callbacks?.Invoke();
                Changed?.Invoke();
            });
        }

        /// <summary>
        /// Checks once per editor session whether a newer official release exists, and logs it to the Console.
        /// Runs only for projects that use the official releases and have one installed.
        /// </summary>
        public static void CheckOnStartup()
        {
            if (SessionState.GetBool(StartupCheckKey, false))
                return;
            SessionState.SetBool(StartupCheckKey, true);

            var settings = MainSettings.Instance.localSource;
            if (!settings.IsOfficial || InstalledVersion == null)
                return;

            Refresh(true, () =>
            {
                var update = AvailableUpdate;
                if (update != null)
                    Debug.Log($"[xAPI4Unity] xAPI definitions {update.tag_name} are available (installed: v{InstalledVersion}). " +
                              "Update them in 'xAPI4Unity > Open Wizard' (step 'Source').");
            });
        }
    }
}
#endif
