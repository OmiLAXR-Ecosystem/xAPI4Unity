using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace xAPI4Unity.Editor
{
    public class XapiFolderTreeWindow : EditorWindow
    {
        private Settings settings => Settings.Instance;
        private Vector2 scroll;
        private Dictionary<string, bool> folderStates = new Dictionary<string, bool>();

        [MenuItem("xAPI4Unity/Edit xAPI definitions (experimental)")]
        public static void ShowWindow()
        {
            GetWindow<XapiFolderTreeWindow>("Assets/xapi definitions");
        }

        private void OnGUI()
        {
            var rootFolder = settings.localSource.path;
            GUILayout.Label($"Showing file structure for: {rootFolder}", EditorStyles.boldLabel);

            if (!Directory.Exists(rootFolder))
            {
                EditorGUILayout.HelpBox($"{rootFolder} does not exist.", MessageType.Warning);
                if (GUILayout.Button("Refresh"))
                    Repaint();
                return;
            }

            if (GUILayout.Button("Refresh"))
                Repaint();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawDirectory(rootFolder, 0);

            EditorGUILayout.EndScrollView();
        }

        private void DrawDirectory(string path, int depth)
        {
            var folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(folderName))
                folderName = path; // root fallback

            // Ensure state exists for this folder
            if (!folderStates.ContainsKey(path))
                folderStates[path] = true;

            // Indent with GUILayout.Space (works in EditorWindow!)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 16f);
            folderStates[path] = EditorGUILayout.Foldout(folderStates[path], folderName, true);
            EditorGUILayout.EndHorizontal();

            if (!folderStates[path])
                return;

            // List files (no .meta)
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                if (!file.EndsWith(".json")) continue;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * 16f);
                GUILayout.Label(Path.GetFileName(file), EditorStyles.label);
                EditorGUILayout.EndHorizontal();
            }

            // Draw subdirectories
            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                DrawDirectory(dir, depth + 1);
            }
        }
    }
}