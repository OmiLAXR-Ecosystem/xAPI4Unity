/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using xAPI4Unity.Editor.Settings;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace xAPI4Unity.Editor.Wizard
{
    /// <summary>
    /// Step-by-step wizard for setting up xAPI4Unity: get the JSON definitions, generate the C# registry and work with it.
    /// It follows the look of the OmiLAXR Wizard. <see cref="Startup"/> opens it when the definitions or the registry are missing.
    /// </summary>
    public sealed class XapiWizardWindow : EditorWindow
    {
        private const string PackageRoot = "Packages/com.rwth.unity.omilaxr.xapi4unity/Editor/Wizard/";
        private const string StyleSheetPath = PackageRoot + "XapiWizardWindow.uss";
        // Shown only if the OmiLAXR core package is installed, xAPI4Unity does not depend on it
        private const string LogoPath = "Packages/com.rwth.unity.omilaxr/Editor/Wizard/Images/OmiLAXR_Logo.png";
        private const string StepKey = "xAPI4Unity.Wizard.Step";
        private const string RegistryUrl = "https://omilaxr-ecosystem.github.io/xapi/";
        private const string RegistryDefine = "XAPI_REGISTRY_EXISTS";

        private enum Step
        {
            Welcome,
            Source,
            Generate,
            Use,
            Done
        }

        // Same order as LocalSourceSettings.selectedOptionIndex
        private enum SourceOption
        {
            PatchIntoAssets,
            DownloadZip,
            GitClone
        }

        private static readonly (Step Step, string Label)[] Steps =
        {
            (Step.Welcome, "Welcome"),
            (Step.Source, "Source"),
            (Step.Generate, "Generate"),
            (Step.Use, "Use"),
            (Step.Done, "Done"),
        };

        private sealed class Check
        {
            public string Label;
            public bool Passed;
            public string Hint;
            public string FixLabel;
            public Action Fix;
            public bool Optional;
        }

        /// <summary>
        /// Outcome of an action run from the wizard, shown below its buttons.
        /// </summary>
        private sealed class ActionResult
        {
            public string Title;
            public string Summary;
            public LogCollector Log;
            public DateTime Time;
        }

        private static MainSettings Settings => MainSettings.Instance;
        private readonly LocalSourceView _source = new LocalSourceView();
        private string _signature;
        private string _message;
        private ActionResult _generateResult;
        private ActionResult _validateResult;

        private VisualElement _stepper;
        private ScrollView _content;
        private Button _back;
        private Button _next;

        [MenuItem("xAPI4Unity / Open Wizard", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<XapiWizardWindow>();
            window.titleContent = new GUIContent("xAPI4Unity Wizard");
            window.minSize = new Vector2(560f, 520f);
            window.Show();
        }

        private static Step CurrentStep
        {
            get => (Step)SessionState.GetInt(StepKey, (int)Step.Welcome);
            set => SessionState.SetInt(StepKey, (int)value);
        }

        #region State

        private static string SourcePath => Settings.localSource.path;
        private static string DefinitionsPath => string.IsNullOrEmpty(SourcePath) ? null : Path.Combine(SourcePath, "definitions");
        private static string RegistryPath => Path.Combine(Application.dataPath, Settings.@namespace);
        // FetcherWindow reads the types config from '<folder>.config.json' next to the source folder
        private static string ConfigPath => string.IsNullOrEmpty(SourcePath) ? null : SourcePath.TrimEnd('/', '\\') + ".config.json";

        private static bool HasDefinitions => DefinitionsPath != null && Directory.Exists(DefinitionsPath);
        private static bool HasRegistry => Directory.Exists(RegistryPath);
        private static bool HasDefine => Startup.DefineExist(RegistryDefine);
        private static bool HasConfig => ConfigPath != null && File.Exists(ConfigPath);
        private static bool HasMergeReports => Directory.Exists(SourcePath) &&
                                               Directory.GetFiles(SourcePath, "*-xapi-report.json", SearchOption.TopDirectoryOnly).Length > 0;

        private static SourceOption SelectedOption
        {
            get => (SourceOption)Mathf.Clamp(Settings.localSource.selectedOptionIndex, 0, 2);
            set
            {
                Settings.localSource.selectedOptionIndex = (int)value;
                Settings.Save();
            }
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            Settings.Load();
            BuildLayout();
            EditorApplication.projectChanged += Evaluate;
            _signature = null;
            Evaluate();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Evaluate;
            Settings.Save();
        }

        private void OnFocus() => Evaluate();

        /// <summary>
        /// Checks the project state and rebuilds the page if it changed.
        /// </summary>
        private void Evaluate()
        {
            var signature = string.Concat(HasDefinitions, HasRegistry, HasDefine, HasConfig, HasMergeReports, _source.IsCloned);
            if (signature == _signature)
                return;
            _signature = signature;
            Rebuild();
        }

        #endregion

        #region Layout

        // Built in OnEnable instead of CreateGUI, which older Unity versions do not call
        private void BuildLayout()
        {
            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList("wizard");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet)
                root.styleSheets.Add(styleSheet);

            root.Add(CreateHeader());

            _stepper = new VisualElement();
            _stepper.AddToClassList("stepper");
            root.Add(_stepper);

            _content = new ScrollView();
            _content.style.flexGrow = 1;
            root.Add(_content);

            root.Add(CreateFooter());
        }

        private static VisualElement CreateHeader()
        {
            var header = Css(new VisualElement(), "header");

            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (logo)
            {
                var image = new Image { image = logo, scaleMode = ScaleMode.ScaleToFit };
                image.AddToClassList("logo");
                header.Add(image);
            }

            var text = Css(new VisualElement(), "header-text");
            text.Add(Css(new Label("xAPI4Unity"), "header-title"));

            var package = PackageInfo.FindForAssembly(typeof(XapiWizardWindow).Assembly);
            text.Add(Css(new Label($"xAPI definitions to C# · xAPI4Unity {package?.version ?? "?"} · Unity {Application.unityVersion}"), "header-subtitle"));
            header.Add(text);
            return header;
        }

        private VisualElement CreateFooter()
        {
            var footer = Css(new VisualElement(), "footer");

            var showOnStartup = new Toggle
            {
                text = "Show on startup",
                value = Settings.isOpeningOnStartup,
                tooltip = "Opens this wizard when the editor starts and the definitions or the registry are missing."
            };
            showOnStartup.RegisterValueChangedCallback(e =>
            {
                Settings.isOpeningOnStartup = e.newValue;
                Settings.Save();
            });
            footer.Add(showOnStartup);

            footer.Add(Css(new VisualElement(), "spacer"));

            _back = Css(new Button(() => GoTo(CurrentStep - 1)) { text = "Back" }, "btn");
            footer.Add(_back);

            _next = Css(new Button(() =>
            {
                if (CurrentStep == Step.Done)
                    Close();
                else
                    GoTo(CurrentStep + 1);
            }), "btn", "btn--primary");
            footer.Add(_next);
            return footer;
        }

        private void GoTo(Step step)
        {
            if (step < Step.Welcome || step > Step.Done)
                return;
            CurrentStep = step;
            _message = null;
            _generateResult = null;
            _validateResult = null;
            Rebuild();
            _content.scrollOffset = Vector2.zero;
        }

        /// <summary>
        /// Rebuilds the stepper, the current page and the footer buttons.
        /// </summary>
        private void Rebuild()
        {
            // The layout is not built yet
            if (_content == null || _next == null)
                return;

            var scroll = _content.scrollOffset;
            BuildStepper();

            _content.Clear();
            var page = Css(new VisualElement(), "page");
            switch (CurrentStep)
            {
                case Step.Welcome: BuildWelcome(page); break;
                case Step.Source: BuildSource(page); break;
                case Step.Generate: BuildGenerate(page); break;
                case Step.Use: BuildUse(page); break;
                case Step.Done: BuildDone(page); break;
            }
            _content.Add(page);
            _content.scrollOffset = scroll;

            _back.style.visibility = CurrentStep == Step.Welcome ? Visibility.Hidden : Visibility.Visible;
            _next.text = CurrentStep == Step.Done ? "Finish" : CurrentStep == Step.Welcome ? "Start" : "Next";
        }

        private void BuildStepper()
        {
            _stepper.Clear();
            for (var i = 0; i < Steps.Length; i++)
            {
                var (step, label) = Steps[i];
                var item = Css(new VisualElement(), "step");
                var state = StepState(step);
                if (step == CurrentStep)
                    item.AddToClassList("step--current");
                else if (state != null)
                    item.AddToClassList(state.Value ? "step--ok" : "step--warn");

                var symbol = step != CurrentStep && state != null ? (state.Value ? "✓" : "!") : (i + 1).ToString();
                item.Add(Css(new Button(() => GoTo(step)) { text = symbol }, "step-circle"));
                item.Add(Css(new Label(label), "step-label"));
                _stepper.Add(item);
            }
        }

        /// <summary>
        /// True if a step is complete, false if it needs attention, null if it has no state.
        /// </summary>
        private static bool? StepState(Step step)
        {
            switch (step)
            {
                case Step.Source: return HasDefinitions;
                case Step.Generate: return HasRegistry;
                default: return null;
            }
        }

        #endregion

        #region Pages

        private static void BuildWelcome(VisualElement page)
        {
            page.Add(Css(new Label("Welcome to xAPI4Unity"), "h1"));
            page.Add(Text("xAPI4Unity turns xAPI definitions (verbs, activities and extensions as JSON files) into C# classes " +
                          $"in 'Assets/{Settings.@namespace}'. The xAPI composers of OmiLAXR.xAPI use these classes to write statements. " +
                          "This wizard sets up your project in a few steps:"));

            var overview = Css(new VisualElement(), "overview");
            overview.Add(OverviewCard("1  Source", "Get the JSON definitions: patch them into 'Assets', download a zip or clone the git repository."));
            overview.Add(OverviewCard("2  Generate", "Generate the C# registry and keep it up to date when a definition changes."));
            overview.Add(OverviewCard("3  Use", "Edit definitions, resolve merge conflicts and export everything as a .unitypackage."));
            page.Add(overview);

            page.Add(Muted("You can come back any time via 'xAPI4Unity > Open Wizard'."));
        }

        private void BuildSource(VisualElement page)
        {
            page.Add(Css(new Label("Source"), "h1"));
            page.Add(Text("Where the xAPI JSON definitions come from. The local path must contain a 'definitions' folder."));

            page.Add(H2("Status"));
            page.Add(ChecksCard(new Check
            {
                Label = "JSON definitions",
                Passed = HasDefinitions,
                Hint = HasDefinitions ? null : $"No 'definitions' folder in '{ToDisplayPath(SourcePath)}'. Get the definitions below."
            }));

            page.Add(H2("Local path"));
            page.Add(PathCard());

            page.Add(H2("Get the definitions"));
            page.Add(OptionsCard());
            page.Add(OptionDetailsCard());

            if (_message != null)
                page.Add(Css(Muted(_message), "field"));
        }

        private void BuildGenerate(VisualElement page)
        {
            page.Add(Css(new Label("Generate"), "h1"));
            page.Add(Text($"Turns the JSON definitions into C# classes in 'Assets/{Settings.@namespace}'. " +
                          "Generated files are overwritten every time, so do not edit them by hand."));

            page.Add(H2("Status"));
            page.Add(ChecksCard(
                new Check
                {
                    Label = "JSON definitions",
                    Passed = HasDefinitions,
                    Hint = "Get them in the 'Source' step first."
                },
                new Check
                {
                    Label = "C# registry",
                    Passed = HasRegistry,
                    Hint = HasDefinitions ? "Not generated yet." : "Needs the JSON definitions.",
                    FixLabel = "Generate",
                    Fix = HasDefinitions ? (Action)RunGenerate : null
                },
                new Check
                {
                    Label = $"Types config '{Path.GetFileName(ConfigPath)}'",
                    Passed = HasConfig,
                    Hint = $"Types the extension values and lists contexts to skip. Without it, extensions use default types and every context is generated. " +
                           $"'Create' writes a default config to '{ToDisplayPath(ConfigPath)}'.",
                    FixLabel = "Create",
                    Fix = ConfigPath != null ? () => ConfigFile.Create(ConfigPath) : (Action)null,
                    Optional = true
                },
                new Check
                {
                    Label = RegistryDefine,
                    Passed = HasDefine,
                    Hint = "OmiLAXR.xAPI sets this define once the registry exists. You only need it with OmiLAXR.xAPI.",
                    Optional = true
                }));

            page.Add(H2("Options"));
            var options = Css(new VisualElement(), "card");
            var watcher = new Toggle { text = "Regenerate on changes", value = Settings.fetcher.watcherEnabled };
            watcher.RegisterValueChangedCallback(e =>
            {
                Settings.fetcher.watcherEnabled = e.newValue;
                Settings.Save();
            });
            options.Add(watcher);
            options.Add(Css(Muted("Regenerates the registry whenever a JSON file in a 'definitions' folder changes. Recommended."), "option-hint"));
            page.Add(options);

            var actions = Css(new VisualElement(), "card-row", "actions");
            actions.Add(Enabled(Css(new Button(Deferred(RunGenerate)) { text = "Update xAPI.Registry (JSON => C#)" }, "btn", "btn--primary"), HasDefinitions));
            actions.Add(Enabled(Css(new Button(RunValidate) { text = "Validate config" }, "btn"), HasConfig));
            page.Add(actions);

            if (_generateResult != null)
                page.Add(ResultCard(_generateResult));
            if (_validateResult != null)
                page.Add(ResultCard(_validateResult));
        }

        /// <summary>
        /// Generates the registry and keeps the logged errors and warnings for the result card.
        /// </summary>
        private void RunGenerate()
        {
            var log = LogCollector.Run(FetcherWindow.QuickFetch);
            var files = HasRegistry ? Directory.GetFiles(RegistryPath, "*.cs", SearchOption.AllDirectories).Length : 0;
            _generateResult = new ActionResult
            {
                Title = "Update xAPI.Registry",
                Summary = log.Errors.Count == 0
                    ? $"Generated {files} C# files in '{ToDisplayPath(RegistryPath)}'. Compile errors in them would show up in the Console."
                    : $"{log.Errors.Count} error(s) while generating. The registry may be incomplete.",
                Log = log,
                Time = DateTime.Now
            };
            // Force a rebuild, the project state may look the same as before
            _signature = null;
            Evaluate();
        }

        /// <summary>
        /// Validates the types config and keeps the logged errors and warnings for the result card.
        /// </summary>
        private void RunValidate()
        {
            var typeCount = 0;
            var log = LogCollector.Run(() => typeCount = ConfigFile.Validate(ConfigPath));
            _validateResult = new ActionResult
            {
                Title = "Validate config",
                Summary = log.Errors.Count == 0
                    ? $"'{ToDisplayPath(ConfigPath)}' is valid. All {typeCount} typed extension(s) resolve to C# types."
                    : $"{log.Errors.Count} problem(s) in '{ToDisplayPath(ConfigPath)}'.",
                Log = log,
                Time = DateTime.Now
            };
            Rebuild();
        }

        private static void BuildUse(VisualElement page)
        {
            page.Add(Css(new Label("Use"), "h1"));
            page.Add(Text("Tools for working with the definitions and the generated registry. Everything on this page is optional."));

            page.Add(ToolCard("Definitions editor",
                "Edit names and descriptions of verbs, activities and extensions. Saving regenerates the registry.",
                Css(new Button(XapiDefinitionsEditorWindow.ShowWindow) { text = "Open editor" }, "btn", "btn--primary")));

            page.Add(ToolCard("Merge conflicts",
                HasMergeReports
                    ? "'Download and patch' saved files that differ from yours as '*.json-conflict'. Pick a version per file."
                    : "No merge reports yet. 'Download and patch' creates one when downloaded files differ from yours.",
                Enabled(Css(new Button(XapiMergeWindow.ShowWindow) { text = "Resolve conflicts" }, "btn"), HasMergeReports)));

            page.Add(ToolCard("Export",
                "Share the definitions and the registry with other projects as a .unitypackage in the project folder.",
                Enabled(Css(new Button(UnityPackageExporter.ExportXApiFolder) { text = "Definitions" }, "btn"), HasDefinitions),
                Enabled(Css(new Button(UnityPackageExporter.ExportXApiRegistryFolder) { text = "Registry" }, "btn"), HasRegistry),
                Enabled(Css(new Button(UnityPackageExporter.ExportBoth) { text = "Both" }, "btn"), HasDefinitions && HasRegistry)));

            page.Add(ToolCard("xAPI registry",
                "Search all published verbs, activities and extensions, with the history and raw JSON of each definition.",
                Css(new Button(() => Application.OpenURL(RegistryUrl)) { text = "Open website" }, "btn")));
        }

        private static void BuildDone(VisualElement page)
        {
            var ready = HasDefinitions && HasRegistry;
            page.Add(Css(new Label(ready ? "All set" : "Almost done"), "h1"));
            page.Add(Text(ready
                ? $"The registry is ready. Use its classes from the '{Settings.@namespace}' namespace, for example in OmiLAXR.xAPI composers."
                : "Some steps still need attention. Go back to 'Source' or 'Generate' to fix them."));

            page.Add(H2("Summary"));
            var summary = Css(new VisualElement(), "card");
            summary.Add(SummaryRow("JSON definitions", HasDefinitions ? ToDisplayPath(DefinitionsPath) : "missing", HasDefinitions));
            summary.Add(SummaryRow("C# registry", HasRegistry ? ToDisplayPath(RegistryPath) : "not generated", HasRegistry));
            summary.Add(SummaryRow("Regenerate on changes", Settings.fetcher.watcherEnabled ? "on" : "off", Settings.fetcher.watcherEnabled));
            page.Add(summary);

            page.Add(H2("Learn more"));
            var links = Css(new VisualElement(), "card");
            links.Add(LinkButton("Documentation (omilaxr.dev)", "https://omilaxr.dev/"));
            links.Add(LinkButton("xAPI Registry", RegistryUrl));
            links.Add(LinkButton("xAPI definitions repository", DefaultValues.RepoDownloadUrl));
            links.Add(LinkButton("Changelog", "https://github.com/OmiLAXR-Ecosystem/xAPI4Unity/blob/main/CHANGELOG.md"));
            page.Add(links);
        }

        #endregion

        #region Source

        private VisualElement PathCard()
        {
            var card = Css(new VisualElement(), "card");
            var row = Css(new VisualElement(), "card-row");

            var path = Css(new TextField { value = SourcePath }, "path-field");
            path.RegisterValueChangedCallback(e =>
            {
                Settings.localSource.path = e.newValue;
                Settings.Save();
            });
            path.RegisterCallback<FocusOutEvent>(_ => Evaluate());
            row.Add(path);

            row.Add(Css(new Button(() =>
            {
                var browsed = EditorUtility.OpenFolderPanel("Browse xAPI definitions root path", SourcePath, "xapi");
                // An empty path means the dialog was cancelled
                if (string.IsNullOrEmpty(browsed))
                    return;
                Settings.localSource.path = browsed;
                Settings.Save();
                path.SetValueWithoutNotify(browsed);
                Evaluate();
            }) { text = "Browse" }, "btn"));
            row.Add(Enabled(Css(new Button(() => EditorUtility.RevealInFinder(SourcePath)) { text = "Open folder" }, "btn"),
                Directory.Exists(SourcePath)));
            card.Add(row);
            return card;
        }

        private VisualElement OptionsCard()
        {
            var card = Css(new VisualElement(), "card");
            AddOption(card, SourceOption.PatchIntoAssets, "Patch into Assets",
                "Downloads the repository archive and merges it into 'Assets/xapi'. New files are added, identical files are skipped, " +
                "and files that differ from yours are kept next to them for review.");
            AddOption(card, SourceOption.DownloadZip, "Download zip",
                "Downloads the archive with your browser. Extract it and set the local path to the extracted folder.");
            AddOption(card, SourceOption.GitClone, LocalSourceView.ProjectHasGit ? "Git submodule" : "Git clone",
                LocalSourceView.ProjectHasGit
                    ? "Adds the repository as a git submodule next to the local path and checks out a new local branch. Needs git."
                    : "Clones the repository next to the local path and checks out a new local branch. Needs git.");
            return card;
        }

        private void AddOption(VisualElement card, SourceOption option, string title, string description)
        {
            var toggle = new Toggle { text = title, value = SelectedOption == option };
            // The toggles act as radio buttons
            toggle.RegisterValueChangedCallback(e =>
            {
                if (!e.newValue)
                {
                    toggle.SetValueWithoutNotify(true);
                    return;
                }
                SelectedOption = option;
                _message = null;
                Rebuild();
            });
            card.Add(Css(toggle, "option"));
            card.Add(Css(Muted(description), "option-hint"));
        }

        private VisualElement OptionDetailsCard()
        {
            var card = Css(new VisualElement(), "card");
            var source = Settings.localSource;
            var option = SelectedOption;

            if (option == SourceOption.GitClone)
            {
                card.Add(SettingField("Git URL", source.repoUrl, v => source.repoUrl = v));
                card.Add(Css(Muted("Change this only if you work with a fork of the repository."), "field"));
            }
            else
            {
                var archive = Muted("");
                void UpdateArchive() => archive.text = $"Archive: {source.GetDownloadUrl()}";
                card.Add(SettingField("Repository URL", source.repoDownloadUrl, v =>
                {
                    source.repoDownloadUrl = v;
                    UpdateArchive();
                }));
                card.Add(SettingField("Branch", source.repoBranch, v =>
                {
                    source.repoBranch = v;
                    UpdateArchive();
                }));
                UpdateArchive();
                card.Add(Css(archive, "field"));
                card.Add(Muted("Change these only if you work with a fork of the repository."));
            }

            var actions = Css(new VisualElement(), "card-row", "actions");
            switch (option)
            {
                case SourceOption.PatchIntoAssets:
                    actions.Add(Css(new Button(() =>
                    {
                        _message = "Downloading... The definitions appear in 'Assets/xapi' once the download is done.";
                        Rebuild();
                        EditorApplication.delayCall += _source.DownloadToAssetsAndPatch;
                    }) { text = "Download and patch" }, "btn", "btn--primary"));
                    if (HasMergeReports)
                        actions.Add(Css(new Button(XapiMergeWindow.ShowWindow) { text = "Resolve conflicts" }, "btn"));
                    break;
                case SourceOption.DownloadZip:
                    actions.Add(Css(new Button(_source.DownloadToDownloads) { text = "Download zip" }, "btn", "btn--primary"));
                    break;
                case SourceOption.GitClone:
                    if (_source.IsCloned)
                    {
                        actions.Add(Enabled(Css(new Button(Deferred(_source.PullClone)) { text = "Pull latest changes" }, "btn", "btn--primary"),
                            !_source.IsBusy));
                    }
                    else
                    {
                        actions.Add(Enabled(Css(new Button(Deferred(() =>
                        {
                            _source.CloneRepository();
                            AssetDatabase.Refresh();
                        })) { text = LocalSourceView.ProjectHasGit ? "Add submodule" : "Clone repository" }, "btn", "btn--primary"),
                            !_source.IsBusy));
                    }
                    break;
            }
            card.Add(actions);
            return card;
        }

        private static TextField SettingField(string label, string value, Action<string> onChange)
        {
            var field = Css(new TextField(label) { value = value }, "field");
            field.RegisterValueChangedCallback(e =>
            {
                onChange(e.newValue);
                Settings.Save();
            });
            return field;
        }

        #endregion

        #region Building blocks

        private VisualElement ChecksCard(params Check[] checks)
        {
            var card = Css(new VisualElement(), "card");
            foreach (var check in checks)
                card.Add(CheckRow(check));
            return card;
        }

        private VisualElement CheckRow(Check check)
        {
            var row = Css(new VisualElement(), "check");

            var icon = check.Passed ? "TestPassed" : check.Optional ? "console.infoicon.sml" : "console.warnicon.sml";
            row.Add(Css(new Image { image = EditorGUIUtility.IconContent(icon).image, scaleMode = ScaleMode.ScaleToFit }, "check-icon"));

            var body = Css(new VisualElement(), "card-body");
            body.Add(new Label(check.Label));
            if (!check.Passed && !string.IsNullOrEmpty(check.Hint))
                body.Add(Muted(check.Hint));
            row.Add(body);

            if (!check.Passed && check.Fix != null)
                row.Add(Css(new Button(Deferred(() =>
                {
                    check.Fix();
                    Evaluate();
                })) { text = check.FixLabel ?? "Fix" }, "btn", "btn--primary"));
            return row;
        }

        private static VisualElement ResultCard(ActionResult result)
        {
            const int maxMessages = 8;
            var card = Css(new VisualElement(), "card", "field");
            var ok = result.Log.Errors.Count == 0;
            card.Add(MessageRow(ok ? "TestPassed" : "console.erroricon.sml", $"{result.Title} · {result.Time:HH:mm:ss}", result.Summary));

            var messages = new List<(string Icon, string Text)>();
            foreach (var error in result.Log.Errors)
                messages.Add(("console.erroricon.sml", error));
            foreach (var warning in result.Log.Warnings)
                messages.Add(("console.warnicon.sml", warning));

            foreach (var (icon, text) in messages.Take(maxMessages))
                card.Add(MessageRow(icon, null, text));
            if (messages.Count > maxMessages)
                card.Add(Muted($"... and {messages.Count - maxMessages} more. See the Console."));
            return card;
        }

        private static VisualElement MessageRow(string icon, string title, string text)
        {
            var row = Css(new VisualElement(), "check");
            row.Add(Css(new Image { image = EditorGUIUtility.IconContent(icon).image, scaleMode = ScaleMode.ScaleToFit }, "check-icon"));
            var body = Css(new VisualElement(), "card-body");
            if (title != null)
                body.Add(Css(new Label(title), "card-title"));
            body.Add(title != null ? Muted(text) : Css(new Label(text), "text"));
            row.Add(body);
            return row;
        }

        private static VisualElement ToolCard(string title, string description, params Button[] buttons)
        {
            var card = Css(new VisualElement(), "card");
            var row = Css(new VisualElement(), "card-row");

            var body = Css(new VisualElement(), "card-body");
            body.Add(Css(new Label(title), "card-title"));
            body.Add(Css(Text(description), "field"));
            row.Add(body);

            var actions = Css(new VisualElement(), "card-actions");
            foreach (var button in buttons)
                actions.Add(button);
            row.Add(actions);

            card.Add(row);
            return card;
        }

        private static VisualElement OverviewCard(string title, string text)
        {
            var card = Css(new VisualElement(), "card");
            card.Add(Css(new Label(title), "card-title"));
            card.Add(Css(Text(text), "field"));
            return card;
        }

        private static VisualElement SummaryRow(string label, string value, bool ok)
        {
            var row = Css(new VisualElement(), "check");
            row.Add(Css(new Image { image = EditorGUIUtility.IconContent(ok ? "TestPassed" : "console.infoicon.sml").image, scaleMode = ScaleMode.ScaleToFit }, "check-icon"));
            var body = Css(new VisualElement(), "card-body");
            body.Add(Css(new Label(label), "card-title"));
            body.Add(Muted(value));
            row.Add(body);
            return row;
        }

        /// <summary>
        /// Shortens a path inside the Unity project to a project-relative path.
        /// </summary>
        private static string ToDisplayPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";
            var fullPath = Path.GetFullPath(path).Replace('\\', '/');
            var projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/') + "/";
            return fullPath.StartsWith(projectRoot) ? fullPath.Substring(projectRoot.Length) : fullPath;
        }

        // Runs an action after the current event, because refreshing the AssetDatabase inside a UI event can break the layout
        private static Action Deferred(Action action) => () => EditorApplication.delayCall += () => action();

        private static Label H2(string text) => Css(new Label(text), "h2");
        private static Label Text(string text) => Css(new Label(text), "text");
        private static Label Muted(string text) => Css(new Label(text), "muted");

        private static Button LinkButton(string text, string url)
            => Css(new Button(() => Application.OpenURL(url)) { text = text }, "btn", "link");

        private static T Enabled<T>(T element, bool enabled) where T : VisualElement
        {
            element.SetEnabled(enabled);
            return element;
        }

        private static T Css<T>(T element, params string[] classes) where T : VisualElement
        {
            foreach (var c in classes)
                element.AddToClassList(c);
            return element;
        }

        #endregion
    }
}
#endif
