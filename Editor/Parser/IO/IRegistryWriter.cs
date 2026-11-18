/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Stores a generated registry.
    /// </summary>
    internal interface IRegistryWriter
    {
        /// <summary>
        /// Writes all files of the registry and replaces what was there before.
        /// </summary>
        void Write(GeneratedRegistry registry);
    }
}
#endif
