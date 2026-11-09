/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Provides custom GUI functionality for Unity Editor with extended layouts and styles.
    /// </summary>
    internal static class CustomGUI
    {
        /// <summary>
        /// Represents a tab within a tabbed GUI interface.
        /// </summary>
        internal struct Tab
        {
            internal readonly Action ClickTab; // Action to perform when the tab is clicked.
            internal readonly Action Content;  // Action defining the tab's main content.
            internal readonly string TabLabel; // The label of the tab.

            /// <summary>
            /// Initializes a new instance of a Tab with the specified label, content, and click action.
            /// </summary>
            /// <param name="tabLabel">The label of the tab.</param>
            /// <param name="content">The content to render when the tab is selected.</param>
            /// <param name="onClickTab">An optional action to perform when the tab is clicked.</param>
            public Tab(string tabLabel, Action content, Action onClickTab = null)
            {
                Content = content;
                TabLabel = tabLabel;
                ClickTab = onClickTab;
            }
        }

        private static readonly int TabsLimit = 3; // Maximum number of tabs before switching to dropdown mode.
        
        /// <summary>
        /// Builds a tab layout allowing selection between multiple tabs.
        /// </summary>
        /// <param name="selectedTab">The currently selected tab index.</param>
        /// <param name="tabs">An array of Tab objects representing available options.</param>
        /// <returns>The index of the newly selected tab.</returns>
        internal static int Tabs(int selectedTab, params Tab[] tabs)
            => Tabs(selectedTab, tabs, null);

        /// <summary>
        /// Builds a tab layout with an optional callback when the selected tab is changed.
        /// </summary>
        /// <param name="selectedTab">The currently selected tab index.</param>
        /// <param name="tabs">An array of Tab objects representing available options.</param>
        /// <param name="onChangeTab">An optional callback triggered when a tab is selected.</param>
        /// <returns>The index of the newly selected tab.</returns>
        internal static int Tabs(int selectedTab, Tab[] tabs, Action<int> onChangeTab)
        {
            Horizontal.Wrap(() =>
            {
                if (tabs.Length > TabsLimit)
                {
                    // Render a dropdown when tabs exceed the limit.
                    var options = tabs.Select(t => t.TabLabel).ToArray();
                    selectedTab = Popup("Download Options", options, selectedTab,
                        index =>
                        {
                            tabs[index].ClickTab?.Invoke();
                            onChangeTab?.Invoke(index);
                        });
                }
                else
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
                                onChangeTab?.Invoke(i);
                            }
                        });
                    }
                }
            });

            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                Region(i != selectedTab, tab.Content);
            }

            return selectedTab;
        }

        /// <summary>
        /// Provides vertical layout helpers.
        /// </summary>
        internal static class Vertical
        {
            public static void Begin(params GUILayoutOption[] options) => EditorGUILayout.BeginVertical(options);
            public static void End() => EditorGUILayout.EndVertical();

            /// <summary>
            /// Wraps content between a vertical layout block.
            /// </summary>
            /// <param name="content">The content to display.</param>
            /// <param name="options">Optional layout options.</param>
            public static void Wrap(Action content, params GUILayoutOption[] options)
            {
                Begin(options);
                content();
                End();
            }
        }

        /// <summary>
        /// Provides horizontal layout helpers.
        /// </summary>
        internal static class Horizontal
        {
            public static void Begin(params GUILayoutOption[] options) => EditorGUILayout.BeginHorizontal(options);
            public static void End() => EditorGUILayout.EndHorizontal();

            /// <summary>
            /// Wraps content between a horizontal layout block.
            /// </summary>
            /// <param name="content">The content to display.</param>
            /// <param name="options">Optional layout options.</param>
            public static void Wrap(Action content, params GUILayoutOption[] options)
            {
                Begin(options);
                content();
                End();
            }
        }

        /// <summary>
        /// Provides a scrollable layout region.
        /// </summary>
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

            /// <summary>
            /// Creates a scrollable region wrapping the provided content.
            /// </summary>
            /// <param name="action">The content to display.</param>
            /// <param name="startScroll">The initial scroll state.</param>
            /// <param name="options">Optional layout options.</param>
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
        
        private static readonly GUIStyle HyperlinkStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true,
            normal = new GUIStyleState()
            {
                textColor = new Color(0.8f, 0.8f, 0) 
            },
            hover = new GUIStyleState()
            {
                textColor = new Color(1f, 1f, 0) 
            }
        };

        /// <summary>
        /// Creates a colored 2D texture of specified size.
        /// </summary>
        /// <param name="width">Width of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="color">Fill color of the texture.</param>
        /// <returns>Newly generated Texture2D object.</returns>
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
        
        
        /// <summary>
        /// Renders a styled listbox with selectable entries inside a scroll view.
        /// </summary>
        /// <param name="label">Label displayed above the listbox.</param>
        /// <param name="entries">Array of items to be displayed in the list.</param>
        /// <param name="scrollPosition">Reference to the current scroll position.</param>
        /// <param name="selectedIndex">Currently selected index.</param>
        /// <param name="width">Width of the listbox.</param>
        /// <param name="height">Height of the listbox.</param>
        /// <param name="alwaysShowHorizontal">Whether to always show horizontal scroll bar.</param>
        /// <param name="alwaysShowVertical">Whether to always show vertical scroll bar.</param>
        /// <param name="onClick">Optional callback triggered on item selection.</param>
        /// <returns>Index of the selected item.</returns>
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
        
        /// <summary>
        /// Renders a GUI button with the given text content and optional layout options.
        /// </summary>
        /// <param name="textContent">Text to display on the button.</param>
        /// <param name="options">Optional layout options.</param>
        /// <returns>True if the button is clicked.</returns>
        public static bool Button(string textContent,  params GUILayoutOption[] options)
            => Button(new GUIContent(textContent), options);
        /// <summary>
        /// Renders a GUI button with the given GUIContent and optional layout options.
        /// </summary>
        /// <param name="textContent">Content to display on the button (text, tooltip, etc.).</param>
        /// <param name="options">Optional layout options.</param>
        /// <returns>True if the button is clicked.</returns>
        public static bool Button(GUIContent textContent,  params GUILayoutOption[] options)
        {
            var down = GUILayout.Button(textContent, ButtonStyle, (new[]
            {
                GUILayout.Height(30),
                GUILayout.ExpandWidth(false)
            }).Concat(options).ToArray());
            
            return down;
        }

        /// <summary>
        /// Renders a Yes/No popup that maps a boolean value to two labels.
        /// </summary>
        /// <param name="label">Label displayed next to the popup.</param>
        /// <param name="value">Current boolean value.</param>
        /// <param name="onChange">Callback when value changes.</param>
        /// <param name="yesWord">Label for true value.</param>
        /// <param name="noWord">Label for false value.</param>
        /// <returns>New boolean value after user interaction.</returns>
        public static bool YesNoPopup(string label, bool value, Action<bool> onChange = null, string yesWord = "Yes", string noWord = "No")
        {
            var v = value ? yesWord : noWord;
            Horizontal.Begin();
            v = Popup(label, new[] { yesWord, noWord }, v);
            Horizontal.End();
            var newValue = v == yesWord;
            
            if (newValue != value)
                onChange?.Invoke(newValue);
            
            return newValue;
        }

        /// <summary>
        /// Displays a state-aware button that disables itself when in state mode.
        /// </summary>
        /// <param name="normalText">Text when not in state mode.</param>
        /// <param name="isStateMode">If true, button is shown as inactive.</param>
        /// <param name="stateText">Text when in state mode.</param>
        /// <param name="options">Optional layout options.</param>
        /// <returns>True if the button was clicked and active.</returns>
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

        /// <summary>
        /// Renders a large title label.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public static void Title(string text)
            => GUILayout.Label(text, TitleLabelStyle);
        /// <summary>
        /// Renders a smaller subtitle label.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public static void Subtitle(string text)
            => GUILayout.Label(text, SubtitleLabelStyle);
        /// <summary>
        /// Renders a labeled step with visual checkmark or cross.
        /// </summary>
        /// <param name="text">Step label.</param>
        /// <param name="checkedCondition">Determines if the step is valid.</param>
        public static void Step(string text, bool checkedCondition)
            => GUILayout.Label(text + " " + (checkedCondition ? CorrectMark : WrongMark), checkedCondition ? StepLabelStyleGreen : StepLabelStyleRed);
        /// <summary>
        /// Renders a plain label with default style.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public static void Label(string text)
            => GUILayout.Label(text);
        /// <summary>
        /// Renders a text block with default paragraph styling.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public static void Description(string text)
            => GUILayout.Label(text, TextStyle, GUILayout.ExpandWidth(true));
        /// <summary>
        /// Renders a numbered bullet list from the given strings.
        /// </summary>
        /// <param name="bullets">The list of bullet point texts.</param>
        public static void Bullets(params string[] bullets)
        {
            for (var i = 0; i < bullets.Length; i++)
            {
                var bullet = bullets[i];
                GUILayout.Label((i + 1) + ". " + bullet, TextStyle, GUILayout.ExpandWidth(true));
            }
        }
        
        /// <summary>
        /// Displays a hyperlink using default URL as display text.
        /// </summary>
        /// <param name="url">The URL to open when clicked.</param>
        public static void Hyperlink(string url)
            => Hyperlink(url, url);
        /// <summary>
        /// Displays a hyperlink label that opens the specified URL when clicked.
        /// </summary>
        /// <param name="text">The display text.</param>
        /// <param name="url">The URL to open.</param>
        public static void Hyperlink(string text, string url)
        {
            var content = new GUIContent(text);
            var rect = GUILayoutUtility.GetRect(content, HyperlinkStyle);
            
            // Verändere den Cursor zum Hand-Symbol wenn die Maus über dem Link ist
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (GUI.Button(rect, content, HyperlinkStyle))
            {
                Application.OpenURL(url);
            }
        }
        
        /// <summary>
        /// Renders an error message in red text.
        /// </summary>
        /// <param name="text">Error message text.</param>
        public static void Error(string text)
            => GUILayout.Label(text, ErrorStyle, GUILayout.ExpandWidth(true));
        /// <summary>
        /// Renders a warning message in yellow text.
        /// </summary>
        /// <param name="text">Warning message text.</param>
        public static void Warning(string text)
            => GUILayout.Label(text, WarningStyle, GUILayout.ExpandWidth(true));

        /// <summary>
        /// Inserts vertical space in the layout.
        /// </summary>
        /// <param name="lines">Number of vertical spaces to insert.</param>
        public static void Space(int lines = 1)
        {
            for (var i = 0; i < lines; i++)
                EditorGUILayout.Space();
        }

        /// <summary>
        /// Displays a popup menu for string-based selection.
        /// </summary>
        /// <param name="label">Label shown next to the dropdown.</param>
        /// <param name="selectionOptions">List of options to choose from.</param>
        /// <param name="value">Currently selected value.</param>
        /// <param name="options">Optional layout parameters.</param>
        /// <returns>The selected value.</returns>
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
        /// <summary>
        /// Displays a popup menu with integer index selection and optional change callback.
        /// </summary>
        /// <param name="label">Label shown next to the dropdown.</param>
        /// <param name="selectionOptions">List of options to choose from.</param>
        /// <param name="selectedIndex">Currently selected index.</param>
        /// <param name="onChange">Callback triggered if selection changes.</param>
        /// <param name="options">Optional layout parameters.</param>
        /// <returns>The selected index.</returns>
        public static int Popup(string label, IEnumerable<string> selectionOptions, int selectedIndex,
            Action<int> onChange = null,
            params GUILayoutOption[] options)
        {
            var opts = selectionOptions.ToArray();

            var newOptions = options.Prepend(GUILayout.MaxWidth(300)).ToArray();

            var newIndex = EditorGUILayout.Popup(label, selectedIndex, opts, newOptions);
            
            if (newIndex != selectedIndex)
                onChange?.Invoke(newIndex);
            
            return newIndex;
        }
        /// <summary>
        /// Displays a popup for selecting enum values.
        /// </summary>
        /// <typeparam name="T">Enum type to select from.</typeparam>
        /// <param name="label">Label shown next to the dropdown.</param>
        /// <param name="value">Current selected value.</param>
        /// <param name="onChange">Callback triggered on selection change.</param>
        /// <param name="options">Optional layout parameters.</param>
        /// <returns>The selected enum value.</returns>
        public static T Popup<T>(string label, T value, Action<T> onChange = null, params GUILayoutOption[] options) where T : Enum
        {
            var newOptions = options.Prepend(GUILayout.MaxWidth(300)).ToArray();
            var newValue = (T)EditorGUILayout.EnumPopup(label, value, newOptions);
            if (!newValue.Equals(value))
                onChange?.Invoke(newValue);
            return newValue;
        }

        /// <summary>
        /// Creates a visual divider with vertical spacing.
        /// </summary>
        /// <param name="spaceLines">Spacing lines before and after the divider.</param>
        public static void Divider(int spaceLines = 1)
        {
            Space(spaceLines);
            const float height = 1.0f;
            var rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            Space(spaceLines);
        }

        /// <summary>
        /// Wraps content inside a region with divider spacing, conditionally rendered.
        /// </summary>
        /// <param name="hidden">Whether the content should be hidden.</param>
        /// <param name="content">The content to render.</param>
        /// <param name="spaceLines">Number of lines before and after the divider.</param>
        public static void DividedRegion(bool hidden, Action content, int spaceLines = 1)
        {
            if (hidden)
                return;
            DividedRegion(content, spaceLines);
        }
        
        /// <summary>
        /// Wraps content inside a region with dividers.
        /// </summary>
        /// <param name="content">The content to render.</param>
        /// <param name="spaceLines">Number of lines before and after the divider.</param>
        public static void DividedRegion(Action content, int spaceLines = 1)
        {
            Divider(spaceLines);
            content();
            Divider(spaceLines);
        }
        
        /// <summary>
        /// Wraps content inside a vertical region with spacing, conditionally rendered.
        /// </summary>
        /// <param name="hidden">Whether the content should be hidden.</param>
        /// <param name="content">The content to render.</param>
        /// <param name="spaceLines">Number of lines before and after the region.</param>
        public static void Region(bool hidden, Action content, int spaceLines = 1)
        {
            if (hidden)
                return;
            Region(content, spaceLines);
        }
        
        /// <summary>
        /// Wraps content inside a vertical region with spacing.
        /// </summary>
        /// <param name="content">The content to render.</param>
        /// <param name="spaceLines">Number of lines before and after the region.</param>
        public static void Region(Action content, int spaceLines = 1)
        {
            Space(spaceLines);
            content();
            Space(spaceLines);
        }

        /// <summary>
        /// Displays a text field with optional default value.
        /// </summary>
        /// <param name="label">Label displayed next to the field.</param>
        /// <param name="text">Current text value.</param>
        /// <param name="defaultText">Default text if input is empty.</param>
        /// <param name="options">Optional layout options.</param>
        /// <returns>New or unchanged text value.</returns>
        public static string TextField(string label, string text, string defaultText = "",
             params GUILayoutOption[] options)
        {
            var newText = EditorGUILayout.TextField(label, string.IsNullOrEmpty(text) ? defaultText : text, (new[]
            {
                GUILayout.MinWidth(250)
            }).Concat(options).ToArray());
            
            return newText;
        }
        /// <summary>
        /// Begins a vertical layout block.
        /// </summary>
        public static void BeginVertical() => EditorGUILayout.BeginVertical();
        /// <summary>
        /// Ends the current vertical layout block.
        /// </summary>
        public static void EndVertical() => EditorGUILayout.EndVertical();
        /// <summary>
        /// Starts a disabled GUI group.
        /// </summary>
        /// <param name="disabled">Whether to disable GUI interaction.</param>
        public static void BeginDisabledGroup(bool disabled = false) => EditorGUI.BeginDisabledGroup(disabled);
        /// <summary>
        /// Ends a disabled GUI group.
        /// </summary>
        public static void EndDisabledGroup() => EditorGUI.EndDisabledGroup();
        /// <summary>
        /// Executes content within a disabled GUI context.
        /// </summary>
        /// <param name="disabled">If true, disables the group.</param>
        /// <param name="content">The content to render.</param>
        public static void Disabled(bool disabled, Action content)
        {
            BeginDisabledGroup(disabled);
            content();
            EndDisabledGroup();
        }
        /// <summary>
        /// Creates a scroll view layout and renders the provided content inside it.
        /// </summary>
        /// <param name="scrollPosition">Reference to the current scroll position.</param>
        /// <param name="content">Content to render inside the scroll view.</param>
        public static void ScrollView(ref Vector2 scrollPosition, Action content)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            content();
            GUILayout.EndScrollView();
        }
    }
}
#endif