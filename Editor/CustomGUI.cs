#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

namespace xAPI4Unity.Editor
{
    internal static class CustomGUI
    {
        internal struct Tab
        {
            internal readonly Action ClickTab;
            internal readonly Action Content;
            internal readonly string TabLabel;

            public Tab(string tabLabel, Action content, Action onClickTab = null)
            {
                Content = content;
                TabLabel = tabLabel;
                ClickTab = onClickTab;
            }
        }

        internal static int Tabs(int selectedTab, params Tab[] tabs)
        {
            Horizontal.Wrap(() =>
            {
                for (var i = 0; i < tabs.Length; i++)
                {
                    var tab = tabs[i];
                    Disabled(i == selectedTab, () =>
                    {
                        if (Button(tab.TabLabel))
                        {
                            selectedTab = i;
                            tab.ClickTab?.Invoke();
                        }
                    });
                }
            });

            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                Region(i != selectedTab, tab.Content);
            }

            return selectedTab;
        }
        
        internal static class Horizontal
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
        internal class ScrollViewGUI
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

        private static readonly GUIStyle ListboxItemStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(5, 5, 0, 0),
            border = new RectOffset(1,1,1,1),
            alignment = TextAnchor.MiddleLeft,
        };
        private static readonly GUIStyle ListboxSelectedItemStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(5, 5, 0, 0),
            border = new RectOffset(1,1,1,1),
            alignment = TextAnchor.MiddleLeft,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
                background = MakeTex(2, 2, new Color(1f, 1f, 1f, 0.3f)),
            },
            
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

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            var pixels = new Color[width * height];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();

            return result;
        }
        
        
        public static int Listbox(string label, string[] entries, ref Vector2 scrollPosition, int selectedIndex, int width, int height, bool alwaysShowHorizontal = false,
            bool alwaysShowVertical = false, Action<string> onClick = null)
        {
            BeginVertical();
            Label(label);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, 
                alwaysShowHorizontal, alwaysShowVertical, 
                GUILayout.Height(height), GUILayout.Width(width));

            for (var i = 0; i < entries.Length; i++)
            {
                if (GUILayout.Button(entries[i], i == selectedIndex ? ListboxSelectedItemStyle : ListboxItemStyle))
                {
                    selectedIndex = i;
                    onClick?.Invoke(entries[i]);
                }
            }
            if (entries.Length < 1)
                Label("No items.");
            
            EditorGUILayout.EndScrollView();
            EndVertical();
            return selectedIndex;
        }
        
        public static bool Button(string textContent,  params GUILayoutOption[] options)
            => Button(new GUIContent(textContent), options);
        public static bool Button(GUIContent textContent,  params GUILayoutOption[] options)
        {
            var down = GUILayout.Button(textContent, ButtonStyle, (new[]
            {
                GUILayout.Height(30),
                GUILayout.ExpandWidth(false)
            }).Concat(options).ToArray());
            
            return down;
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
            var result = false;
            Disabled(isStateMode, () =>
            {
                result = Button(isStateMode ? stateText : normalText, options);
            });
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

        public static void Bullets(params string[] bullets)
        {
            for (var i = 0; i < bullets.Length; i++)
            {
                var bullet = bullets[i];
                GUILayout.Label((i + 1) + ". " + bullet, TextStyle, GUILayout.ExpandWidth(true));
            }
        }
        
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

            var newValue = opts[EditorGUILayout.Popup(label, optsIndex, opts, newOptions)];
            return newValue;
        }

        public static T Popup<T>(string label, T value, params GUILayoutOption[] options) where T : Enum
        {
            var newOptions = options.Prepend(GUILayout.MaxWidth(300)).ToArray();
            var newValue = (T)EditorGUILayout.EnumPopup(label, value, newOptions);
            return newValue;
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

        public static void DividedRegion(bool hidden, Action content, int spaceLines = 1)
        {
            if (hidden)
                return;
            DividedRegion(content, spaceLines);
        }
        
        public static void DividedRegion(Action content, int spaceLines = 1)
        {
            Divider(spaceLines);
            content();
            Divider(spaceLines);
        }
        
        public static void Region(bool hidden, Action content, int spaceLines = 1)
        {
            if (hidden)
                return;
            Region(content, spaceLines);
        }
        
        public static void Region(Action content, int spaceLines = 1)
        {
            Space(spaceLines);
            content();
            Space(spaceLines);
        }

        public static string TextField(string label, string text, string defaultText = "",
             params GUILayoutOption[] options)
        {
            var newText = EditorGUILayout.TextField(label, string.IsNullOrEmpty(text) ? defaultText : text, (new[]
            {
                GUILayout.MinWidth(250)
            }).Concat(options).ToArray());
            
            return newText;
        }


        public static void BeginVertical() => EditorGUILayout.BeginVertical();
        public static void EndVertical() => EditorGUILayout.EndVertical();
        public static void BeginDisabledGroup(bool disabled = false) => EditorGUI.BeginDisabledGroup(disabled);
        public static void EndDisabledGroup() => EditorGUI.EndDisabledGroup();

        public static void Disabled(bool disabled, Action content)
        {
            BeginDisabledGroup(disabled);
            content();
            EndDisabledGroup();
        }

        public static void ScrollView(ref Vector2 scrollPosition, Action content)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            content();
            GUILayout.EndScrollView();
        }
    }
}
#endif