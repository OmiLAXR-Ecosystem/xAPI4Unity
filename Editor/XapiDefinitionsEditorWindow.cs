/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using xAPI4Unity.Editor.Settings;
using xAPI4Unity.Editor.Wizard;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Editor for the xAPI definition JSON files: a searchable tree on the left and the selected definition on the right.
    /// It uses the look of the xAPI4Unity wizard.
    /// </summary>
    public class XapiDefinitionsEditorWindow : EditorWindow
    {
        private const string PackageRoot = "Packages/com.rwth.unity.omilaxr.xapi4unity/Editor/";
        private const string WizardStyleSheetPath = PackageRoot + "Wizard/XapiWizardWindow.uss";
        private const string StyleSheetPath = PackageRoot + "XapiDefinitionsEditorWindow.uss";
        private const string ExpandedKey = "xAPI4Unity.DefinitionsEditor.Expanded.";
        private const string RawJsonOpenKey = "xAPI4Unity.DefinitionsEditor.RawJsonOpen";

        // Keys of the previous IMGUI editor, read once to keep its unsaved changes
        private const string LegacyUnsavedKey = "XapiEditor.UnsavedChanges";
        private const string LegacySelectedKey = "XapiEditor.LastSelectedJson";
        private const string LegacySelectedDirKey = "XapiEditor.LastSelectedDir";

        // Folder of each kind below 'definitions/<context>/'
        private static readonly (string Label, string Folder)[] Kinds =
        {
            ("Verb", "verbs"),
            ("Activity", "activities"),
            ("Activity extension", "extensions/activity"),
            ("Context extension", "extensions/context"),
            ("Result extension", "extensions/result"),
        };
        private const string NewContextChoice = "New context...";

        private sealed class Node
        {
            public string Name;
            public string Label;
            public string FullPath;
            public string Key;
            public int Depth;
            public bool IsFile;
            public bool Ignored;
            public int FileCount;
            public Node Parent;
            public readonly List<Node> Children = new List<Node>();
        }

        private enum Mode
        {
            View,
            Create,
            Rename
        }

        // Kept across domain reloads by Unity's serialization of the window
        [SerializeField] private string _selectedPath;
        [SerializeField] private List<string> _dirtyPaths = new List<string>();
        [SerializeField] private List<string> _dirtyJson = new List<string>();

        // Unsaved JSON per file and the saved JSON in the same formatting, to tell whether a file changed
        private readonly Dictionary<string, string> _dirty = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _originals = new Dictionary<string, string>();
        private readonly Dictionary<string, VisualElement> _rows = new Dictionary<string, VisualElement>();

        private Node _root;
        private string _treeSignature;
        private string _search = "";
        private Mode _mode;
        private string _renameTarget;
        private string _createDefaultKey;
        private JObject _current;
        private string _currentError;
        private string _currentRaw;
        private string _status;

        private ScrollView _tree;
        private ScrollView _detail;
        private ToolbarButton _renameButton;
        private ToolbarButton _deleteButton;
        private ToolbarButton _revealButton;
        private Button _saveButton;
        private Button _saveAllButton;
        private Button _revertButton;
        private Label _statusLabel;
        private Label _dirtyBadge;
        private VisualElement _checksCard;
        private TextField _rawField;

        private static MainSettings Settings => MainSettings.Instance;

        private static string DefinitionsRoot => string.IsNullOrEmpty(Settings.localSource.path)
            ? null
            : Norm(Path.Combine(Settings.localSource.path, "definitions"));

        /// <summary>
        /// Opens the xAPI Definitions Editor window via the Unity menu.
        /// </summary>
        [MenuItem("xAPI4Unity/Edit xAPI definitions", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<XapiDefinitionsEditorWindow>();
            window.titleContent = new GUIContent("xAPI Definitions");
            window.minSize = new Vector2(720f, 420f);
            window.Show();
        }

        [Shortcut("xAPI4Unity/Definitions Editor/Save", typeof(XapiDefinitionsEditorWindow), KeyCode.S, ShortcutModifiers.Action)]
        private static void SaveShortcut(ShortcutArguments args)
            => (args.context as XapiDefinitionsEditorWindow)?.SaveSelected();

        #region Lifecycle

        private void OnEnable()
        {
            _dirty.Clear();
            for (var i = 0; i < Math.Min(_dirtyPaths.Count, _dirtyJson.Count); i++)
                _dirty[_dirtyPaths[i]] = _dirtyJson[i];
            ImportLegacyState();

            BuildLayout();
            Rescan();
            if (_selectedPath != null && !File.Exists(_selectedPath))
                _selectedPath = null;
            ExpandTo(_selectedPath);
            RenderTree();
            LoadSelected();
            RebuildDetail();
            UpdateDirtyState();

            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            _dirtyPaths = _dirty.Keys.ToList();
            _dirtyJson = _dirty.Values.ToList();
        }

        private void OnFocus() => OnProjectChanged();

#if UNITY_2022_1_OR_NEWER
        public override void SaveChanges()
        {
            SaveAll();
            base.SaveChanges();
        }
#else
        private void OnDestroy()
        {
            if (_dirty.Count > 0 && EditorUtility.DisplayDialog("Unsaved definitions",
                    $"{_dirty.Count} definition(s) have unsaved changes.", "Save", "Discard"))
                SaveAll();
        }
#endif

        /// <summary>
        /// Rescans the definitions when files were added, moved or removed, and reloads the selection if it changed on disk.
        /// </summary>
        private void OnProjectChanged()
        {
            if (_tree == null)
                return;

            var signature = _treeSignature;
            Rescan();
            if (signature != _treeSignature)
            {
                if (_selectedPath != null && !File.Exists(_selectedPath))
                {
                    _selectedPath = null;
                    _mode = Mode.View;
                }
                RenderTree();
                LoadSelected();
                RebuildDetail();
            }
            else if (_selectedPath != null && !_dirty.ContainsKey(_selectedPath) && _mode == Mode.View && ChangedOnDisk(_selectedPath))
            {
                LoadSelected();
                RebuildDetail();
            }
            UpdateDirtyState();
        }

        /// <summary>
        /// Takes over the unsaved changes and the selection of the previous editor, which kept them in EditorPrefs.
        /// </summary>
        private void ImportLegacyState()
        {
            if (EditorPrefs.HasKey(LegacyUnsavedKey))
            {
                try
                {
                    var legacy = JsonConvert.DeserializeObject<Dictionary<string, string>>(EditorPrefs.GetString(LegacyUnsavedKey));
                    foreach (var pair in legacy ?? new Dictionary<string, string>())
                    {
                        if (File.Exists(pair.Key) && !_dirty.ContainsKey(Norm(pair.Key)))
                            _dirty[Norm(pair.Key)] = DefinitionDocument.Serialize(DefinitionDocument.Parse(pair.Value));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[xAPI4Unity] Could not restore unsaved changes of the previous definitions editor: {ex.Message}");
                }
                EditorPrefs.DeleteKey(LegacyUnsavedKey);
            }

            if (EditorPrefs.HasKey(LegacySelectedKey))
            {
                var selected = EditorPrefs.GetString(LegacySelectedKey);
                if (_selectedPath == null && File.Exists(selected))
                    _selectedPath = Norm(selected);
                EditorPrefs.DeleteKey(LegacySelectedKey);
            }
            EditorPrefs.DeleteKey(LegacySelectedDirKey);
        }

        #endregion

        #region Layout

        // Built in OnEnable instead of CreateGUI, which older Unity versions do not call
        private void BuildLayout()
        {
            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList("wizard");
            root.AddToClassList("definitions-editor");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");
            foreach (var path in new[] { WizardStyleSheetPath, StyleSheetPath })
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet)
                    root.styleSheets.Add(styleSheet);
            }

            root.Add(CreateToolbar());

            var tree = Css(new VisualElement(), "tree-pane");
            var search = Css(new ToolbarSearchField(), "tree-search");
            search.RegisterValueChangedCallback(e =>
            {
                _search = e.newValue ?? "";
                RenderTree();
            });
            tree.Add(search);
            _tree = Css(new ScrollView(), "tree");
            tree.Add(_tree);

            var detail = Css(new VisualElement(), "detail-pane");
            _detail = new ScrollView();
            _detail.style.flexGrow = 1;
            detail.Add(_detail);
            detail.Add(CreateFooter());

#if UNITY_2022_1_OR_NEWER
            var body = new TwoPaneSplitView(0, 300f, TwoPaneSplitViewOrientation.Horizontal);
#else
            var body = Css(new VisualElement(), "split-fallback");
#endif
            body.style.flexGrow = 1;
            body.Add(tree);
            body.Add(detail);
            root.Add(body);
        }

        private VisualElement CreateToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() => StartCreate(null)) { text = "New definition" });
            _renameButton = new ToolbarButton(() => StartRename(_selectedPath)) { text = "Rename" };
            toolbar.Add(_renameButton);
            _deleteButton = new ToolbarButton(() => Delete(_selectedPath)) { text = "Delete" };
            toolbar.Add(_deleteButton);
            _revealButton = new ToolbarButton(() => EditorUtility.RevealInFinder(_selectedPath)) { text = "Reveal" };
            toolbar.Add(_revealButton);
            toolbar.Add(Css(new ToolbarSpacer(), "spacer"));
            toolbar.Add(new ToolbarButton(Deferred(UpdateRegistry))
            {
                text = "Update xAPI.Registry",
                tooltip = "Generates the C# registry from all definitions."
            });
            toolbar.Add(new ToolbarButton(XapiWizardWindow.Open) { text = "Wizard" });
            return toolbar;
        }

        private VisualElement CreateFooter()
        {
            var footer = Css(new VisualElement(), "footer");
            _statusLabel = Css(new Label(), "muted", "status");
            footer.Add(_statusLabel);
            footer.Add(Css(new VisualElement(), "spacer"));
            _revertButton = Css(new Button(RevertSelected) { text = "Revert" }, "btn");
            footer.Add(_revertButton);
            _saveAllButton = Css(new Button(SaveAll) { text = "Save all" }, "btn");
            footer.Add(_saveAllButton);
            _saveButton = Css(new Button(SaveSelected) { text = "Save", tooltip = "Ctrl/Cmd+S" }, "btn", "btn--primary");
            footer.Add(_saveButton);
            return footer;
        }

        #endregion

        #region Tree

        private void Rescan()
        {
            var root = DefinitionsRoot;
            if (root == null || !Directory.Exists(root))
            {
                _root = null;
                _treeSignature = null;
                return;
            }

            var ignored = ReadIgnoredContexts();
            var signature = new StringBuilder();
            _root = ScanFolder(root, root, null, 0, ignored, signature);
            _treeSignature = signature.ToString();

            // Unsaved changes of files that are gone cannot be saved anymore
            foreach (var path in _dirty.Keys.Where(p => !File.Exists(p)).ToList())
                _dirty.Remove(path);
        }

        private static Node ScanFolder(string path, string root, Node parent, int depth, ICollection<string> ignored, StringBuilder signature)
        {
            var node = new Node
            {
                Name = Path.GetFileName(path),
                FullPath = path,
                Depth = depth,
                Parent = parent,
                Key = depth == 0 ? "" : DefinitionDocument.GetKey(root, path)
            };
            node.Label = FolderLabel(node);
            node.Ignored = (parent?.Ignored ?? false) || (depth == 1 && ignored.Contains(node.Name));
            signature.Append(node.Key).Append('|');

            foreach (var dir in Directory.GetDirectories(path).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var child = ScanFolder(Norm(dir), root, node, depth + 1, ignored, signature);
                node.Children.Add(child);
                node.FileCount += child.FileCount;
            }

            foreach (var file in Directory.GetFiles(path, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || name.StartsWith("."))
                    continue;
                var fullPath = Norm(file);
                node.Children.Add(new Node
                {
                    Name = name,
                    Label = name,
                    FullPath = fullPath,
                    Key = DefinitionDocument.GetKey(root, fullPath),
                    Depth = depth + 1,
                    Parent = node,
                    IsFile = true,
                    Ignored = node.Ignored
                });
                node.FileCount++;
                signature.Append(name).Append('|');
            }
            return node;
        }

        private static string FolderLabel(Node node)
        {
            if (node.Depth == 2)
            {
                switch (node.Name)
                {
                    case "verbs": return "Verbs";
                    case "activities": return "Activities";
                    case "extensions": return "Extensions";
                }
            }
            if (node.Depth == 3 && node.Parent?.Name == "extensions")
                return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(node.Name);
            return node.Name;
        }

        private static HashSet<string> ReadIgnoredContexts()
        {
            var path = Settings.localSource.path;
            var configPath = string.IsNullOrEmpty(path) ? null : path.TrimEnd('/', '\\') + ".config.json";
            try
            {
                if (configPath != null && File.Exists(configPath))
                    return new HashSet<string>(ConfigFile.Read(configPath)?.ignoredContexts ?? Array.Empty<string>());
            }
            catch (Exception)
            {
                // The wizard reports broken configs, the tree just shows every context
            }
            return new HashSet<string>();
        }

        private void RenderTree()
        {
            if (_tree == null)
                return;
            _tree.Clear();
            _rows.Clear();

            if (_root == null)
            {
                _tree.Add(Css(Muted("No definitions folder."), "tree-empty"));
                return;
            }

            var term = _search.Trim();
            foreach (var child in _root.Children)
                AddRows(child, 0, term);
            if (_tree.childCount == 0)
                _tree.Add(Css(Muted(term.Length == 0 ? "No definitions yet." : $"Nothing matches '{term}'."), "tree-empty"));
        }

        private void AddRows(Node node, int depth, string term)
        {
            var searching = term.Length > 0;
            if (searching && !Matches(node, term))
                return;

            var row = Css(new VisualElement(), "tree-row");
            row.style.paddingLeft = 6 + depth * 14;
            if (node.Ignored)
            {
                row.AddToClassList("tree-row--ignored");
                row.tooltip = "Ignored by the types config, so the generator skips it.";
            }

            if (node.IsFile)
            {
                row.Add(Css(new Label(""), "tree-arrow"));
                row.Add(Css(new Label(node.Label), "tree-label"));
                var dot = Css(new Label("●"), "tree-dirty");
                dot.style.display = _dirty.ContainsKey(node.FullPath) ? DisplayStyle.Flex : DisplayStyle.None;
                row.Add(dot);
                if (node.FullPath == _selectedPath)
                    row.AddToClassList("tree-row--selected");
                row.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0)
                        Select(node.FullPath);
                });
                row.AddManipulator(new ContextualMenuManipulator(e => AddFileMenu(e, node)));
                _rows[node.FullPath] = row;
                _tree.Add(row);
                return;
            }

            var expanded = searching || IsExpanded(node);
            row.Add(Css(new Label(expanded ? "▾" : "▸"), "tree-arrow"));
            row.Add(Css(new Label(node.Label), "tree-label", "tree-folder"));
            row.Add(Css(new Label(node.FileCount.ToString()), "tree-count"));
            row.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0 || searching)
                    return;
                SetExpanded(node, !expanded);
                RenderTree();
            });
            row.AddManipulator(new ContextualMenuManipulator(e => AddFolderMenu(e, node)));
            _tree.Add(row);

            if (expanded)
                foreach (var child in node.Children)
                    AddRows(child, depth + 1, term);
        }

        private static bool Matches(Node node, string term)
            => node.IsFile
                ? node.Key.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                : node.Children.Any(c => Matches(c, term));

        // Contexts start collapsed, the folders inside a context start open
        private static bool IsExpanded(Node node) => SessionState.GetBool(ExpandedKey + node.Key, node.Depth >= 2);

        private static void SetExpanded(Node node, bool expanded) => SessionState.SetBool(ExpandedKey + node.Key, expanded);

        private void ExpandTo(string path)
        {
            for (var node = FindNode(path)?.Parent; node != null && node.Depth > 0; node = node.Parent)
                SetExpanded(node, true);
        }

        private Node FindNode(string path)
        {
            if (_root == null || path == null)
                return null;
            var stack = new Stack<Node>();
            stack.Push(_root);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.FullPath == path)
                    return node;
                foreach (var child in node.Children)
                    stack.Push(child);
            }
            return null;
        }

        private void AddFileMenu(ContextualMenuPopulateEvent e, Node node)
        {
            e.menu.AppendAction("Rename", _ => StartRename(node.FullPath));
            e.menu.AppendAction("Delete", _ => Delete(node.FullPath));
            e.menu.AppendSeparator();
            e.menu.AppendAction("Copy IRI", _ => EditorGUIUtility.systemCopyBuffer = DefinitionDocument.GetIri(node.Key));
            e.menu.AppendAction("Reveal in file browser", _ => EditorUtility.RevealInFinder(node.FullPath));
        }

        private void AddFolderMenu(ContextualMenuPopulateEvent e, Node node)
        {
            e.menu.AppendAction("New definition here", _ => StartCreate(node));
            e.menu.AppendAction("Rename", _ => StartRename(node.FullPath));
            e.menu.AppendAction("Delete", _ => Delete(node.FullPath));
            e.menu.AppendSeparator();
            e.menu.AppendAction("Reveal in file browser", _ => EditorUtility.RevealInFinder(node.FullPath));
        }

        #endregion

        #region Selection and model

        private void Select(string path)
        {
            if (_selectedPath == path && _mode == Mode.View)
                return;
            if (_selectedPath != null && _rows.TryGetValue(_selectedPath, out var previous))
                previous.RemoveFromClassList("tree-row--selected");

            _selectedPath = path;
            _mode = Mode.View;
            _status = null;
            if (path != null && _rows.TryGetValue(path, out var row))
                row.AddToClassList("tree-row--selected");

            LoadSelected();
            RebuildDetail();
            UpdateDirtyState();
        }

        private void LoadSelected()
        {
            _current = null;
            _currentError = null;
            _currentRaw = null;
            if (_selectedPath == null || !File.Exists(_selectedPath))
                return;

            var text = File.ReadAllText(_selectedPath);
            _dirty.TryGetValue(_selectedPath, out var unsaved);
            try
            {
                _originals[_selectedPath] = DefinitionDocument.Serialize(DefinitionDocument.Parse(text));
            }
            catch (Exception)
            {
                // A broken file counts as changed until it is fixed and saved
                _originals.Remove(_selectedPath);
            }

            try
            {
                _current = DefinitionDocument.Parse(unsaved ?? text);
            }
            catch (Exception ex)
            {
                _currentError = ex.Message;
                _currentRaw = unsaved ?? text;
            }
        }

        private bool ChangedOnDisk(string path)
        {
            try
            {
                var onDisk = DefinitionDocument.Serialize(DefinitionDocument.Parse(File.ReadAllText(path)));
                return !_originals.TryGetValue(path, out var original) || original != onDisk;
            }
            catch (Exception)
            {
                return _originals.ContainsKey(path);
            }
        }

        /// <summary>
        /// Updates the unsaved state after an edit. Structural changes, such as an added language, rebuild the page.
        /// </summary>
        private void OnModelChanged(bool structural)
        {
            var json = DefinitionDocument.Serialize(_current);
            if (_originals.TryGetValue(_selectedPath, out var original) && original == json)
                _dirty.Remove(_selectedPath);
            else
                _dirty[_selectedPath] = json;
            _status = null;

            if (structural)
            {
                RebuildDetail();
            }
            else
            {
                FillChecks();
                _rawField?.SetValueWithoutNotify(json);
            }
            UpdateDirtyState();
        }

        private void UpdateDirtyState()
        {
            if (_statusLabel == null)
                return;

            foreach (var pair in _rows)
            {
                var dot = pair.Value.Q<Label>(className: "tree-dirty");
                if (dot != null)
                    dot.style.display = _dirty.ContainsKey(pair.Key) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var selectedDirty = _selectedPath != null && _dirty.ContainsKey(_selectedPath);
            if (_dirtyBadge != null)
                _dirtyBadge.style.display = selectedDirty ? DisplayStyle.Flex : DisplayStyle.None;

            var hasFile = _selectedPath != null && File.Exists(_selectedPath);
            _renameButton.SetEnabled(hasFile);
            _deleteButton.SetEnabled(hasFile);
            _revealButton.SetEnabled(hasFile);
            _saveButton.SetEnabled(selectedDirty && _current != null);
            _revertButton.SetEnabled(selectedDirty || _currentError != null);
            _saveAllButton.SetEnabled(_dirty.Count > 0);
            _saveAllButton.style.display = _dirty.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            _statusLabel.text = _status ?? (_dirty.Count > 0 ? $"{_dirty.Count} unsaved definition(s)" : "All changes saved");

#if UNITY_2022_1_OR_NEWER
            hasUnsavedChanges = _dirty.Count > 0;
            saveChangesMessage = $"{_dirty.Count} xAPI definition(s) have unsaved changes.";
#endif
        }

        #endregion

        #region Detail pages

        private void RebuildDetail()
        {
            if (_detail == null)
                return;
            _detail.Clear();
            _rawField = null;
            _checksCard = null;
            _dirtyBadge = null;

            var page = Css(new VisualElement(), "page");
            switch (_mode)
            {
                case Mode.Create: BuildCreate(page); break;
                case Mode.Rename: BuildRename(page); break;
                default: BuildView(page); break;
            }
            _detail.Add(page);
        }

        private void BuildView(VisualElement page)
        {
            if (_root == null)
            {
                page.Add(H1("No definitions"));
                page.Add(Text($"There is no 'definitions' folder at '{Settings.localSource.path}'. Get the definitions in the wizard."));
                page.Add(Css(new Button(XapiWizardWindow.Open) { text = "Open wizard" }, "btn", "btn--primary", "field"));
                return;
            }

            if (_selectedPath == null)
            {
                var contexts = _root.Children.Count(c => !c.IsFile);
                page.Add(H1("xAPI definitions"));
                page.Add(Text($"{_root.FileCount} definitions in {contexts} contexts at '{ToDisplayPath(DefinitionsRoot)}'. " +
                              "Select one on the left or create a new one."));
                page.Add(Css(new Button(() => StartCreate(null)) { text = "New definition" }, "btn", "btn--primary", "field"));
                return;
            }

            var key = DefinitionDocument.GetKey(DefinitionsRoot, _selectedPath);
            page.Add(H1(Path.GetFileNameWithoutExtension(_selectedPath)));
            page.Add(Badges(key));
            page.Add(IriRow(key));

            if (_current == null)
            {
                BuildInvalid(page);
                return;
            }

            page.Add(H2("Status"));
            _checksCard = Css(new VisualElement(), "card");
            FillChecks();
            page.Add(_checksCard);

            page.Add(H2("Name"));
            page.Add(LanguageCard(DefinitionDocument.NameField, false));
            page.Add(H2("Description"));
            page.Add(LanguageCard(DefinitionDocument.DescriptionField, true));
            page.Add(H2("Equivalent IRIs (exactMatch)"));
            page.Add(ExactMatchCard());

            var others = DefinitionDocument.GetOtherFields(_current);
            if (others.Count > 0)
                page.Add(Css(Muted($"Also contains {string.Join(", ", others.Select(o => $"'{o}'"))}. " +
                                   "These fields are kept as they are. Edit them in the raw JSON."), "field"));

            page.Add(RawJsonFoldout());
        }

        private void BuildInvalid(VisualElement page)
        {
            var card = Css(new VisualElement(), "card", "field");
            card.Add(CheckRow(false, false, "Valid JSON", _currentError));
            page.Add(card);

            page.Add(H2("Raw JSON"));
            var error = Css(new Label(), "error-text");
            var raw = Css(new TextField { value = _currentRaw ?? "", multiline = true }, "raw-json");
            page.Add(raw);
            page.Add(error);
            var actions = Css(new VisualElement(), "card-row", "actions");
            actions.Add(Css(new Button(() => ApplyRaw(raw.value, error)) { text = "Apply JSON" }, "btn", "btn--primary"));
            page.Add(actions);
        }

        private VisualElement Badges(string key)
        {
            var row = Css(new VisualElement(), "badge-row");
            var kind = DefinitionDocument.GetKind(key);
            var extensionType = DefinitionDocument.GetExtensionType(key);
            var kindLabel = kind == DefinitionKind.Extension && extensionType != null ? $"{extensionType} extension" : kind.ToString().ToLowerInvariant();
            row.Add(Css(new Label(kindLabel), "badge", "badge--" + kind.ToString().ToLowerInvariant()));
            row.Add(Css(new Label(DefinitionDocument.GetContext(key)), "badge", "badge--off"));
            if (FindNode(_selectedPath)?.Ignored ?? false)
                row.Add(Css(new Label("ignored by config"), "badge", "badge--off"));
            _dirtyBadge = Css(new Label("unsaved"), "badge", "badge--dirty");
            row.Add(_dirtyBadge);
            return row;
        }

        private static VisualElement IriRow(string key)
        {
            var iri = DefinitionDocument.GetIri(key);
            var row = Css(new VisualElement(), "card-row", "iri-row");
            var field = Css(new TextField { value = iri, isReadOnly = true, tooltip = "IRI of this definition in the xAPI Registry" }, "iri");
            row.Add(field);
            row.Add(Css(new Button(() => EditorGUIUtility.systemCopyBuffer = iri) { text = "Copy" }, "btn"));
            row.Add(Css(new Button(() => Application.OpenURL(iri)) { text = "Open" }, "btn"));
            return row;
        }

        private void FillChecks()
        {
            if (_checksCard == null || _current == null)
                return;
            _checksCard.Clear();

            var names = DefinitionDocument.GetLanguageMap(_current, DefinitionDocument.NameField);
            var descriptions = DefinitionDocument.GetLanguageMap(_current, DefinitionDocument.DescriptionField);
            var hasName = names.Any(p => p.Key == "en-US" && !string.IsNullOrWhiteSpace(p.Value));
            var hasDescription = descriptions.Any(p => p.Key == "en-US" && !string.IsNullOrWhiteSpace(p.Value));

            _checksCard.Add(CheckRow(hasName, false, "English name (en-US)",
                "The registry and the generated code use the English name."));
            _checksCard.Add(CheckRow(hasDescription, true, "English description (en-US)",
                "The generated code shows the English description as documentation comment."));
        }

        private VisualElement LanguageCard(string field, bool multiline)
        {
            var card = Css(new VisualElement(), "card");
            foreach (var pair in DefinitionDocument.GetLanguageMap(_current, field))
            {
                var language = pair.Key;
                var row = Css(new VisualElement(), "lang-row");

                var choices = DefinitionDocument.CommonLanguages.ToList();
                if (!choices.Contains(language))
                    choices.Insert(0, language);
                var languageField = Css(new PopupField<string>(choices, language), "lang-key");
                languageField.RegisterValueChangedCallback(e =>
                {
                    if (DefinitionDocument.RenameLanguage(_current, field, e.previousValue, e.newValue))
                    {
                        OnModelChanged(true);
                        return;
                    }
                    languageField.SetValueWithoutNotify(e.previousValue);
                    _status = $"'{e.newValue}' is already used in {field}.";
                    UpdateDirtyState();
                });
                row.Add(languageField);

                var text = Css(new TextField { value = pair.Value, multiline = multiline }, "lang-value");
                if (multiline)
                    text.AddToClassList("multiline-text");
                text.RegisterValueChangedCallback(e =>
                {
                    DefinitionDocument.SetText(_current, field, language, e.newValue);
                    OnModelChanged(false);
                });
                row.Add(text);

                row.Add(Css(new Button(() =>
                {
                    DefinitionDocument.RemoveLanguage(_current, field, language);
                    OnModelChanged(true);
                }) { text = "−", tooltip = "Remove this language" }, "btn", "icon-btn"));
                card.Add(row);
            }

            card.Add(Css(new Button(() =>
            {
                DefinitionDocument.AddLanguage(_current, field);
                OnModelChanged(true);
            }) { text = "+ Add language" }, "btn", "link"));
            return card;
        }

        private VisualElement ExactMatchCard()
        {
            var card = Css(new VisualElement(), "card");
            card.Add(Css(Muted("IRIs of other registries that mean the same as this definition."), "card-hint"));
            var iris = DefinitionDocument.GetExactMatches(_current);
            for (var i = 0; i < iris.Count; i++)
            {
                var index = i;
                var row = Css(new VisualElement(), "lang-row");
                var field = Css(new TextField { value = iris[i] }, "lang-value");
                field.RegisterValueChangedCallback(e =>
                {
                    var list = DefinitionDocument.GetExactMatches(_current);
                    list[index] = e.newValue;
                    DefinitionDocument.SetExactMatches(_current, list);
                    OnModelChanged(false);
                });
                row.Add(field);
                row.Add(Css(new Button(() =>
                {
                    var list = DefinitionDocument.GetExactMatches(_current);
                    list.RemoveAt(index);
                    DefinitionDocument.SetExactMatches(_current, list);
                    OnModelChanged(true);
                }) { text = "−", tooltip = "Remove this IRI" }, "btn", "icon-btn"));
                card.Add(row);
            }

            card.Add(Css(new Button(() =>
            {
                var list = DefinitionDocument.GetExactMatches(_current);
                list.Add("");
                DefinitionDocument.SetExactMatches(_current, list);
                OnModelChanged(true);
            }) { text = "+ Add IRI" }, "btn", "link"));
            return card;
        }

        private VisualElement RawJsonFoldout()
        {
            var foldout = Css(new Foldout { text = "Raw JSON", value = SessionState.GetBool(RawJsonOpenKey, false) }, "raw-foldout");
            foldout.RegisterValueChangedCallback(e =>
            {
                if (e.target == foldout)
                    SessionState.SetBool(RawJsonOpenKey, e.newValue);
            });

            _rawField = Css(new TextField { value = DefinitionDocument.Serialize(_current), multiline = true }, "raw-json");
            var error = Css(new Label(), "error-text");
            var actions = Css(new VisualElement(), "card-row", "actions");
            var raw = _rawField;
            actions.Add(Css(new Button(() => ApplyRaw(raw.value, error)) { text = "Apply JSON" }, "btn", "btn--primary"));
            actions.Add(Css(new Button(() =>
            {
                raw.SetValueWithoutNotify(DefinitionDocument.Serialize(_current));
                error.text = "";
            }) { text = "Discard JSON edits" }, "btn"));

            foldout.Add(Muted("Edits here only take effect after 'Apply JSON'."));
            foldout.Add(_rawField);
            foldout.Add(error);
            foldout.Add(actions);
            return foldout;
        }

        private void ApplyRaw(string json, Label error)
        {
            try
            {
                _current = DefinitionDocument.Parse(json);
                _currentError = null;
                _currentRaw = null;
                OnModelChanged(true);
            }
            catch (Exception ex)
            {
                error.text = "Invalid JSON: " + ex.Message;
            }
        }

        #endregion

        #region Create, rename, delete

        private void StartCreate(Node at)
        {
            _mode = Mode.Create;
            _status = null;
            // The folder the user right-clicked or the selected definition decides the defaults
            _createDefaultKey = at?.Key ?? (_selectedPath != null && DefinitionsRoot != null ? DefinitionDocument.GetKey(DefinitionsRoot, _selectedPath) : "");
            RebuildDetail();
            UpdateDirtyState();
        }

        private void BuildCreate(VisualElement page)
        {
            page.Add(H1("New definition"));
            if (_root == null)
            {
                page.Add(Text("Set up the definitions in the wizard first."));
                return;
            }

            var contexts = _root.Children.Where(c => !c.IsFile).Select(c => c.Name).ToList();
            var defaultKey = _createDefaultKey ?? "";
            var defaultContext = DefinitionDocument.GetContext(defaultKey);
            var contextChoices = contexts.Concat(new[] { NewContextChoice }).ToList();
            var contextField = Css(new PopupField<string>("Context", contextChoices,
                contexts.Contains(defaultContext) ? defaultContext : contextChoices[0]), "field");
            var newContextField = Css(new TextField("New context name"), "field");

            var kindIndex = Array.FindIndex(Kinds, k => (defaultKey + "/").StartsWith($"{defaultContext}/{k.Folder}/", StringComparison.Ordinal));
            var kindField = Css(new PopupField<string>("Kind", Kinds.Select(k => k.Label).ToList(), Math.Max(0, kindIndex)), "field");
            var nameField = Css(new TextField("Name"), "field");
            var preview = Css(Muted(""), "field");
            var error = Css(new Label(), "error-text");

            string Context() => contextField.value == NewContextChoice ? ToCamelCase(newContextField.value.Trim()) : contextField.value;
            string Folder() => Kinds.First(k => k.Label == kindField.value).Folder;
            string FileName() => ToCamelCase(nameField.value.Trim());
            void UpdatePreview()
            {
                newContextField.style.display = contextField.value == NewContextChoice ? DisplayStyle.Flex : DisplayStyle.None;
                var name = FileName();
                var key = $"{Context()}/{Folder()}/{(string.IsNullOrEmpty(name) ? "<name>" : name)}";
                preview.text = $"File: {key}.json\nIRI: {DefinitionDocument.GetIri(key)}";
            }
            contextField.RegisterValueChangedCallback(_ => UpdatePreview());
            newContextField.RegisterValueChangedCallback(_ => UpdatePreview());
            kindField.RegisterValueChangedCallback(_ => UpdatePreview());
            nameField.RegisterValueChangedCallback(_ => UpdatePreview());
            UpdatePreview();

            var card = Css(new VisualElement(), "card");
            card.Add(contextField);
            card.Add(newContextField);
            card.Add(kindField);
            card.Add(nameField);
            card.Add(preview);
            card.Add(error);
            page.Add(card);

            var actions = Css(new VisualElement(), "card-row", "actions");
            actions.Add(Css(new Button(() => Create(Context(), Folder(), FileName(), error)) { text = "Create" }, "btn", "btn--primary"));
            actions.Add(Css(new Button(CancelMode) { text = "Cancel" }, "btn"));
            page.Add(actions);
            nameField.Focus();
        }

        private void Create(string context, string folder, string fileName, Label error)
        {
            if (!IsValidName(context) || !IsValidName(fileName))
            {
                error.text = "Enter a context and a name without characters that are invalid in file names.";
                return;
            }

            var directory = Path.Combine(DefinitionsRoot, context, folder);
            var path = Norm(Path.Combine(directory, fileName + ".json"));
            if (File.Exists(path))
            {
                error.text = $"'{context}/{folder}/{fileName}.json' already exists.";
                return;
            }

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, DefinitionDocument.Serialize(DefinitionDocument.CreateTemplate(fileName)) + "\n", new UTF8Encoding(false));
                if (ToAssetPath(path) != null)
                    AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                error.text = "Could not create the file: " + ex.Message;
                return;
            }

            Rescan();
            ExpandTo(path);
            RenderTree();
            _mode = Mode.View;
            _selectedPath = null;
            Select(path);
            _status = $"Created '{fileName}'.";
            UpdateDirtyState();
        }

        private void StartRename(string path)
        {
            if (path == null)
                return;
            _renameTarget = path;
            _mode = Mode.Rename;
            _status = null;
            RebuildDetail();
            UpdateDirtyState();
        }

        private void BuildRename(VisualElement page)
        {
            var isFile = File.Exists(_renameTarget);
            var oldName = isFile ? Path.GetFileNameWithoutExtension(_renameTarget) : Path.GetFileName(_renameTarget);
            page.Add(H1($"Rename '{oldName}'"));
            if (!isFile)
                page.Add(Text("Renaming a folder changes the IRIs of all definitions in it."));
            else
                page.Add(Text("Renaming a definition changes its IRI and the name of its generated property."));

            var nameField = Css(new TextField("New name") { value = oldName }, "field");
            var error = Css(new Label(), "error-text");
            var card = Css(new VisualElement(), "card");
            card.Add(nameField);
            card.Add(error);
            page.Add(card);

            var actions = Css(new VisualElement(), "card-row", "actions");
            actions.Add(Css(new Button(() => Rename(_renameTarget, ToCamelCase(nameField.value.Trim()), isFile, error)) { text = "Rename" }, "btn", "btn--primary"));
            actions.Add(Css(new Button(CancelMode) { text = "Cancel" }, "btn"));
            page.Add(actions);
            nameField.Focus();
        }

        private void Rename(string path, string newName, bool isFile, Label error)
        {
            if (!IsValidName(newName))
            {
                error.text = "Enter a name without characters that are invalid in file names.";
                return;
            }

            var newPath = Norm(Path.Combine(Path.GetDirectoryName(path) ?? "", isFile ? newName + ".json" : newName));
            if (newPath == path)
            {
                CancelMode();
                return;
            }
            if (File.Exists(newPath) || Directory.Exists(newPath))
            {
                error.text = $"'{newName}' already exists.";
                return;
            }

            var result = MoveEntry(path, newPath);
            if (!string.IsNullOrEmpty(result))
            {
                error.text = result;
                return;
            }

            // Unsaved changes and the selection move with the files
            RemapPaths(path, newPath);
            Rescan();
            ExpandTo(_selectedPath);
            RenderTree();
            _mode = Mode.View;
            LoadSelected();
            RebuildDetail();
            _status = $"Renamed to '{newName}'.";
            UpdateDirtyState();
        }

        private void Delete(string path)
        {
            if (path == null)
                return;
            var isFile = File.Exists(path);
            var name = isFile ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path);
            var count = isFile ? 1 : Directory.GetFiles(path, "*.json", SearchOption.AllDirectories).Length;
            var message = isFile
                ? $"Permanently delete the definition '{name}'?"
                : $"Permanently delete the folder '{name}' with {count} definition(s)?";
            if (!EditorUtility.DisplayDialog("Delete", message, "Delete", "Cancel"))
                return;

            try
            {
                var assetPath = ToAssetPath(path);
                if (assetPath != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                else
                {
                    if (isFile)
                        File.Delete(path);
                    else
                        Directory.Delete(path, true);
                    if (File.Exists(path + ".meta"))
                        File.Delete(path + ".meta");
                }
            }
            catch (Exception ex)
            {
                _status = "Could not delete: " + ex.Message;
                UpdateDirtyState();
                return;
            }

            foreach (var dirtyPath in _dirty.Keys.Where(p => p == path || p.StartsWith(path + "/", StringComparison.Ordinal)).ToList())
                _dirty.Remove(dirtyPath);
            if (_selectedPath != null && (_selectedPath == path || _selectedPath.StartsWith(path + "/", StringComparison.Ordinal)))
                _selectedPath = null;

            Rescan();
            RenderTree();
            _mode = Mode.View;
            LoadSelected();
            RebuildDetail();
            _status = $"Deleted '{name}'.";
            UpdateDirtyState();
        }

        private void CancelMode()
        {
            _mode = Mode.View;
            _status = null;
            RebuildDetail();
            UpdateDirtyState();
        }

        private void RemapPaths(string from, string to)
        {
            string Remap(string p) => p == from ? to : p.StartsWith(from + "/", StringComparison.Ordinal) ? to + p.Substring(from.Length) : p;

            foreach (var pair in _dirty.ToList())
            {
                var mapped = Remap(pair.Key);
                if (mapped == pair.Key)
                    continue;
                _dirty.Remove(pair.Key);
                _dirty[mapped] = pair.Value;
            }
            if (_selectedPath != null)
                _selectedPath = Remap(_selectedPath);
        }

        /// <summary>
        /// Moves a file or folder with its .meta file. Inside 'Assets', the AssetDatabase moves it.
        /// </summary>
        /// <returns>An error message, or an empty string on success.</returns>
        private static string MoveEntry(string from, string to)
        {
            var fromAsset = ToAssetPath(from);
            var toAsset = ToAssetPath(to);
            if (fromAsset != null && toAsset != null)
                return AssetDatabase.MoveAsset(fromAsset, toAsset);

            try
            {
                if (Directory.Exists(from))
                    Directory.Move(from, to);
                else
                    File.Move(from, to);
                if (File.Exists(from + ".meta"))
                    File.Move(from + ".meta", to + ".meta");
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static bool IsValidName(string name)
            => !string.IsNullOrEmpty(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && name != "." && name != "..";

        #endregion

        #region Saving

        private void SaveSelected()
        {
            if (_selectedPath == null || !_dirty.ContainsKey(_selectedPath))
                return;
            if (SaveFile(_selectedPath, _dirty[_selectedPath]))
                AfterSave(1, ToAssetPath(_selectedPath) != null);
        }

        private void SaveAll()
        {
            if (_dirty.Count == 0)
                return;
            var saved = 0;
            var allInAssets = true;
            // One import for all files, so the watcher generates the registry once
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var pair in _dirty.ToList())
                {
                    if (!SaveFile(pair.Key, pair.Value))
                        continue;
                    saved++;
                    allInAssets &= ToAssetPath(pair.Key) != null;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AfterSave(saved, allInAssets);
        }

        private bool SaveFile(string path, string json)
        {
            try
            {
                var previous = File.Exists(path) ? File.ReadAllText(path) : "";
                var text = previous.Length == 0 || previous.EndsWith("\n") ? json + "\n" : json;
                File.WriteAllText(path, text, new UTF8Encoding(false));
                _dirty.Remove(path);
                _originals[path] = json;

                var assetPath = ToAssetPath(path);
                if (assetPath != null)
                    AssetDatabase.ImportAsset(assetPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[xAPI4Unity] Could not save '{path}': {ex.Message}");
                _status = $"Could not save '{Path.GetFileName(path)}': {ex.Message}";
                return false;
            }
        }

        private void AfterSave(int saved, bool allInAssets)
        {
            if (saved > 0)
            {
                // The watcher only sees files that Unity imports, which are the ones inside 'Assets'
                _status = Settings.fetcher.watcherEnabled && allInAssets
                    ? $"Saved {saved} definition(s). The registry updates automatically."
                    : $"Saved {saved} definition(s). Use 'Update xAPI.Registry' to regenerate the C# classes.";
            }
            LoadSelected();
            RebuildDetail();
            UpdateDirtyState();
        }

        private void RevertSelected()
        {
            if (_selectedPath == null)
                return;
            _dirty.Remove(_selectedPath);
            _status = "Reverted to the saved file.";
            LoadSelected();
            RebuildDetail();
            UpdateDirtyState();
        }

        private void UpdateRegistry()
        {
            var log = LogCollector.Run(FetcherWindow.QuickFetch);
            _status = log.Errors.Count == 0
                ? "xAPI.Registry updated."
                : $"xAPI.Registry updated with {log.Errors.Count} error(s). See the Console.";
            UpdateDirtyState();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Converts a string to camelCase using space, underscore, and hyphen as separators.
        /// </summary>
        /// <param name="input">The input string to convert.</param>
        /// <returns>The camelCase version of the string.</returns>
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

        /// <summary>
        /// Converts a PascalCase or CamelCase string to a lowercase, space-separated sentence.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The transformed lowercase string.</returns>
        public static string ToLowercaseWords(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Insert a space before each capital letter (except the first character)
            var split = Regex.Replace(input, "(\\B[A-Z])", " $1");

            // Lowercase the entire result
            return split.ToLowerInvariant();
        }

        private static string Norm(string path) => path?.Replace('\\', '/');

        /// <summary>
        /// Path below 'Assets' for a file inside the project, otherwise null.
        /// </summary>
        private static string ToAssetPath(string path)
        {
            var dataPath = Norm(Application.dataPath);
            var full = Norm(Path.GetFullPath(path));
            if (full == dataPath)
                return "Assets";
            return full.StartsWith(dataPath + "/", StringComparison.Ordinal) ? "Assets" + full.Substring(dataPath.Length) : null;
        }

        private static string ToDisplayPath(string path)
        {
            var projectRoot = Norm(Path.GetDirectoryName(Application.dataPath)) + "/";
            var full = Norm(Path.GetFullPath(path));
            return full.StartsWith(projectRoot, StringComparison.Ordinal) ? full.Substring(projectRoot.Length) : full;
        }

        private static VisualElement CheckRow(bool passed, bool optional, string label, string hint)
        {
            var row = Css(new VisualElement(), "check");
            var icon = passed ? "TestPassed" : optional ? "console.infoicon.sml" : "console.warnicon.sml";
            row.Add(Css(new Image { image = EditorGUIUtility.IconContent(icon).image, scaleMode = ScaleMode.ScaleToFit }, "check-icon"));
            var body = Css(new VisualElement(), "card-body");
            body.Add(new Label(label));
            if (!passed && !string.IsNullOrEmpty(hint))
                body.Add(Muted(hint));
            row.Add(body);
            return row;
        }

        // Runs an action after the current event, because refreshing the AssetDatabase inside a UI event can break the layout
        private static Action Deferred(Action action) => () => EditorApplication.delayCall += () => action();

        private static Label H1(string text) => Css(new Label(text), "h1");
        private static Label H2(string text) => Css(new Label(text), "h2");
        private static Label Text(string text) => Css(new Label(text), "text");
        private static Label Muted(string text) => Css(new Label(text), "muted");

        private static T Css<T>(T element, params string[] classes) where T : VisualElement
        {
            foreach (var c in classes)
                element.AddToClassList(c);
            return element;
        }

        #endregion
    }
}
