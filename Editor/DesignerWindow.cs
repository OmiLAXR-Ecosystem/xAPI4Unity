using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    public class DesignerWindow: EditorWindow
    {
        private enum Group
        {
            Verbs,
            Activities,
            Extensions
        }

        private enum ExtensionType
        {
            Activity,
            Context,
            Result
        }
        
        private static Settings Settings => Settings.instance;
        private readonly LocalSourceView _localSourceView = new LocalSourceView(Settings.instance);

        private string _selectedContext = "";
        private Group _selectedGroup = Group.Verbs;
        private ExtensionType _selectedExtensionType = ExtensionType.Activity;


        private int _selectedDefinitionIndex = -1;
        private Vector2 _selectedDefinitionScrollPosition;

        private string[] _verbs = Array.Empty<string>();
        private string[] _activities = Array.Empty<string>();
        private string[] _extensions = Array.Empty<string>();

        private Vector2 _definitionScrollPosition;

        private bool _isEditMode = false;
        private Dictionary<string, Definition> _definition;
        private string _selectedDefinition = "";
        
        public event OnTriggeredControlHandler OnTriggeredControl;

        
        [MenuItem("EduXR / xAPI / Designer")]
        public static void ShowWindow()
        {
            GetWindow<DesignerWindow>("xAPI Designer").Show();
        }

        public void OnGUI()
        {
            CustomGUI.Title("xAPI Definitions Designer");
            
            CustomGUI.Horizontal.Begin();

            CustomGUI.BeginDisabledGroup(!Settings.IsDirty());
            
            if (CustomGUI.Button("Save settings", OnTriggeredControl))
            {
                GUI.FocusControl(null);
                Settings.instance.Save();
            }
            
            CustomGUI.EndDisabledGroup();

            CustomGUI.Space();

            if (CustomGUI.Button("Configure transformation", OnTriggeredControl))
            {
                FetcherWindow.ShowWindow();
            }
            
            CustomGUI.Horizontal.End();

            CustomGUI.Space();
            
            _localSourceView.Show();
            
            CustomGUI.Divider();
            CustomGUI.Space();

            var contexts = GetContexts();
            
            CustomGUI.Horizontal.Begin();
            _selectedContext = CustomGUI.Popup("Context", contexts, _selectedContext, OnChangeValue);
            _selectedGroup = CustomGUI.Popup("", _selectedGroup, OnChangeValue);

            
            if (_selectedGroup == Group.Extensions)
            {
                CustomGUI.Label("for");
                _selectedExtensionType = CustomGUI.Popup("",  _selectedExtensionType, OnChangeValue);
            }
            
            CustomGUI.Horizontal.End();
            
            switch (_selectedGroup)
            {
                case Group.Activities:
                    ShowActivities();
                    break;
                case Group.Extensions:
                    ShowExtensions();
                    break;
                default:
                case Group.Verbs:
                    ShowVerbs();
                    break;
            }
            
        }
        
        private void OnChangeValue(Type type, string label, string value)
        {
            OnTriggeredControl?.Invoke(type, label, value);
            ReloadDefinitions();
        }

        private void ShowDefinition()
        {
            CustomGUI.BeginDisabledGroup(!_isEditMode);
            CustomGUI.BeginVertical();

            var n = new Dictionary<Group, string>()
            {
                [Group.Activities] = "Activity",
                [Group.Extensions] = "Extension",
                [Group.Verbs] = "Verb"
            };
            
            CustomGUI.TextField(n[_selectedGroup] + " name: ", _selectedDefinition);

            _definitionScrollPosition = EditorGUILayout.BeginScrollView(_definitionScrollPosition, new []{ GUILayout.ExpandWidth(true), GUILayout.MinWidth(800) });

            if (_definition != null)
            {
                foreach (var (key, value) in _definition)
                {
                    CustomGUI.TextField("Language", key, "en-US", OnChangeValue);
                    CustomGUI.TextField("Name", value.name, default, OnChangeValue);
                    CustomGUI.Label("Description");
                    EditorGUILayout.TextArea(value.description, new[] { GUILayout.ExpandWidth(true), GUILayout.Height(150) });
                }
            }
            else
            {
                CustomGUI.Warning("Select a definition on the left to edit.");
            }

            EditorGUILayout.EndScrollView();

            CustomGUI.EndVertical();
            CustomGUI.EndDisabledGroup();
            
        }

        private void ShowDefinitions(string[] definitions)
        {
            CustomGUI.Horizontal.Begin();
            _selectedDefinitionIndex = CustomGUI.Listbox("Definitions", definitions, ref _selectedDefinitionScrollPosition, _selectedDefinitionIndex,  200, 400, false, true, LoadDefinitionContent);
            ShowDefinition();
            CustomGUI.Horizontal.End();
        }

        private void LoadDefinitionContent(string value)
        {
            _selectedDefinition = value;
            var groupFolder = _selectedGroup.ToString().ToLower();

            if (_selectedGroup == Group.Extensions)
                groupFolder = Path.Combine(groupFolder, _selectedExtensionType.ToString().ToLower());

            var fullFilePath = Path.Combine(Settings.localSource.path, "definitions", _selectedContext, groupFolder,
                value + ".json");

            var definitionFile =
                Newtonsoft.Json.JsonConvert.DeserializeObject<DefinitionFile>(File.ReadAllText(fullFilePath));
            Debug.Log(definitionFile);
            _definition = definitionFile.ToDefinitionList();
        }

        private void ReloadDefinitions()
        {
            _selectedDefinitionIndex = -1;
            _selectedDefinition = "";
            _definition = null;
            _verbs = LoadVerbs(_selectedContext);
            _activities = LoadActivities(_selectedContext);
            _extensions = LoadExtensions(_selectedContext, _selectedExtensionType);
        }
        
        private void ShowVerbs()
            => ShowDefinitions(_verbs);

        private void ShowActivities()
            => ShowDefinitions(_activities);

        private void ShowExtensions()
            => ShowDefinitions(_extensions);
        
        private static IEnumerable<string> GetContexts()
        {
            var dirs = Directory.GetDirectories(Path.Combine(Settings.localSource.path, "definitions"));
            return dirs.Select(dir => Path.GetFileName(dir)!).ToArray();
        }
        
        private static string[] LoadDefinitions(string context, string folder)
        {
            var path = Path.Combine(Settings.localSource.path, "definitions", context, folder);
            if (!Directory.Exists(path))
                return Array.Empty<string>();
            var files = Directory.GetFiles(path);
            return files.Where(f => Path.GetExtension(f) != ".meta").Select(f => Path.GetFileNameWithoutExtension(f)!).ToArray();
        }

        private string[] LoadVerbs(string context)
            => LoadDefinitions(context, Group.Verbs.ToString().ToLower());
        
        private string[] LoadActivities(string context)
            => LoadDefinitions(context, Group.Activities.ToString().ToLower());

        private string[] LoadExtensions(string context, ExtensionType extensionType)
            => LoadDefinitions(context,
                Path.Combine(Group.Extensions.ToString().ToLower(), extensionType.ToString().ToLower()));
    }
}