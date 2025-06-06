#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    public class XapiMergeWindow : EditorWindow
    {
        private class Conflict
        {
            public string RelativePath;
            public string ExistingContent;
            public string IncomingContent;
            public string IncomingFile;
            public string ExistingFile;
        }

        private Vector2 _scrollPos;
        private string[] _reportPaths;
        private int _selectedReportIndex = 0;
        
        private MergeReport _mergeReport;
        
        private List<Conflict> _conflicts = new List<Conflict>();
        private Dictionary<string, Vector2> _existingScrolls = new();
        private Dictionary<string, Vector2> _incomingScrolls = new();

        [MenuItem("xAPI4Unity / Resolve Conflicts from Report", priority = 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<XapiMergeWindow>(true, "xAPI Conflict Resolver");
            window.minSize = new Vector2(1200, 600);
            window.LoadReportOptions();
        }

        private void LoadReportOptions()
        {
            var definitionsRoot = Settings.Instance.localSource.path;
            _reportPaths = Directory
                .GetFiles(definitionsRoot, "*-xapi-report.json", SearchOption.TopDirectoryOnly)
                .Select(p => p.Replace("\\", "/"))
                .OrderByDescending(Path.GetFileName)
                .ToArray();

            _selectedReportIndex = _reportPaths.Length > 0 ? 0 : -1;
            if (_selectedReportIndex >= 0)
                Initialize(definitionsRoot, _reportPaths[_selectedReportIndex]);
        }

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

        public void Initialize(string definitionsRoot, string reportPath)
        {
            var report = LoadMergeReport(reportPath);
            if (report == null)
                return;
            _mergeReport = report;

            _conflicts.Clear();

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

            if (_selectedReportIndex < 0 || _mergeReport == null)
                return;

            if (_conflicts.Count == 0)
            {
                EditorGUILayout.HelpBox("No conflicts to resolve.", MessageType.Info);
                if (GUILayout.Button("Close"))
                    Close();
                return;
            }

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
                    File.Delete(c.IncomingFile);
                    _conflicts.RemoveAt(i);
                }
                else if (acceptRight)
                {
                    File.Delete(c.ExistingFile);
                    File.Move(c.IncomingFile, c.ExistingFile);
                    _conflicts.RemoveAt(i);
                }
            }

            EditorGUILayout.EndScrollView();
            
            
        }

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
                Initialize(Settings.Instance.localSource.path, _reportPaths[_selectedReportIndex]);
            }
        }
    }
}

#endif