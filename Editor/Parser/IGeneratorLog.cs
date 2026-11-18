/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Receives the messages of a registry generation run.
    /// The generator depends on this interface instead of <c>UnityEngine.Debug</c>,
    /// so tests can collect the messages and check them.
    /// </summary>
    internal interface IGeneratorLog
    {
        /// <summary>
        /// Reports progress or a detail of the run.
        /// </summary>
        void Info(string message);

        /// <summary>
        /// Reports a problem that the generator skipped over.
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// Reports a problem that stopped the run.
        /// </summary>
        void Error(string message);
    }
}
#endif
