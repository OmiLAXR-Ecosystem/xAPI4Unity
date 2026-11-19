/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Provides the templates of the static registry core classes (<c>xAPI_Verb.cs</c>, …).
    /// </summary>
    internal interface ICoreTemplateSource
    {
        /// <summary>
        /// Returns the template text of a core file.
        /// </summary>
        /// <param name="fileName">Name of the generated file, such as <c>xAPI_Verb.cs</c>.</param>
        string Read(string fileName);
    }
}
#endif
