/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Provides the xAPI definition files a registry is generated from.
    /// </summary>
    internal interface IDefinitionSource
    {
        /// <summary>
        /// Reads all definitions. Each <see cref="DefinitionFile.Path"/> is relative to the
        /// definitions root, such as <c>generic/verbs/accepted.json</c>.
        /// </summary>
        IReadOnlyList<DefinitionFile> ReadDefinitions();
    }
}
#endif
