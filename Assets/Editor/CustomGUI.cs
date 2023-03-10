using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    public static class CustomGUI
    {
        private static readonly GUIStyle TitleLabelStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 16,
        };

        private static readonly GUIStyle SubtitleLabelStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 14,
        };

        private static readonly GUIStyle ButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(5, 5, -5, 0)
        };

        private static readonly GUIStyle TextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true
        };
        
        private static readonly GUIStyle ErrorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true,
            normal = new GUIStyleState()
            {
                textColor = Color.red
            }
        };

        public static bool Button(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, ButtonStyle, (new[]
            {
                GUILayout.Height(30),
                GUILayout.ExpandWidth(false)
            }).Concat(options).ToArray());
        }

        public static bool StateButton(string normalText, bool isStateMode, string stateText,
            params GUILayoutOption[] options)
        {
            BeginDisabledGroup(isStateMode);
            var result = Button(isStateMode ? stateText : normalText, options);
            EndDisabledGroup();
            return result;
        }

        public static void Title(string text)
            => GUILayout.Label(text, TitleLabelStyle);

        public static void Subtitle(string text)
            => GUILayout.Label(text, SubtitleLabelStyle);

        public static void Label(string text)
            => GUILayout.Label(text);

        public static void Description(string text)
            => GUILayout.Label(text, TextStyle, GUILayout.ExpandWidth(true));

        public static void Error(string text)
            => GUILayout.Label(text, ErrorStyle, GUILayout.ExpandWidth(true));

        public static void Space(int lines = 1)
        {
            for (var i = 0; i < lines; i++)
                EditorGUILayout.Space();
        }

        public static string Popup(string label, IEnumerable<string> options, string value)
        {
            var opts = new[] { "" };
            opts = opts.Concat(options).ToArray();

            if (string.IsNullOrEmpty(value))
                value = "";

            var optsIndex = Array.IndexOf(opts, value);
            if (optsIndex < 0)
                optsIndex = 0;

            return opts[EditorGUILayout.Popup(label, optsIndex, opts)];
        }

        public static T Popup<T>(string label, T value) where T : Enum
            => (T)EditorGUILayout.EnumPopup(label, value, GUILayout.MaxWidth(300));

        public static void Divider(int spaceLines = 1)
        {
            Space(spaceLines);
            const float height = 1.0f;
            var rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            Space(spaceLines);
        }

        public static string TextField(string label, string text, string defaultText = "",
            params GUILayoutOption[] options)
            => EditorGUILayout.TextField(label, string.IsNullOrEmpty(text) ? defaultText : text, (new[]
            {
                GUILayout.MinWidth(250)
            }).Concat(options).ToArray());

        public static void BeginHorizontal() => EditorGUILayout.BeginHorizontal();
        public static void EndHorizontal() => EditorGUILayout.EndHorizontal();
        public static void BeginDisabledGroup(bool conditions) => EditorGUI.BeginDisabledGroup(conditions);
        public static void EndDisabledGroup() => EditorGUI.EndDisabledGroup();
    }
}