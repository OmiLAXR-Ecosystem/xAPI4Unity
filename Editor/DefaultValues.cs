/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    internal static class DefaultValues
    {
        public const bool WatcherEnabled = true;
        public const string RepoUrl = "git@gitlab.com:learntech-rwth/xapi.git";
        public const string RepoDownloadUrl = "https://gitlab.com/learntech-rwth/xapi";
        public const string RepoBranch = "master";
        public const string Namespace = "xAPI.Registry";
        public static string RepoPath => Path.Combine(AssetRoot, "xapi");
        public static string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static string AssetRoot => Application.dataPath;
    }
}
#endif