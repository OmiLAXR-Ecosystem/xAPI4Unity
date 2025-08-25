/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace xAPI4Unity.Editor.Settings
{
    /// <summary>
    /// Stores settings related to the fetcher utility.
    /// </summary>
    [Serializable]
    public class FetcherSettings : ISettings
    {
        /// <summary>
        /// Represents arguments for the fetcher.
        /// </summary>
        public class FetcherArguments : Dictionary<string, string> { }

        [SerializeField] public string localPath = DefaultValues.HomeDirectory;
        [SerializeField] public bool watcherEnabled = DefaultValues.WatcherEnabled;

        /// <inheritdoc />
        public T Clone<T>()
        {
            var f = new FetcherSettings
            {
                localPath = localPath,
                watcherEnabled = watcherEnabled
            };
            return (T)(object)f;
        }

        /// <inheritdoc />
        public bool Equals(ISettings s)
        {
            var f = s as FetcherSettings;
            return watcherEnabled == f.watcherEnabled && localPath == f.localPath;
        }
    }
}