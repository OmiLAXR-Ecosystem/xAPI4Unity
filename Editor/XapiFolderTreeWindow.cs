using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System;

[Serializable]
public class VerbDefinition
{
    public Localization name = new Localization();
    public Localization description = new Localization();
}

[Serializable]
public class Localization
{
    public string en_US;
    public string de_DE;
}

namespace xAPI4Unity.Editor
{
    public class XapiFolderTreeWindow : EditorWindow
    {
        private Settings settings => Settings.Instance;
        private Vector2 scroll;
        private Vector2 scrollRight;
        private Dictionary<string, bool> folderStates = new Dictionary<string, bool>();

        private string selectedJsonPath = null;
        private VerbDefinition currentVerb = null;
        private string lastLoadedJson = null;
        private bool showRawJson = false;
        private string editableRawJson = "";
        private Vector2 rawJsonScroll;

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

            EditorGUILayout.BeginHorizontal();

            // LEFT: Folder/File Tree
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Width(position.width / 2f));
            DrawDirectory(rootFolder, 0);
            EditorGUILayout.EndScrollView();

            // RIGHT: Details/Edit panel
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            scrollRight = EditorGUILayout.BeginScrollView(scrollRight);

            if (selectedJsonPath != null)
            {
                if (showRawJson)
                    DrawRawJsonEditor(selectedJsonPath);
                else
                    DrawJsonEditor(selectedJsonPath);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a .json file to edit.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDirectory(string path, int depth)
        {
            var folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(folderName))
                folderName = path;

            if (!folderStates.ContainsKey(path))
                folderStates[path] = true;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 16f);
            folderStates[path] = EditorGUILayout.Foldout(folderStates[path], folderName, true);
            EditorGUILayout.EndHorizontal();

            if (!folderStates[path])
                return;

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                if (!file.EndsWith(".json")) continue;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * 16f);

                GUIStyle style = (selectedJsonPath == file) ? EditorStyles.boldLabel : EditorStyles.label;

                if (GUILayout.Button(Path.GetFileName(file), style))
                {
                    selectedJsonPath = file;
                    LoadJson(file);
                    showRawJson = false;
                }

                EditorGUILayout.EndHorizontal();
            }

            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                DrawDirectory(dir, depth + 1);
            }
        }

        private void LoadJson(string path)
        {
            try
            {
                lastLoadedJson = File.ReadAllText(path, Encoding.UTF8);
                var replaced = lastLoadedJson.Replace("en-US", "en_US").Replace("de-DE", "de_DE");
                currentVerb = JsonUtility.FromJson<VerbDefinition>(replaced);
                if (currentVerb == null)
                    currentVerb = new VerbDefinition();
                editableRawJson = lastLoadedJson;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load JSON: " + ex.Message);
                currentVerb = new VerbDefinition();
                editableRawJson = "";
            }
        }

        private void DrawJsonEditor(string path)
        {
            EditorGUILayout.LabelField("Editing:", Path.GetFileName(path), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (currentVerb == null)
                LoadJson(path);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);
            currentVerb.name.en_US = EditorGUILayout.TextField("en-US", currentVerb.name.en_US);
            currentVerb.name.de_DE = EditorGUILayout.TextField("de-DE", currentVerb.name.de_DE);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            currentVerb.description.en_US = EditorGUILayout.TextField("en-US", currentVerb.description.en_US, GUILayout.Height(40));
            currentVerb.description.de_DE = EditorGUILayout.TextField("de-DE", currentVerb.description.de_DE, GUILayout.Height(40));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload"))
            {
                LoadJson(path);
            }
            GUI.enabled = currentVerb != null;
            if (GUILayout.Button("Save"))
            {
                SaveJson(path);
            }
            GUI.enabled = true;
            if (GUILayout.Button("Show raw JSON"))
            {
                editableRawJson = lastLoadedJson ?? "";
                showRawJson = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRawJsonEditor(string path)
        {
            EditorGUILayout.LabelField("Raw JSON Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            rawJsonScroll = EditorGUILayout.BeginScrollView(rawJsonScroll, GUILayout.Height(300));
            editableRawJson = EditorGUILayout.TextArea(editableRawJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save JSON"))
            {
                try
                {
                    File.WriteAllText(path, editableRawJson, Encoding.UTF8);
                    AssetDatabase.Refresh();
                    lastLoadedJson = editableRawJson;
                    showRawJson = false;
                    LoadJson(path); // reload to update structured editor
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to save raw JSON: " + ex.Message);
                }
            }
            if (GUILayout.Button("Close Raw JSON"))
            {
                showRawJson = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SaveJson(string path)
        {
            try
            {
                string json = JsonUtility.ToJson(currentVerb, true)
                    .Replace("en_US", "en-US")
                    .Replace("de_DE", "de-DE");
                File.WriteAllText(path, json, Encoding.UTF8);
                AssetDatabase.Refresh();
                lastLoadedJson = json;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to save JSON: " + ex.Message);
            }
        }
    }
}