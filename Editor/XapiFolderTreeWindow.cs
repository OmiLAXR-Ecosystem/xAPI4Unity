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

        private string _selectedJsonPath = null;
        private JObject _jsonObj = null; // JObject for dynamic JSON editing
        private string _lastLoadedJson = null;
        private bool _showRawJson = false;
        private string _editableRawJson = "";
        private Vector2 _rawJsonScroll;

        // --- Added for Create Definition ---
        private string _newJsonFileName = "newDefinition.json";
        private bool _showCreateDialog = false;
        private bool _showRenameDialog = false;
        private string _renameJsonFileName = "";

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

            // --- Buttons Row ---
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                Repaint();

            if (GUILayout.Button("Create Definition", GUILayout.Width(140)))
            {
                _showCreateDialog = true;
                _showRenameDialog = false; // Only one at a time!
                _newJsonFileName = "newDefinition.json";
            }

            GUI.enabled = _selectedJsonPath != null && File.Exists(_selectedJsonPath);
            if (GUILayout.Button("Rename Definition", GUILayout.Width(140)))
            {
                _showRenameDialog = true;
                _showCreateDialog = false; // Only one at a time!
                _renameJsonFileName = Path.GetFileName(_selectedJsonPath);
            }
            
            GUI.enabled = _selectedJsonPath != null && File.Exists(_selectedJsonPath); // Only enable if a JSON is selected
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete Definition", GUILayout.Width(140)))
            {
                if (EditorUtility.DisplayDialog("Delete Definition",
                        $"Are you sure you want to permanently delete '{Path.GetFileName(_selectedJsonPath)}'?",
                        "Delete", "Cancel"))
                {
                    DeleteDefinition(_selectedJsonPath);
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // --- Create Dialog ---
            if (_showCreateDialog)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("File name:", GUILayout.Width(70));
                _newJsonFileName = EditorGUILayout.TextField(_newJsonFileName, GUILayout.Width(200));
                if (GUILayout.Button("Create", GUILayout.Width(70)))
                {
                    CreateNewDefinitionFile();
                    _showCreateDialog = false;
                }
                if (GUILayout.Button("Cancel", GUILayout.Width(70)))
                {
                    _showCreateDialog = false;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (_showRenameDialog)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Rename to:", GUILayout.Width(80));
                _renameJsonFileName = EditorGUILayout.TextField(_renameJsonFileName, GUILayout.Width(200));
                if (GUILayout.Button("Rename", GUILayout.Width(70)))
                {
                    RenameDefinitionFile();
                    _showRenameDialog = false;
                }
                if (GUILayout.Button("Cancel", GUILayout.Width(70)))
                {
                    _showRenameDialog = false;
                }
                EditorGUILayout.EndHorizontal();
            }

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
        
        private void RenameDefinitionFile()
        {
            if (string.IsNullOrEmpty(_selectedJsonPath) || !File.Exists(_selectedJsonPath))
                return;

            var folder = Path.GetDirectoryName(_selectedJsonPath);
            var newFileName = _renameJsonFileName.Trim();

            if (!newFileName.EndsWith(".json"))
                newFileName += ".json";

            if (string.IsNullOrEmpty(newFileName) || newFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("Invalid File Name", "Please enter a valid JSON file name.", "OK");
                return;
            }

            var newPath = Path.Combine(folder, newFileName);

            if (File.Exists(newPath))
            {
                EditorUtility.DisplayDialog("File Exists", "A file with this name already exists.", "OK");
                return;
            }

            try
            {
                File.Move(_selectedJsonPath, newPath);

                // Also move the .meta file if it exists
                var oldMeta = _selectedJsonPath + ".meta";
                var newMeta = newPath + ".meta";
                if (File.Exists(oldMeta))
                    File.Move(oldMeta, newMeta);

                AssetDatabase.Refresh();
                _selectedJsonPath = newPath;
                LoadJson(newPath);
                _showRawJson = false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to rename definition: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Could not rename file:\n" + ex.Message, "OK");
            }
        }

        private void DrawDirectory(string path, int depth)
        {
            var folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(folderName))
                folderName = path;

            // Unique key for each folder
            var sessionKey = $"XapiFolderTreeWindow.Foldout.{path.GetHashCode()}";
            var isOpen = SessionState.GetBool(sessionKey, depth < 2); // open first 2 levels by default

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 16f);
            var newIsOpen = EditorGUILayout.Foldout(isOpen, folderName, true);
            EditorGUILayout.EndHorizontal();

            if (newIsOpen != isOpen)
                SessionState.SetBool(sessionKey, newIsOpen);

            if (!newIsOpen)
                return;

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                if (!file.EndsWith(".json")) continue;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * 16f + 5f);

                var style = (_selectedJsonPath == file) ? EditorStyles.boldLabel : EditorStyles.label;

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
        
        private void DeleteDefinition(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    var metaPath = path + ".meta";
                    if (File.Exists(metaPath))
                        File.Delete(metaPath);
                    AssetDatabase.Refresh();
                }
                _selectedJsonPath = null;
                _jsonObj = null;
                _lastLoadedJson = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to delete definition: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Could not delete file:\n" + ex.Message, "OK");
            }
        }

        private void CreateNewDefinitionFile()
        {
            var fileName = _newJsonFileName.Trim();
            if (!fileName.EndsWith(".json"))
                fileName += ".json";
            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("Invalid File Name", "Please enter a valid JSON file name.", "OK");
                return;
            }

            // Use parent of selected file or root
            string folder;
            if (!string.IsNullOrEmpty(_selectedJsonPath))
                folder = Path.GetDirectoryName(_selectedJsonPath);
            else
                folder = Settings.localSource.path;

            var filePath = Path.Combine(folder, fileName);
            if (File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("File Exists", "A file with this name already exists.", "OK");
                return;
            }

            var newObj = new JObject
            {
                ["name"] = new JObject { ["en-US"] = "" },
                ["description"] = new JObject { ["en-US"] = "" }
            };
            var json = JsonConvert.SerializeObject(newObj, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            AssetDatabase.Refresh();
            _selectedJsonPath = filePath;
            LoadJson(filePath);
            _showRawJson = false;
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
                var json = JsonConvert.SerializeObject(_jsonObj, Formatting.Indented);
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

            var locObj = _jsonObj[field] as JObject;
            if (locObj == null)
            {
                locObj = new JObject();
                _jsonObj[field] = locObj;
            }

            var keysToRemove = new List<string>();
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
                var oldVal = locObj[oldKey]?.ToString() ?? "";

                var newKey = EditorGUILayout.TextField(oldKey, GUILayout.Width(80));
                var newVal = EditorGUILayout.TextField(oldVal);

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
                var newKey = "xx-XX";
                var counter = 1;
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