using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace xAPI4Unity.Editor
{
    public class XapiFolderTreeWindow : EditorWindow
    {
        private static Settings Settings => Settings.Instance;
        private Vector2 _scroll;
        private Vector2 _scrollRight;
        private Vector2 _previewScroll;
        private readonly Dictionary<string, bool> _folderStates = new Dictionary<string, bool>();

        private string _selectedJsonPath = null;
        private JObject _jsonObj = null; // JObject for dynamic JSON editing
        private string _lastLoadedJson = null;
        private bool _showRawJson = false;
        private string _editableRawJson = "";
        private Vector2 _rawJsonScroll;

        [MenuItem("xAPI4Unity/Edit xAPI definitions (experimental)")]
        public static void ShowWindow()
        {
            GetWindow<XapiFolderTreeWindow>("Assets/xapi definitions");
        }

        private void OnGUI()
        {
            var rootFolder = Settings.localSource.path;
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
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Width(position.width / 2f));
            DrawDirectory(rootFolder, 0);
            EditorGUILayout.EndScrollView();

            // RIGHT: Details/Edit panel
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            _scrollRight = EditorGUILayout.BeginScrollView(_scrollRight);

            if (_selectedJsonPath != null)
            {
                if (_showRawJson)
                    DrawRawJsonEditor(_selectedJsonPath);
                else
                    DrawJsonEditor(_selectedJsonPath);
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

            if (!_folderStates.ContainsKey(path))
                _folderStates[path] = depth < 2;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 16f);
            _folderStates[path] = EditorGUILayout.Foldout(_folderStates[path], folderName, true);
            EditorGUILayout.EndHorizontal();

            if (!_folderStates[path])
                return;

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                if (!file.EndsWith(".json")) continue;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * 16f + 5f);

                GUIStyle style = (_selectedJsonPath == file) ? EditorStyles.boldLabel : EditorStyles.label;

                if (GUILayout.Button(Path.GetFileName(file), style))
                {
                    _selectedJsonPath = file;
                    LoadJson(file);
                    _showRawJson = false;
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
                _lastLoadedJson = File.ReadAllText(path, Encoding.UTF8);
                _jsonObj = JObject.Parse(_lastLoadedJson);
                _editableRawJson = _lastLoadedJson;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to load JSON: " + ex.Message);
                _jsonObj = new JObject();
                _editableRawJson = "";
            }
        }

        private void SaveJson(string path)
        {
            try
            {
                string json = JsonConvert.SerializeObject(_jsonObj, Formatting.Indented);
                File.WriteAllText(path, json, Encoding.UTF8);
                AssetDatabase.Refresh();
                _lastLoadedJson = json;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to save JSON: " + ex.Message);
            }
        }

        private void DrawJsonEditor(string path)
        {
            EditorGUILayout.LabelField("Editing:", Path.GetFileName(path), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_jsonObj == null)
                LoadJson(path);

            DrawLocalization("name");
            DrawLocalization("description");

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload"))
            {
                LoadJson(path);
            }
            if (GUILayout.Button("Save"))
            {
                SaveJson(path);
            }
            if (GUILayout.Button("Edit raw JSON"))
            {
                _editableRawJson = _lastLoadedJson ?? "";
                _showRawJson = true;
            }
            EditorGUILayout.EndHorizontal();

            // --- FULL FILE PREVIEW CANVAS ---
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Full File Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var pretty = JsonConvert.SerializeObject(_jsonObj, Formatting.Indented);
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(300));
            EditorGUILayout.TextArea(pretty ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawLocalization(string field)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(field.Substring(0, 1).ToUpper() + field.Substring(1), EditorStyles.boldLabel);

            JObject locObj = _jsonObj[field] as JObject;
            if (locObj == null)
            {
                locObj = new JObject();
                _jsonObj[field] = locObj;
            }

            List<string> keysToRemove = new List<string>();
            string keyToRenameFrom = null;
            string keyToRenameTo = null;
            string valueToSet = null;

            // Take snapshot of keys for safe iteration
            var keysSnapshot = new List<string>();
            foreach (var prop in locObj.Properties())
                keysSnapshot.Add(prop.Name);

            foreach (var oldKey in keysSnapshot)
            {
                EditorGUILayout.BeginHorizontal();
                string oldVal = locObj[oldKey]?.ToString() ?? "";

                string newKey = EditorGUILayout.TextField(oldKey, GUILayout.Width(80));
                string newVal = EditorGUILayout.TextField(oldVal);

                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    keysToRemove.Add(oldKey);
                }
                else if (newKey != oldKey && !locObj.ContainsKey(newKey))
                {
                    keyToRenameFrom = oldKey;
                    keyToRenameTo = newKey;
                    valueToSet = newVal;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                else if (newVal != oldVal)
                {
                    locObj[oldKey] = newVal;
                }
                EditorGUILayout.EndHorizontal();
            }
            foreach (var key in keysToRemove)
                locObj.Remove(key);

            if (!string.IsNullOrEmpty(keyToRenameFrom) && !string.IsNullOrEmpty(keyToRenameTo))
            {
                locObj.Remove(keyToRenameFrom);
                locObj[keyToRenameTo] = valueToSet;
            }

            // Add new language
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            if (GUILayout.Button("+ Add language", GUILayout.Width(120)))
            {
                string newKey = "xx-XX";
                int counter = 1;
                while (locObj.ContainsKey(newKey))
                    newKey = $"xx-XX{counter++}";
                locObj[newKey] = "";
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRawJsonEditor(string path)
        {
            EditorGUILayout.LabelField("Raw JSON Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _rawJsonScroll = EditorGUILayout.BeginScrollView(_rawJsonScroll, GUILayout.Height(300));
            _editableRawJson = EditorGUILayout.TextArea(_editableRawJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save JSON"))
            {
                try
                {
                    File.WriteAllText(path, _editableRawJson, Encoding.UTF8);
                    AssetDatabase.Refresh();
                    _lastLoadedJson = _editableRawJson;
                    _showRawJson = false;
                    LoadJson(path); // reload to update structured editor
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Failed to save raw JSON: " + ex.Message);
                }
            }
            if (GUILayout.Button("Close Raw JSON"))
            {
                _showRawJson = false;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}