/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Kind of a definitions source. The generator reads local folders; <c>GitLab</c> is left from an earlier remote source.
    /// </summary>
    public enum SourceType
    {
        /// <summary>
        /// The GitLab repository, read remotely.
        /// </summary>
        GitLab,
        /// <summary>
        /// A folder on disk.
        /// </summary>
        Local
    }

    /// <summary>
    /// Where the wizard gets the definitions from.
    /// </summary>
    public enum DefinitionsSource
    {
        /// <summary>
        /// A released version of the official OmiLAXR registry, installed from its zip archive.
        /// </summary>
        Official,
        /// <summary>
        /// Any repository with a <c>definitions</c> folder, such as a fork: patch, zip download or git clone of a branch or commit.
        /// </summary>
        Custom
    }
}
#endif