using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    public static class CustomGUI
    {
        public static class Horizontal
        {
            public static void Begin() => EditorGUILayout.BeginHorizontal();
            public static void End() => EditorGUILayout.EndHorizontal();

            public static void Wrap(Action action)
            {
                Begin();
                action();
                End();
            }
        }
        public class ScrollViewGUI
        {
            private Vector2 _scrollPos;
            private ScrollViewGUI(Vector2 startScroll)
            {
                _scrollPos = startScroll;
            }

            private void Begin(params GUILayoutOption[] options)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, options);
            }

            private void End()
            {
                EditorGUILayout.EndScrollView();
            }

            public static void Wrap(Action action, Vector2 startScroll = default, params GUILayoutOption[] options)
            {
                var scrollView = new ScrollViewGUI(startScroll);
                scrollView.Begin(options);
                action();
                scrollView.End();
            }
        }
        
        private static readonly GUIStyle TitleLabelStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 16,
        };

        private static readonly GUIStyle SubtitleLabelStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 14,
        };

        private static readonly GUIStyle StepLabelStyleGreen = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 14,
            normal = new GUIStyleState()
            {
                textColor = Color.green
            },
            hover = new GUIStyleState()
            {
                textColor = Color.green
            }
        };
        
        private static readonly GUIStyle StepLabelStyleRed = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 14,
            normal = new GUIStyleState()
            {
                textColor = Color.red
            },
            hover = new GUIStyleState()
            {
                textColor = Color.red
            }
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
            },
            hover = new GUIStyleState()
            {
                textColor = Color.red
            }
        };
        
        private static readonly GUIStyle WarningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true,
            normal = new GUIStyleState()
            {
                textColor = Color.yellow
            },
            hover = new GUIStyleState()
            {
                textColor = Color.yellow
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

        public static bool YesNoPopup(string label, bool value, string yesWord = "Yes", string noWord = "No")
        {
            var v = value ? yesWord : noWord;
            Horizontal.Begin();
            v = Popup(label, new[] { yesWord, noWord }, v);
            Horizontal.End();
            return v == yesWord;
        }

        public static bool StateButton(string normalText, bool isStateMode, string stateText,
            params GUILayoutOption[] options)
        {
            BeginDisabledGroup(isStateMode);
            var result = Button(isStateMode ? stateText : normalText, options);
            EndDisabledGroup();
            return result;
        }

        private const string CorrectMark = "✓";
        private const string WrongMark = "✗";

        public static void Title(string text)
            => GUILayout.Label(text, TitleLabelStyle);

        public static void Subtitle(string text)
            => GUILayout.Label(text, SubtitleLabelStyle);

        public static void Step(string text, bool checkedCondition)
            => GUILayout.Label(text + " " + (checkedCondition ? CorrectMark : WrongMark), checkedCondition ? StepLabelStyleGreen : StepLabelStyleRed);

        public static void Label(string text)
            => GUILayout.Label(text);

        public static void Description(string text)
            => GUILayout.Label(text, TextStyle, GUILayout.ExpandWidth(true));

        public static void Error(string text)
            => GUILayout.Label(text, ErrorStyle, GUILayout.ExpandWidth(true));
        
        public static void Warning(string text)
            => GUILayout.Label(text, WarningStyle, GUILayout.ExpandWidth(true));

        public static void Space(int lines = 1)
        {
            for (var i = 0; i < lines; i++)
                EditorGUILayout.Space();
        }

        public static string Popup(string label, IEnumerable<string> selectionOptions, string value,
            params GUILayoutOption[] options)
        {
            var opts = selectionOptions.Prepend("").ToArray();

            if (string.IsNullOrEmpty(value))
                value = "";

            var optsIndex = Array.IndexOf(opts, value);
            if (optsIndex < 0)
                optsIndex = 0;
            
            var newOptions = options.Prepend(GUILayout.MaxWidth(300)).ToArray();

            return opts[EditorGUILayout.Popup(label, optsIndex, opts, newOptions)];
        }

        public static T Popup<T>(string label, T value, params GUILayoutOption[] options) where T : Enum
        {
            var newOptions = options.Prepend(GUILayout.MaxWidth(300)).ToArray();
            return (T)EditorGUILayout.EnumPopup(label, value, newOptions);
        }

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


        public static void BeginVertical() => EditorGUILayout.BeginVertical();
        public static void EndVertical() => EditorGUILayout.EndVertical();
        public static void BeginDisabledGroup(bool conditions) => EditorGUI.BeginDisabledGroup(conditions);
        public static void EndDisabledGroup() => EditorGUI.EndDisabledGroup();
    }
}