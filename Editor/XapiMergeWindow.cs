/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Settings;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Editor window for resolving merge conflicts from xAPI merge reports.
    /// </summary>
    public class XapiMergeWindow : EditorWindow
    {
        /// <summary>
        /// Represents a file conflict including the relative path, existing file content, 
        /// and incoming conflicting file content.
        /// </summary>
        private class Conflict
        {
            public string RelativePath; // Path of the conflicting file within the repository
            public string ExistingContent; // Content of the existing file
            public string IncomingContent; // Content of the incoming conflicting file
            public string IncomingFile; // Full path to the incoming conflicting file
            public string ExistingFile; // Full path to the existing file
        }

        private Vector2 _scrollPos; // Scroll position for the conflict list
        private string[] _reportPaths; // List of available merge report file paths
        private int _selectedReportIndex = 0; // Index of the currently selected merge report
        
        private MergeReport _mergeReport; // Currently loaded a merge report
        
        private readonly List<Conflict> _conflicts = new List<Conflict>(); // List of conflicts for resolution
        private readonly Dictionary<string, Vector2> _existingScrolls = new Dictionary<string, Vector2>(); // Scroll positions for "existing" files
        private readonly Dictionary<string, Vector2> _incomingScrolls = new Dictionary<string, Vector2>(); // Scroll positions for "incoming" files

        /// <summary>
        /// Displays the xAPI Conflict Resolver window.
        /// </summary>
        [MenuItem("xAPI4Unity / Resolve Conflicts from Report", priority = 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<XapiMergeWindow>(true, "xAPI Conflict Resolver");
            window.minSize = new Vector2(1200, 600);
            window.LoadReportOptions();
        }

        /// <summary>
        /// Loads available merge reports and initializes the first report if present.
        /// </summary>
        private void LoadReportOptions()
        {
            var definitionsRoot = MainSettings.Instance.localSource.path;
            _reportPaths = Directory
                .GetFiles(definitionsRoot, "*-xapi-report.json", SearchOption.TopDirectoryOnly)
                .Select(p => p.Replace("\\", "/"))
                .OrderByDescending(Path.GetFileName)
                .ToArray();

            _selectedReportIndex = _reportPaths.Length > 0 ? 0 : -1;
            if (_selectedReportIndex >= 0)
                Initialize(definitionsRoot, _reportPaths[_selectedReportIndex]);
        }

        /// <summary>
        /// Loads a merge report file and deserializes it into a MergeReport object.
        /// </summary>
        private MergeReport LoadMergeReport(string reportPath)
        {
            if (!File.Exists(reportPath))
            {
                Debug.LogError($"Merge report not found: {reportPath}");
                return null;
            }

            var json = File.ReadAllText(reportPath);
            return JsonUtility.FromJson<MergeReport>(json);
        }

        /// <summary>
        /// Initializes the conflict resolution window with a specific merge report.
        /// </summary>
        /// <param name="definitionsRoot">Root directory for definitions.</param>
        /// <param name="reportPath">Path to the merge report file.</param>
        private void Initialize(string definitionsRoot, string reportPath)
        {
            var report = LoadMergeReport(reportPath);
            if (report == null)
                return;
            _mergeReport = report;

            _conflicts.Clear();

            // Populate the list of conflicts from the report
            foreach (var conflict in report.ConflictResults)
            {
                var file = Path.Combine(definitionsRoot, conflict.path);
                var conflictFile = Path.Combine(definitionsRoot, conflict.path + "-conflict");

                if (!File.Exists(conflictFile))
                    continue;

                _conflicts.Add(new Conflict
                {
                    RelativePath = conflict.path,
                    ExistingFile = file,
                    IncomingFile = conflictFile,
                    ExistingContent = File.ReadAllText(file),
                    IncomingContent = File.ReadAllText(conflictFile)
                });
            }
        }

        private void OnGUI()
        {
            DrawReportDropdown();

            // Exit if no report is selected or there are no conflicts
            if (_selectedReportIndex < 0 || _mergeReport == null)
                return;

            if (_conflicts.Count == 0)
            {
                EditorGUILayout.HelpBox("No conflicts to resolve.", MessageType.Info);
                if (GUILayout.Button("Close"))
                    Close();
                return;
            }

            // Render the list of conflicts
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (var i = _conflicts.Count - 1; i >= 0; i--)
            {
                var c = _conflicts[i];
                CustomGUI.Space(2);
                EditorGUILayout.LabelField($"File: {c.RelativePath}", EditorStyles.boldLabel);

                bool acceptLeft = false, acceptRight = false;

                CustomGUI.Horizontal.Wrap(() =>
                {
                    CustomGUI.Vertical.Wrap(() =>
                    {
                        // Display existing file content
                        EditorGUILayout.LabelField("Existing:", EditorStyles.miniBoldLabel);

                        if (!_existingScrolls.ContainsKey(c.RelativePath))
                            _existingScrolls[c.RelativePath] = Vector2.zero;

                        _existingScrolls[c.RelativePath] = EditorGUILayout.BeginScrollView(
                            _existingScrolls[c.RelativePath], GUILayout.ExpandWidth(true), GUILayout.MinHeight(150), GUILayout.ExpandHeight(true));
                        EditorGUILayout.TextArea(c.ExistingContent, EditorStyles.textArea, GUILayout.ExpandHeight(true));
                        EditorGUILayout.EndScrollView();

                        if (GUILayout.Button("Accept left", GUILayout.ExpandWidth(true)))
                        {
                            acceptLeft = true;
                        }
                    }, GUILayout.Width(position.width / 2 - 15));

                    CustomGUI.Vertical.Wrap(() =>
                    {
                        // Display incoming file content
                        EditorGUILayout.LabelField("Incoming:", EditorStyles.miniBoldLabel);

                        if (!_incomingScrolls.ContainsKey(c.RelativePath))
                            _incomingScrolls[c.RelativePath] = Vector2.zero;

                        _incomingScrolls[c.RelativePath] = EditorGUILayout.BeginScrollView(
                            _incomingScrolls[c.RelativePath], GUILayout.ExpandWidth(true), GUILayout.MinHeight(150), GUILayout.ExpandHeight(true));
                        EditorGUILayout.TextArea(c.IncomingContent, EditorStyles.textArea, GUILayout.ExpandHeight(true));
                        EditorGUILayout.EndScrollView();

                        if (GUILayout.Button("Accept right", GUILayout.ExpandWidth(true)))
                        {
                            acceptRight = true;
                        }
                    }, GUILayout.Width(position.width / 2 - 15));
                });

                // Apply actions after layout
                if (acceptLeft)
                {
                    File.Delete(c.IncomingFile); // Delete conflict file
                    _conflicts.RemoveAt(i); // Remove conflict from a list
                }
                else if (acceptRight)
                {
                    File.Delete(c.ExistingFile); // Delete existing file
                    File.Move(c.IncomingFile, c.ExistingFile); // Replace it with an incoming file
                    _conflicts.RemoveAt(i); // Remove conflict from a list
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the dropdown for selecting merge reports and loads the selected report.
        /// </summary>
        private void DrawReportDropdown()
        {
            if (_reportPaths == null || _reportPaths.Length == 0)
            {
                EditorGUILayout.HelpBox("No xAPI report files found in 'Assets/xapi'.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Select Merge Report", EditorStyles.boldLabel);
            var newIndex = EditorGUILayout.Popup(_selectedReportIndex, _reportPaths.Select(Path.GetFileName).ToArray());

            if (newIndex != _selectedReportIndex)
            {
                _selectedReportIndex = newIndex;
                Initialize(MainSettings.Instance.localSource.path, _reportPaths[_selectedReportIndex]);
            }
        }
    }
}

#endif