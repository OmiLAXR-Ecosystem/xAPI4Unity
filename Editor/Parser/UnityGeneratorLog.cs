/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using UnityEngine;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Writes generator messages to the Unity console.
    /// </summary>
    internal sealed class UnityGeneratorLog : IGeneratorLog
    {
        /// <summary>
        /// Shared instance. The class has no state.
        /// </summary>
        public static readonly UnityGeneratorLog Instance = new UnityGeneratorLog();

        private UnityGeneratorLog() { }

        /// <inheritdoc/>
        public void Info(string message) => Debug.Log(message);

        /// <inheritdoc/>
        public void Warning(string message) => Debug.LogWarning(message);
    }
}
#endif
