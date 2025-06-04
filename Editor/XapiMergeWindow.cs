#if UNITY_EDITOR 

using System;
using System.Collections.Generic;
using System.IO;
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
            public ConflictResolution Resolution = ConflictResolution.Undecided;
        }

        private enum ConflictResolution
        {
            Undecided,
            KeepExisting,
            ReplaceWithIncoming,
            Skip
        }

        private List<Conflict> _conflicts = new List<Conflict>();
        private Vector2 _scrollPos;
        private string _existingFolder;
        private string _incomingFolder;

        [MenuItem("xAPI4Unity / Resolve Registry Merge Conflicts")]
        public static void ShowWindow()
        {
            var window = GetWindow<XapiMergeWindow>(true, "xAPI Merge Conflict Resolver");
            window.minSize = new Vector2(900, 600);
            window.Initialize("Assets/xapi", Path.Combine(Application.dataPath, "__ExtractedXAPI__/xapi-master"));
        }
        
        public static string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            var fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        public void Initialize(string existingFolder, string incomingFolder)
        {
            _existingFolder = existingFolder;
            _incomingFolder = incomingFolder;

            _conflicts.Clear();

            foreach (var newFilePath in Directory.GetFiles(incomingFolder, "*.json", SearchOption.AllDirectories))
            {
                var relativePath = GetRelativePath(incomingFolder, newFilePath).Replace("\\", "/");
                var existingFilePath = Path.Combine(existingFolder, relativePath);

                if (File.Exists(existingFilePath))
                {
                    var newContent = File.ReadAllText(newFilePath);
                    var existingContent = File.ReadAllText(existingFilePath);

                    if (newContent != existingContent)
                    {
                        _conflicts.Add(new Conflict
                        {
                            RelativePath = relativePath,
                            IncomingContent = newContent,
                            ExistingContent = existingContent
                        });
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (_conflicts.Count == 0)
            {
                EditorGUILayout.HelpBox("No conflicts detected.", MessageType.Info);
                if (GUILayout.Button("Close")) Close();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var conflict in _conflicts)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"File: {conflict.RelativePath}", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Keep Existing")) conflict.Resolution = ConflictResolution.KeepExisting;
                if (GUILayout.Button("Replace with Incoming"))
                    conflict.Resolution = ConflictResolution.ReplaceWithIncoming;
                if (GUILayout.Button("Skip")) conflict.Resolution = ConflictResolution.Skip;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Existing:", EditorStyles.miniBoldLabel);
                EditorGUILayout.TextArea(conflict.ExistingContent, GUILayout.Height(100));
                EditorGUILayout.LabelField("Incoming:", EditorStyles.miniBoldLabel);
                EditorGUILayout.TextArea(conflict.IncomingContent, GUILayout.Height(100));
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Apply Decisions")) ApplyResolutions();
        }

        private void ApplyResolutions()
        {
            foreach (var conflict in _conflicts)
            {
                var destFile = Path.Combine(_existingFolder, conflict.RelativePath);
                var srcFile = Path.Combine(_incomingFolder, conflict.RelativePath);

                switch (conflict.Resolution)
                {
                    case ConflictResolution.ReplaceWithIncoming:
                        File.Copy(srcFile, destFile, true);
                        Debug.Log($"Replaced: {conflict.RelativePath}");
                        break;
                    case ConflictResolution.KeepExisting:
                        Debug.Log($"Kept: {conflict.RelativePath}");
                        break;
                    case ConflictResolution.Skip:
                        Debug.Log($"Skipped: {conflict.RelativePath}");
                        break;
                    case ConflictResolution.Undecided:
                        Debug.LogWarning($"Undecided, skipping: {conflict.RelativePath}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", "Merge operation completed.", "OK");
            Close();
        }
    }
}

#endif