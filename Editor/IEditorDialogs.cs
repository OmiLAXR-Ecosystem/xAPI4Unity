/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using UnityEditor;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Asks the user. Code that needs a confirmation depends on this interface instead of
    /// <see cref="EditorUtility.DisplayDialog(string,string,string,string)"/>, so tests can answer.
    /// </summary>
    internal interface IEditorDialogs
    {
        /// <summary>
        /// Shows a question with two buttons.
        /// </summary>
        /// <returns>True if the user chose <paramref name="ok"/>.</returns>
        bool Confirm(string title, string message, string ok, string cancel);
    }

    /// <summary>
    /// Shows Unity's modal editor dialogs.
    /// </summary>
    internal sealed class UnityEditorDialogs : IEditorDialogs
    {
        public static readonly UnityEditorDialogs Instance = new UnityEditorDialogs();

        private UnityEditorDialogs() { }

        /// <inheritdoc/>
        public bool Confirm(string title, string message, string ok, string cancel) =>
            EditorUtility.DisplayDialog(title, message, ok, cancel);
    }
}
#endif
