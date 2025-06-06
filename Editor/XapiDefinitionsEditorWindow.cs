using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace xAPI4Unity.Editor
{
    public class XapiDefinitionsEditorWindow : EditorWindow
    {
        private static Settings Settings => Settings.Instance;
        private Vector2 _scroll;
        private Vector2 _scrollRight;
        private Vector2 _previewScroll;

        private string _selectedJsonPath = null;
        private string _selectedDirectory = null;
        private JObject _jsonObj = null;
        private string _lastLoadedJson = null;
        private bool _showRawJson = false;
        private string _editableRawJson = "";
        private Vector2 _rawJsonScroll;

        private string _newJsonFileName = "newDefinition";
        private bool _showCreateDialog = false;
        private bool _showRenameDialog = false;
        private string _renameJsonFileName = "";
        private string _previousSelection = null;

        private bool _showCreateFolderDialog = false;
        private string _newFolderName = "New Folder";

        private readonly Dictionary<string, string> _unsavedChanges = new Dictionary<string, string>();
        private bool _jsonDirty = false;

        [MenuItem("xAPI4Unity/Edit xAPI definitions", priority = 1)]
        public static void ShowWindow()
        {
            GetWindow<XapiDefinitionsEditorWindow>("xAPI Definitions Editor");
        }

        private void OnGUI()
        {
            UpdateRenameField();

            var rootFolder = Settings.localSource.path;
            GUILayout.Label($"Showing file structure for: {rootFolder}", EditorStyles.boldLabel);

            var currentSelection = _selectedJsonPath ?? _selectedDirectory;
            if (_previousSelection != currentSelection)
            {
                if (_selectedJsonPath != null)
                    _renameJsonFileName = Path.GetFileNameWithoutExtension(_selectedJsonPath);
                else if (_selectedDirectory != null)
                    _renameJsonFileName = Path.GetFileName(_selectedDirectory);
                _previousSelection = currentSelection;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(100))) Repaint();
            if (GUILayout.Button("Create Definition", GUILayout.Width(140)))
            {
                _showCreateDialog = true;
                _showRenameDialog = _showCreateFolderDialog = false;
                _newJsonFileName = "newDefinition";
            }

            if (GUILayout.Button("Create Folder", GUILayout.Width(140)))
            {
                _showCreateFolderDialog = true;
                _showCreateDialog = _showRenameDialog = false;
                _newFolderName = "New Folder";
            }

            if ((_selectedJsonPath != null && File.Exists(_selectedJsonPath)) ||
                !string.IsNullOrEmpty(_selectedDirectory))
            {
                if (GUILayout.Button("Rename", GUILayout.Width(140)))
                {
                    _showRenameDialog = true;
                    _showCreateDialog = _showCreateFolderDialog = false;
                }
            }

            if (_selectedJsonPath != null && File.Exists(_selectedJsonPath))
            {
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
            }

            EditorGUILayout.EndHorizontal();

            if (_showCreateDialog)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("File name:", GUILayout.Width(70));
                _newJsonFileName = EditorGUILayout.TextField(_newJsonFileName, GUILayout.Width(200));
                EditorGUILayout.LabelField(".json", GUILayout.Width(30));
                if (GUILayout.Button("Create", GUILayout.Width(70)))
                {
                    CreateNewDefinitionFile();
                    _showCreateDialog = false;
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(70))) _showCreateDialog = false;
                EditorGUILayout.EndHorizontal();
            }

            if (_showCreateFolderDialog)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Folder name:", GUILayout.Width(90));
                _newFolderName = EditorGUILayout.TextField(_newFolderName, GUILayout.Width(200));
                if (GUILayout.Button("Create", GUILayout.Width(70)))
                {
                    CreateNewFolder();
                    _showCreateFolderDialog = false;
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(70))) _showCreateFolderDialog = false;
                EditorGUILayout.EndHorizontal();
            }

            if (_showRenameDialog)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Rename to:", GUILayout.Width(80));
                _renameJsonFileName = EditorGUILayout.TextField(_renameJsonFileName, GUILayout.Width(200));
                if (_selectedJsonPath != null) EditorGUILayout.LabelField(".json", GUILayout.Width(30));
                if (GUILayout.Button("Rename", GUILayout.Width(70)))
                {
                    if (_selectedJsonPath != null) RenameDefinitionFile();
                    else if (_selectedDirectory != null) RenameDirectory();
                    _showRenameDialog = false;
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(70))) _showRenameDialog = false;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            DrawPathHeader();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Width(position.width / 2f));
            DrawDirectory(rootFolder, 0);
            EditorGUILayout.EndScrollView();

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

        private void CreateNewFolder()
        {
            var folderName = _newFolderName.Trim();
            if (string.IsNullOrEmpty(folderName) || folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("Invalid Folder Name", "Please enter a valid folder name.", "OK");
                return;
            }

            string parentFolder;
            if (!string.IsNullOrEmpty(_selectedDirectory))
                parentFolder = _selectedDirectory;
            else if (!string.IsNullOrEmpty(_selectedJsonPath))
                parentFolder = Path.GetDirectoryName(_selectedJsonPath);
            else
                parentFolder = Settings.localSource.path;

            var newFolderPath = Path.Combine(parentFolder, folderName);
            if (Directory.Exists(newFolderPath))
            {
                EditorUtility.DisplayDialog("Folder Exists", "A folder with this name already exists.", "OK");
                return;
            }

            try
            {
                Directory.CreateDirectory(newFolderPath);
                SessionState.SetBool($"XapiDefinitionsEditorWindow.Foldout.{newFolderPath.GetHashCode()}", true);
                AssetDatabase.Refresh();
                _selectedDirectory = newFolderPath;
                _selectedJsonPath = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to create folder: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Could not create folder:\n" + ex.Message, "OK");
            }
        }

        private void RenameDefinitionFile()
        {
            if (string.IsNullOrEmpty(_selectedJsonPath) || !File.Exists(_selectedJsonPath))
                return;

            var folder = Path.GetDirectoryName(_selectedJsonPath);

            var fileNameWithoutExtension = ToCamelCase(Path.GetFileNameWithoutExtension(_renameJsonFileName.Trim()));
            var newFileName = fileNameWithoutExtension + ".json";

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
                if (!string.IsNullOrEmpty(newPath))
                {
                    LoadJson(newPath);
                    _showRawJson = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to rename definition: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Could not rename file:\n" + ex.Message, "OK");
            }
        }

        private void DrawPathHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(0, 0, 0, 10)
            };

            var pathStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true
            };

            EditorGUILayout.BeginVertical(headerStyle);

            string displayPath = null;
            string label = null;

            if (!string.IsNullOrEmpty(_selectedJsonPath))
            {
                label = "Selected File:";
                displayPath = _selectedJsonPath;
            }
            else if (!string.IsNullOrEmpty(_selectedDirectory))
            {
                label = "Selected Directory:";
                displayPath = _selectedDirectory;
            }

            if (displayPath != null)
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(displayPath, pathStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDirectory(string path, int depth)
        {
            var folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(folderName))
                folderName = path;

            var sessionKey = $"XapiDefinitionsEditorWindow.Foldout.{path.GetHashCode()}";
            var isOpen = SessionState.GetBool(sessionKey, depth < 2);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 16f);

            var style = (_selectedDirectory == path) ? EditorStyles.boldLabel : EditorStyles.label;
            var rect = EditorGUILayout.GetControlRect();
            var foldoutRect = new Rect(rect.x, rect.y, 15, rect.height);
            var labelRect = new Rect(rect.x + 15, rect.y, rect.width - 15, rect.height);

            var newIsOpen = EditorGUI.Foldout(foldoutRect, isOpen, "", true);
            if (GUI.Button(labelRect, folderName, style))
            {
                GUI.FocusControl(null);
                _selectedDirectory = path;
                _selectedJsonPath = null;
                _renameJsonFileName = folderName;
            }

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

                var fileStyle = (_selectedJsonPath == file) ? EditorStyles.boldLabel : EditorStyles.label;
                var displayName = Path.GetFileName(file);
                if (_unsavedChanges.ContainsKey(file))
                    displayName += "*";

                if (GUILayout.Button(displayName, fileStyle))
                {
                    GUI.FocusControl(null);
                    _selectedJsonPath = file;
                    _selectedDirectory = null;
                    _renameJsonFileName = Path.GetFileNameWithoutExtension(file);
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

        private void RenameDirectory()
        {
            if (string.IsNullOrEmpty(_selectedDirectory) || !Directory.Exists(_selectedDirectory))
                return;

            var parent = Path.GetDirectoryName(_selectedDirectory);
            var newFolderName = ToCamelCase(_renameJsonFileName.Trim());

            if (string.IsNullOrEmpty(newFolderName) || newFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("Invalid Folder Name", "Please enter a valid folder name.", "OK");
                return;
            }

            var newPath = Path.Combine(parent, newFolderName);
            if (Directory.Exists(newPath))
            {
                EditorUtility.DisplayDialog("Folder Exists", "A folder with this name already exists.", "OK");
                return;
            }

            try
            {
                Directory.Move(_selectedDirectory, newPath);
                AssetDatabase.Refresh();
                _selectedDirectory = newPath;
                _selectedJsonPath = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to rename folder: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Could not rename folder:\n" + ex.Message, "OK");
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
            var fileNameWithoutExtension = ToCamelCase(Path.GetFileNameWithoutExtension(_newJsonFileName.Trim()));
            var fileName = fileNameWithoutExtension + ".json";
            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("Invalid File Name", "Please enter a valid JSON file name.", "OK");
                return;
            }

            // Bestimme den Zielordner in dieser Reihenfolge:
            // 1. Ausgewähltes Verzeichnis
            // 2. Verzeichnis der ausgewählten Datei
            // 3. Root-Verzeichnis
            string folder;
            if (!string.IsNullOrEmpty(_selectedDirectory))
                folder = _selectedDirectory;
            else if (!string.IsNullOrEmpty(_selectedJsonPath))
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
                ["name"] = new JObject { ["en-US"] = ToLowercaseWords(Path.GetFileNameWithoutExtension(fileName)) },
                ["description"] = new JObject { ["en-US"] = "Put a description here..." }
            };
            var json = JsonConvert.SerializeObject(newObj, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            AssetDatabase.Refresh();
            _selectedJsonPath = filePath;
            _selectedDirectory = null; // Setze die Verzeichnisauswahl zurück
            LoadJson(filePath);
            _showRawJson = false;
        }

        public static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Split by spaces, underscores, or hyphens
            var words = Regex.Split(input, @"[\s_\-]+");

            for (var i = 0; i < words.Length; i++)
            {
                if (string.IsNullOrEmpty(words[i])) continue;

                if (i == 0)
                    words[i] = words[i].Substring(0, 1).ToLowerInvariant() + words[i].Substring(1);
                else
                    words[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(words[i].ToLowerInvariant());
            }

            return string.Concat(words);
        }

        private void UpdateRenameField()
        {
            var current = _selectedJsonPath ?? _selectedDirectory;
            if (_previousSelection == current) return;
            if (!_showRenameDialog) // Only update if not actively editing
            {
                _renameJsonFileName = _selectedJsonPath != null
                    ? Path.GetFileNameWithoutExtension(_selectedJsonPath)
                    : Path.GetFileName(_selectedDirectory);
            }

            _previousSelection = current;
        }

        public static string ToLowercaseWords(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Insert a space before each capital letter (except the first character)
            var split = Regex.Replace(input, "(\\B[A-Z])", " $1");

            // Lowercase the entire result
            return split.ToLowerInvariant();
        }

        private void LoadJson(string path)
        {
            try
            {
                if (_unsavedChanges.TryGetValue(path, out var cachedJson))
                {
                    // Only update editable and parsed JSON
                    _editableRawJson = cachedJson;
                    _jsonObj = JObject.Parse(cachedJson);

                    // ⚠ Don't update _lastLoadedJson! This file was NOT saved yet
                }
                else
                {
                    _lastLoadedJson = File.ReadAllText(path, Encoding.UTF8);
                    _editableRawJson = _lastLoadedJson;
                    _jsonObj = JObject.Parse(_lastLoadedJson);
                }

                _jsonDirty = _unsavedChanges.ContainsKey(path); // Ensure consistency
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
                FetcherWindow.QuickFetch();
                AssetDatabase.Refresh();
                _lastLoadedJson = json;
                _unsavedChanges.Remove(path);
                _jsonDirty = false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to save JSON: " + ex.Message);
            }
        }

        private void DrawJsonEditor(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            EditorGUILayout.LabelField("Editing:", Path.GetFileName(path), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_jsonObj == null)
                LoadJson(path);

            DrawLocalization("name");
            DrawLocalization("description");

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            /*if (GUILayout.Button("Reload"))
            {
                LoadJson(path);
            }*/

            if (GUILayout.Button("Save"))
            {
                SaveJson(path);
            }

            /*if (GUILayout.Button("Edit raw JSON"))
            {
                _editableRawJson = _lastLoadedJson ?? "";
                _showRawJson = true;
            }*/

            EditorGUILayout.EndHorizontal();

            if (_jsonDirty)
            {
                var currentJson = JsonConvert.SerializeObject(_jsonObj, Formatting.Indented);
                _unsavedChanges[_selectedJsonPath] = currentJson;
            }
            else
            {
                _unsavedChanges.Remove(_selectedJsonPath);
            }

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
                    _jsonDirty = true;
                }

                if (newKey != oldKey && !locObj.ContainsKey(newKey))
                {
                    keyToRenameFrom = oldKey;
                    keyToRenameTo = newKey;
                    valueToSet = newVal;
                    _jsonDirty = true;
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                // ✅ Always assign newVal to keep GUI in sync with JObject
                if (newVal != oldVal)
                    _jsonDirty = true;

                locObj[oldKey] = newVal;

                EditorGUILayout.EndHorizontal();
            }

            foreach (var key in keysToRemove)
                locObj.Remove(key);

            if (!string.IsNullOrEmpty(keyToRenameFrom) && !string.IsNullOrEmpty(keyToRenameTo))
            {
                locObj.Remove(keyToRenameFrom);
                locObj[keyToRenameTo] = valueToSet;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            if (GUILayout.Button("+ Add language", GUILayout.Width(120)))
            {
                var newKey = "xx-XX";
                var counter = 1;
                while (locObj.ContainsKey(newKey))
                    newKey = $"xx-XX{counter++}";
                locObj[newKey] = "";
                _jsonDirty = true;
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