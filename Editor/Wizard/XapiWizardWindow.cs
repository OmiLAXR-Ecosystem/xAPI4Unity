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
using UnityEditor.UIElements;
using xAPI4Unity.Editor.Releases;
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
        /// <summary>
        /// Folder of the wizard's assets in this package.
        /// </summary>
        private const string PackageRoot = "Packages/com.rwth.unity.omilaxr.xapi4unity/Editor/Wizard/";
        /// <summary>
        /// Style sheet of the wizard.
        /// </summary>
        private const string StyleSheetPath = PackageRoot + "XapiWizardWindow.uss";
        /// <summary>
        /// OmiLAXR logo, shown in the header only if the OmiLAXR core package is installed. xAPI4Unity does not depend on it.
        /// </summary>
        private const string LogoPath = "Packages/com.rwth.unity.omilaxr/Editor/Wizard/Images/OmiLAXR_Logo.png";
        /// <summary>
        /// SessionState key of the current step, so the step survives script reloads.
        /// </summary>
        private const string StepKey = "xAPI4Unity.Wizard.Step";
        /// <summary>
        /// Website of the xAPI registry.
        /// </summary>
        private const string RegistryUrl = "https://omilaxr-ecosystem.github.io/xapi/";
        /// <summary>
        /// Define that OmiLAXR.xAPI sets once the registry exists.
        /// </summary>
        private const string RegistryDefine = "XAPI_REGISTRY_EXISTS";
        /// <summary>
        /// Title of the window.
        /// </summary>
        private const string Title = "xAPI4Unity Wizard";
        /// <summary>
        /// Built-in icon that marks the window and the Source step when a newer official release exists.
        /// </summary>
        private const string UpdateIcon = "console.infoicon.sml";

        /// <summary>
        /// Steps of the wizard, in order.
        /// </summary>
        private enum Step
        {
            /// <summary>
            /// Introduction and overview.
            /// </summary>
            Welcome,
            /// <summary>
            /// Where the definitions come from.
            /// </summary>
            Source,
            /// <summary>
            /// Generate the registry and set the options.
            /// </summary>
            Generate,
            /// <summary>
            /// Optional tools: definitions editor, merge conflicts, export.
            /// </summary>
            Use,
            /// <summary>
            /// Summary and links.
            /// </summary>
            Done
        }

        /// <summary>
        /// How the wizard gets the definitions of a custom repository. Same order as <see cref="LocalSourceSettings.selectedOptionIndex"/>.
        /// </summary>
        private enum SourceOption
        {
            /// <summary>
            /// Download the repository archive and merge it into <c>Assets/xapi</c>.
            /// </summary>
            PatchIntoAssets,
            /// <summary>
            /// Download the archive with the browser.
            /// </summary>
            DownloadZip,
            /// <summary>
            /// Clone the repository with git, as a submodule if the project uses git.
            /// </summary>
            GitClone
        }

        /// <summary>
        /// Steps with their labels in the stepper.
        /// </summary>
        private static readonly (Step Step, string Label)[] Steps =
        {
            (Step.Welcome, "Welcome"),
            (Step.Source, "Source"),
            (Step.Generate, "Generate"),
            (Step.Use, "Use"),
            (Step.Done, "Done"),
        };

        /// <summary>
        /// One line of a status card: a check with a hint and an optional fix button.
        /// </summary>
        private sealed class Check
        {
            /// <summary>
            /// Text of the check.
            /// </summary>
            public string Label;
            /// <summary>
            /// True if the check passed.
            /// </summary>
            public bool Passed;
            /// <summary>
            /// Shown below the label while the check fails.
            /// </summary>
            public string Hint;
            /// <summary>
            /// Text of the fix button. Defaults to <c>Fix</c>.
            /// </summary>
            public string FixLabel;
            /// <summary>
            /// Action of the fix button. No button without it.
            /// </summary>
            public Action Fix;
            /// <summary>
            /// An optional check shows an info icon instead of a warning when it fails.
            /// </summary>
            public bool Optional;
        }

        /// <summary>
        /// Outcome of an action run from the wizard, shown below its buttons.
        /// </summary>
        private sealed class ActionResult
        {
            /// <summary>
            /// Name of the action.
            /// </summary>
            public string Title;
            /// <summary>
            /// One sentence about the outcome.
            /// </summary>
            public string Summary;
            /// <summary>
            /// Errors and warnings logged while the action ran.
            /// </summary>
            public LogCollector Log;
            /// <summary>
            /// When the action ran.
            /// </summary>
            public DateTime Time;
        }

        /// <summary>
        /// The settings of this project.
        /// </summary>
        private static MainSettings Settings => MainSettings.Instance;
        /// <summary>
        /// Download, patch, clone and pull actions of the Source step.
        /// </summary>
        private readonly LocalSourceView _source = new LocalSourceView();
        /// <summary>
        /// Project state of the last <see cref="Evaluate"/>, to rebuild only when it changes.
        /// </summary>
        private string _signature;
        /// <summary>
        /// Message shown below the Source step's actions, such as a running download.
        /// </summary>
        private string _message;
        /// <summary>
        /// Result of the last Generate run, shown in the Generate step.
        /// </summary>
        private ActionResult _generateResult;
        /// <summary>
        /// Result of the last config validation, shown in the Generate step.
        /// </summary>
        private ActionResult _validateResult;

        /// <summary>
        /// Row with one circle per step.
        /// </summary>
        private VisualElement _stepper;
        /// <summary>
        /// Scrollable area that holds the current page.
        /// </summary>
        private ScrollView _content;
        /// <summary>
        /// Footer button to the previous step.
        /// </summary>
        private Button _back;
        /// <summary>
        /// Footer button to the next step, or Finish on the last one.
        /// </summary>
        private Button _next;

        /// <summary>
        /// Opens the wizard (<c>xAPI4Unity / Open Wizard</c>).
        /// </summary>
        [MenuItem("xAPI4Unity / Open Wizard", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<XapiWizardWindow>();
            window.UpdateTitle();
            window.minSize = new Vector2(560f, 520f);
            window.Show();
        }

        /// <summary>
        /// The step that is shown, kept in SessionState.
        /// </summary>
        private static Step CurrentStep
        {
            get => (Step)SessionState.GetInt(StepKey, (int)Step.Welcome);
            set => SessionState.SetInt(StepKey, (int)value);
        }

        #region State

        /// <summary>
        /// Paths and checks of the project, shared with the menu items and <see cref="Startup"/>.
        /// </summary>
        private static ProjectStatus Status => new ProjectStatus(RegistryPaths.FromSettings(Settings));
        /// <summary>
        /// Source folder from the settings.
        /// </summary>
        private static string SourcePath => Settings.localSource.path;
        /// <summary>
        /// The <c>definitions</c> folder in the source folder.
        /// </summary>
        private static string DefinitionsPath => Status.Paths.DefinitionsFolder;
        /// <summary>
        /// The registry folder <c>Assets/&lt;namespace&gt;</c>.
        /// </summary>
        private static string RegistryPath => Status.Paths.RegistryFolder;
        /// <summary>
        /// The types config next to the source folder.
        /// </summary>
        private static string ConfigPath => Status.Paths.ConfigFile;

        /// <summary>
        /// The source folder has a <c>definitions</c> folder.
        /// </summary>
        private static bool HasDefinitions => Status.HasDefinitions;
        /// <summary>
        /// The registry folder exists.
        /// </summary>
        private static bool HasRegistry => Status.HasRegistry;
        /// <summary>
        /// The <c>XAPI_REGISTRY_EXISTS</c> define is set.
        /// </summary>
        private static bool HasDefine => Startup.DefineExist(RegistryDefine);
        /// <summary>
        /// The types config exists.
        /// </summary>
        private static bool HasConfig => Status.HasConfig;
        /// <summary>
        /// The source folder has merge reports.
        /// </summary>
        private static bool HasMergeReports => Status.HasMergeReports;
        /// <summary>
        /// True if the definitions come from the official releases.
        /// </summary>
        private static bool IsOfficial => Settings.localSource.IsOfficial;
        /// <summary>
        /// A newer official release than the installed one, or null.
        /// </summary>
        private static ReleaseInfo AvailableUpdate => OfficialReleases.AvailableUpdate;

        /// <summary>
        /// The selected source option, saved in the settings.
        /// </summary>
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

        /// <summary>
        /// Loads the settings, builds the layout and starts watching for project changes.
        /// </summary>
        private void OnEnable()
        {
            Settings.Load();
            BuildLayout();
            EditorApplication.projectChanged += Evaluate;
            OfficialReleases.Changed += OnReleasesChanged;
            _signature = null;
            Evaluate();
            UpdateTitle();

            // Check for a newer official release whenever the window opens
            if (IsOfficial)
                OfficialReleases.Refresh();
        }

        /// <summary>
        /// Stops watching for project changes and saves the settings.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.projectChanged -= Evaluate;
            OfficialReleases.Changed -= OnReleasesChanged;
            Settings.Save();
        }

        /// <summary>
        /// Checks the project again when the window gets focus.
        /// </summary>
        private void OnFocus() => Evaluate();

        /// <summary>
        /// Checks the project state and rebuilds the page if it changed.
        /// </summary>
        private void Evaluate()
        {
            var signature = string.Concat(HasDefinitions, HasRegistry, HasDefine, HasConfig, HasMergeReports, _source.IsCloned,
                IsOfficial, OfficialReleases.InstalledVersion, AvailableUpdate?.tag_name);
            if (signature == _signature)
                return;
            _signature = signature;
            Rebuild();
            UpdateTitle();
        }

        /// <summary>
        /// Rebuilds the page when the official releases start or finish loading.
        /// </summary>
        private void OnReleasesChanged()
        {
            _signature = null;
            Evaluate();
        }

        /// <summary>
        /// Sets the window title, with an icon and a tooltip when a newer official release exists.
        /// </summary>
        private void UpdateTitle()
        {
            var update = AvailableUpdate;
            titleContent = update != null
                ? new GUIContent(Title, EditorGUIUtility.IconContent(UpdateIcon).image,
                    $"xAPI definitions {update.tag_name} are available (installed: v{OfficialReleases.InstalledVersion}).")
                : new GUIContent(Title);
        }

        #endregion

        #region Layout

        /// <summary>
        /// Builds the header, stepper, content area and footer. Called in OnEnable instead of CreateGUI, which older Unity versions do not call.
        /// </summary>
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

        /// <summary>
        /// Creates the header with the logo (if available), the title and the versions.
        /// </summary>
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

        /// <summary>
        /// Creates the footer with the startup toggle and the Back and Next buttons.
        /// </summary>
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

        /// <summary>
        /// Shows a step and clears the messages and results of the previous one.
        /// </summary>
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

        /// <summary>
        /// Builds the stepper: one circle per step, marked done or needing attention.
        /// </summary>
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
                // A newer official release marks the step, too
                case Step.Source: return HasDefinitions && AvailableUpdate == null;
                case Step.Generate: return HasRegistry;
                default: return null;
            }
        }

        #endregion

        #region Pages

        /// <summary>
        /// Builds the Welcome page.
        /// </summary>
        private static void BuildWelcome(VisualElement page)
        {
            page.Add(Css(new Label("Welcome to xAPI4Unity"), "h1"));
            page.Add(Text("xAPI4Unity turns xAPI definitions (verbs, activities and extensions as JSON files) into C# classes " +
                          $"in 'Assets/{Settings.@namespace}'. The xAPI composers of OmiLAXR.xAPI use these classes to write statements. " +
                          "This wizard sets up your project in a few steps:"));

            var overview = Css(new VisualElement(), "overview");
            overview.Add(OverviewCard("1  Source", "Get the JSON definitions: install a release of the official OmiLAXR definitions, or use a repository of your own."));
            overview.Add(OverviewCard("2  Generate", "Generate the C# registry and keep it up to date when a definition changes."));
            overview.Add(OverviewCard("3  Use", "Edit definitions, resolve merge conflicts and export everything as a .unitypackage."));
            page.Add(overview);

            page.Add(Muted("You can come back any time via 'xAPI4Unity > Open Wizard'."));
        }

        /// <summary>
        /// Builds the Source page: status, the choice between the official releases and a custom repository, and its settings.
        /// </summary>
        private void BuildSource(VisualElement page)
        {
            page.Add(Css(new Label("Source"), "h1"));
            page.Add(Text("Where the xAPI JSON definitions come from: a release of the official OmiLAXR definitions, or a repository of your own. " +
                          "The local path must contain a 'definitions' folder."));

            page.Add(H2("Status"));
            var checks = new List<Check>
            {
                new Check
                {
                    Label = "JSON definitions",
                    Passed = HasDefinitions,
                    Hint = HasDefinitions ? null : $"No 'definitions' folder in '{ToDisplayPath(SourcePath)}'. Get the definitions below."
                }
            };
            if (IsOfficial)
                checks.Add(ReleaseCheck());
            page.Add(ChecksCard(checks.ToArray()));

            page.Add(H2("Repository"));
            page.Add(SourceCard());

            if (IsOfficial)
            {
                BuildOfficial(page);
            }
            else
            {
                page.Add(H2("Local path"));
                page.Add(PathCard());

                page.Add(H2("Get the definitions"));
                page.Add(OptionsCard());
                page.Add(OptionDetailsCard());

                if (_message != null)
                    page.Add(Css(Muted(_message), "field"));
            }
        }

        /// <summary>
        /// Builds the Generate page: status checks, options and the generate and validate buttons.
        /// </summary>
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
            var files = Status.RegistryFileCount;
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

        /// <summary>
        /// Builds the Use page with the optional tools.
        /// </summary>
        private static void BuildUse(VisualElement page)
        {
            page.Add(Css(new Label("Use"), "h1"));
            page.Add(Text("Tools for working with the definitions and the generated registry. Everything on this page is optional."));

            page.Add(ToolCard("Definitions editor",
                "Edit names and descriptions of verbs, activities and extensions. Saving regenerates the registry.",
                Css(new Button(XapiDefinitionsEditorWindow.ShowWindow) { text = "Open editor" }, "btn", "btn--primary")));

            page.Add(ToolCard("Merge conflicts",
                HasMergeReports
                    ? "Installing a release or 'Download and patch' saved files that differ from yours as '*.json-conflict'. Pick a version per file."
                    : "No merge reports yet. Installing a release or 'Download and patch' creates one when downloaded files differ from yours.",
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

        /// <summary>
        /// Builds the Done page with a summary and links.
        /// </summary>
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
            if (IsOfficial)
            {
                var installed = OfficialReleases.InstalledVersion;
                var update = AvailableUpdate;
                summary.Add(SummaryRow("Official release",
                    installed == null ? "not installed" : update != null ? $"v{installed}, {update.tag_name} is available" : $"v{installed}",
                    installed != null && update == null));
            }
            summary.Add(SummaryRow("C# registry", HasRegistry ? ToDisplayPath(RegistryPath) : "not generated", HasRegistry));
            summary.Add(SummaryRow("Regenerate on changes", Settings.fetcher.watcherEnabled ? "on" : "off", Settings.fetcher.watcherEnabled));
            page.Add(summary);

            page.Add(H2("Learn more"));
            var links = Css(new VisualElement(), "card");
            links.Add(LinkButton("Documentation (omilaxr.dev)", "https://omilaxr.dev/"));
            links.Add(LinkButton("xAPI Registry", RegistryUrl));
            links.Add(LinkButton("xAPI definitions repository", DefaultValues.OfficialRepoUrl));
            links.Add(LinkButton("xAPI definitions releases", DefaultValues.OfficialReleasesUrl));
            links.Add(LinkButton("Changelog", "https://github.com/OmiLAXR-Ecosystem/xAPI4Unity/blob/main/CHANGELOG.md"));
            page.Add(links);
        }

        #endregion

        #region Source

        /// <summary>
        /// Creates the check of the installed official release, with a button to install or update it.
        /// </summary>
        private Check ReleaseCheck()
        {
            var installed = OfficialReleases.InstalledVersion;
            var update = AvailableUpdate;
            var latest = OfficialReleases.Releases?.Latest();
            var target = update ?? latest;
            return new Check
            {
                Label = installed != null ? $"Official release v{installed}" : "Official release",
                Passed = installed != null && update == null,
                Hint = update != null
                    ? $"{update.tag_name} is available. See its changelog below."
                    : "No official release is installed in 'Assets/xapi' yet.",
                FixLabel = target != null ? (update != null ? $"Update to {target.tag_name}" : $"Install {target.tag_name}") : null,
                Fix = target != null && !_source.IsInstalling ? () => Install(target) : (Action)null,
                // A newer release is news, not a problem
                Optional = update != null
            };
        }

        /// <summary>
        /// Creates the card with the choice between the official releases and a custom repository.
        /// </summary>
        private VisualElement SourceCard()
        {
            var card = Css(new VisualElement(), "card");
            AddRadio(card, IsOfficial, "OmiLAXR official",
                $"Released versions of the official definitions ({DefaultValues.OfficialRepository}). Pick a version, " +
                "read its changelog and install it into 'Assets/xapi'. xAPI4Unity tells you when a newer version is out.",
                () => SelectSource(DefinitionsSource.Official));
            AddRadio(card, !IsOfficial, "Custom repository",
                "Any repository with a 'definitions' folder, such as a fork or the old Learning Technologies repository. " +
                "Patch, download or clone a branch, a tag, a commit or HEAD.",
                () => SelectSource(DefinitionsSource.Custom));
            return card;
        }

        /// <summary>
        /// Saves the chosen source and loads the official releases if needed.
        /// </summary>
        private void SelectSource(DefinitionsSource source)
        {
            Settings.localSource.Source = source;
            Settings.Save();
            _message = null;
            if (source == DefinitionsSource.Official)
                OfficialReleases.Refresh();
            _signature = null;
            Evaluate();
        }

        /// <summary>
        /// The release picked in the version field: <see cref="LocalSourceSettings.officialVersion"/>, or the latest one.
        /// </summary>
        private static ReleaseInfo SelectedRelease(ReleaseList releases)
            => releases.Find(Settings.localSource.officialVersion) ?? releases.Latest() ?? releases.All.FirstOrDefault();

        /// <summary>
        /// Builds the official part of the Source page: version, install button and changelog.
        /// </summary>
        private void BuildOfficial(VisualElement page)
        {
            page.Add(H2("Version"));
            var card = Css(new VisualElement(), "card");
            page.Add(card);

            var releases = OfficialReleases.Releases;
            if (releases == null || releases.All.Count == 0)
            {
                card.Add(Muted(OfficialReleases.IsLoading
                    ? "Loading the releases from GitHub..."
                    : OfficialReleases.Error != null
                        ? $"Could not load the releases. {OfficialReleases.Error}"
                        : "No releases found."));
                var retry = Css(new VisualElement(), "card-row", "actions");
                retry.Add(Enabled(Css(new Button(() => OfficialReleases.Refresh(true)) { text = "Retry" }, "btn", "btn--primary"), !OfficialReleases.IsLoading));
                retry.Add(Css(new Button(() => Application.OpenURL(DefaultValues.OfficialReleasesUrl)) { text = "Open releases page" }, "btn"));
                card.Add(retry);
                return;
            }

            var installed = OfficialReleases.InstalledVersion;
            ReleaseVersion.TryParse(installed, out var installedVersion);
            var latest = releases.Latest();
            var selected = SelectedRelease(releases);

            string Format(string tag)
            {
                var release = releases.Find(tag);
                var marks = new List<string>();
                if (release == latest) marks.Add("latest");
                if (installed != null && release != null && release.Version.Equals(installedVersion)) marks.Add("installed");
                if (release != null && release.IsPreRelease) marks.Add("pre-release");
                return marks.Count > 0 ? $"{tag}  ({string.Join(", ", marks)})" : tag;
            }

            var row = Css(new VisualElement(), "card-row");
            var choices = releases.All.Select(r => r.tag_name).ToList();
            var version = Css(new PopupField<string>("Version", choices, Mathf.Max(0, choices.IndexOf(selected.tag_name)), Format, Format), "path-field");
            version.RegisterValueChangedCallback(e =>
            {
                // Empty means 'latest', so a new release is picked up without changing the setting
                Settings.localSource.officialVersion = e.newValue == latest?.tag_name ? "" : e.newValue;
                Settings.Save();
                _message = null;
                Rebuild();
            });
            row.Add(version);
            row.Add(Enabled(Css(new Button(() => OfficialReleases.Refresh(true)) { text = OfficialReleases.IsLoading ? "Checking..." : "Check for updates" }, "btn"),
                !OfficialReleases.IsLoading));
            card.Add(row);

            var loadedAt = OfficialReleases.LoadedAt;
            card.Add(Css(Muted($"Installed: {(installed != null ? "v" + installed : "none")} · Latest: {latest?.tag_name ?? "none"}" +
                               (loadedAt.HasValue ? $" · Checked at {loadedAt.Value.ToLocalTime():HH:mm}" : "")), "field"));

            var update = AvailableUpdate;
            if (update != null)
                card.Add(Css(MessageRow(UpdateIcon, $"New version {update.tag_name} available",
                    $"You have v{installed}. Update to get the new and corrected definitions. Files you changed are kept."), "field"));

            string label;
            if (installed == null)
                label = $"Install {selected.tag_name}";
            else
            {
                var order = selected.Version.CompareTo(installedVersion);
                label = order == 0 ? $"Reinstall {selected.tag_name}" : order > 0 ? $"Update to {selected.tag_name}" : $"Downgrade to {selected.tag_name}";
            }

            var actions = Css(new VisualElement(), "card-row", "actions");
            actions.Add(Enabled(Css(new Button(Deferred(() => Install(selected))) { text = _source.IsInstalling ? "Installing..." : label }, "btn", "btn--primary"),
                !_source.IsInstalling));
            if (!string.IsNullOrEmpty(selected.html_url))
                actions.Add(Css(new Button(() => Application.OpenURL(selected.html_url)) { text = "Open on GitHub" }, "btn"));
            if (HasMergeReports)
                actions.Add(Css(new Button(XapiMergeWindow.ShowWindow) { text = "Resolve conflicts" }, "btn"));
            card.Add(actions);
            card.Add(Css(Muted("Installs into 'Assets/xapi'. Files you did not change are updated, files you changed are kept and the release's " +
                               "version is saved next to them as '*.json-conflict'."), "field"));

            if (_message != null)
                card.Add(Css(Text(_message), "field"));

            page.Add(H2(string.IsNullOrEmpty(selected.PublishedDate) ? $"Changelog {selected.tag_name}" : $"Changelog {selected.tag_name} · {selected.PublishedDate}"));
            var notes = Css(new VisualElement(), "card");
#if UNITY_2021_2_OR_NEWER
            // UI Toolkit renders underline and strikethrough tags since TextCore
            const bool extendedTags = true;
#else
            const bool extendedTags = false;
#endif
            var codeColor = EditorGUIUtility.isProSkin ? "#99C9ED" : "#2959A5";
            var changelog = string.IsNullOrWhiteSpace(selected.body)
                ? "This release has no release notes."
                : MarkdownRichText.Convert(selected.body, codeColor, extendedTags);
            notes.Add(Css(new Label(changelog), "text", "changelog"));
            page.Add(notes);
        }

        /// <summary>
        /// Installs an official release and shows the outcome below the version.
        /// </summary>
        private void Install(ReleaseInfo release)
        {
            _message = $"Downloading {release.tag_name}...";
            _source.InstallOfficialRelease(release, (message, ok) =>
            {
                _message = message;
                _signature = null;
                Evaluate();
            });
            Rebuild();
        }

        /// <summary>
        /// Creates the card with the local path field and the Browse and Open folder buttons.
        /// </summary>
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

        /// <summary>
        /// Creates the card with the three source options.
        /// </summary>
        private VisualElement OptionsCard()
        {
            var card = Css(new VisualElement(), "card");
            AddOption(card, SourceOption.PatchIntoAssets, "Patch into Assets",
                "Downloads the archive of the branch, tag or commit and merges it into 'Assets/xapi'. New files are added, identical files are skipped, " +
                "and files that differ from yours are kept next to them for review.");
            AddOption(card, SourceOption.DownloadZip, "Download zip",
                "Downloads the archive of the branch, tag or commit with your browser. Extract it and set the local path to the extracted folder.");
            AddOption(card, SourceOption.GitClone, LocalSourceView.ProjectHasGit ? "Git submodule" : "Git clone",
                LocalSourceView.ProjectHasGit
                    ? "Adds the repository as a git submodule next to the local path and checks out a new local branch at the branch, tag or commit. Needs git."
                    : "Clones the repository next to the local path and checks out a new local branch at the branch, tag or commit. Needs git.");
            return card;
        }

        /// <summary>
        /// Adds one source option. The toggles act as radio buttons.
        /// </summary>
        private void AddOption(VisualElement card, SourceOption option, string title, string description)
            => AddRadio(card, SelectedOption == option, title, description, () =>
            {
                SelectedOption = option;
                _message = null;
                Rebuild();
            });

        /// <summary>
        /// Adds a toggle with a description that acts as a radio button: it cannot be switched off, only another one switched on.
        /// </summary>
        private static void AddRadio(VisualElement card, bool selected, string title, string description, Action select)
        {
            var toggle = new Toggle { text = title, value = selected };
            toggle.RegisterValueChangedCallback(e =>
            {
                if (!e.newValue)
                {
                    toggle.SetValueWithoutNotify(true);
                    return;
                }
                select();
            });
            card.Add(Css(toggle, "option"));
            card.Add(Css(Muted(description), "option-hint"));
        }

        /// <summary>
        /// Creates the card with the settings and the action buttons of the selected source option.
        /// </summary>
        private VisualElement OptionDetailsCard()
        {
            var card = Css(new VisualElement(), "card");
            var source = Settings.localSource;
            var option = SelectedOption;

            const string refHint = "A branch, a tag or a commit hash. Leave it empty or write 'HEAD' for the default branch.";
            var refWarning = Css(Muted(""), "field");
            void UpdateRefWarning() => refWarning.text = source.IsValidGitRef ? "" : $"'{source.GitRef}' is not a valid branch, tag or commit.";

            if (option == SourceOption.GitClone)
            {
                card.Add(SettingField("Git URL", source.repoUrl, v => source.repoUrl = v));
                card.Add(SettingField("Branch, tag or commit", source.repoBranch, v =>
                {
                    source.repoBranch = v;
                    UpdateRefWarning();
                }));
                card.Add(Css(Muted(refHint + " The clone gets a new local branch that starts there."), "field"));
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
                card.Add(SettingField("Branch, tag or commit", source.repoBranch, v =>
                {
                    source.repoBranch = v;
                    UpdateArchive();
                    UpdateRefWarning();
                }));
                UpdateArchive();
                card.Add(Css(Muted(refHint), "field"));
                card.Add(Css(archive, "field"));
            }
            UpdateRefWarning();
            card.Add(refWarning);

            var reset = Css(new VisualElement(), "card-row", "field");
            reset.Add(Css(Muted($"Example: {DefaultValues.RepoDownloadUrl} ({DefaultValues.RepoBranch})."), "card-body"));
            reset.Add(Css(new Button(() =>
            {
                source.ResetRepository();
                Settings.Save();
                _message = null;
                Rebuild();
            }) { text = "Reset to defaults", tooltip = "Puts the repository URLs and the branch back to the example repository." }, "btn"));
            card.Add(reset);

            var actions = Css(new VisualElement(), "card-row", "actions");
            switch (option)
            {
                case SourceOption.PatchIntoAssets:
                    actions.Add(Css(new Button(() =>
                    {
                        _message = "Downloading... The definitions appear in 'Assets/xapi' once the download is done.";
                        Rebuild();
                        EditorApplication.delayCall += () => _source.DownloadToAssetsAndPatch((message, ok) =>
                        {
                            _message = message;
                            _signature = null;
                            Evaluate();
                        });
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

        /// <summary>
        /// Creates a text field that saves the settings on every change.
        /// </summary>
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

        /// <summary>
        /// Creates a card with one row per check.
        /// </summary>
        private VisualElement ChecksCard(params Check[] checks)
        {
            var card = Css(new VisualElement(), "card");
            foreach (var check in checks)
                card.Add(CheckRow(check));
            return card;
        }

        /// <summary>
        /// Creates the row of a check: icon, label, hint and fix button.
        /// </summary>
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

        /// <summary>
        /// Creates the card of an action result with its errors and warnings (at most eight, the rest in the Console).
        /// </summary>
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

        /// <summary>
        /// Creates a row with an icon, an optional title and a text.
        /// </summary>
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

        /// <summary>
        /// Creates a card of the Use page with a title, a description and buttons.
        /// </summary>
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

        /// <summary>
        /// Creates a card of the Welcome page's overview.
        /// </summary>
        private static VisualElement OverviewCard(string title, string text)
        {
            var card = Css(new VisualElement(), "card");
            card.Add(Css(new Label(title), "card-title"));
            card.Add(Css(Text(text), "field"));
            return card;
        }

        /// <summary>
        /// Creates a row of the Done page's summary.
        /// </summary>
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

        /// <summary>
        /// Runs an action after the current event, because refreshing the AssetDatabase inside a UI event can break the layout.
        /// </summary>
        private static Action Deferred(Action action) => () => EditorApplication.delayCall += () => action();

        /// <summary>
        /// Creates a section heading.
        /// </summary>
        private static Label H2(string text) => Css(new Label(text), "h2");
        /// <summary>
        /// Creates a paragraph.
        /// </summary>
        private static Label Text(string text) => Css(new Label(text), "text");
        /// <summary>
        /// Creates a dimmed hint text.
        /// </summary>
        private static Label Muted(string text) => Css(new Label(text), "muted");

        /// <summary>
        /// Creates a button that opens a URL.
        /// </summary>
        private static Button LinkButton(string text, string url)
            => Css(new Button(() => Application.OpenURL(url)) { text = text }, "btn", "link");

        /// <summary>
        /// Enables or disables an element and returns it.
        /// </summary>
        private static T Enabled<T>(T element, bool enabled) where T : VisualElement
        {
            element.SetEnabled(enabled);
            return element;
        }

        /// <summary>
        /// Adds USS classes to an element and returns it.
        /// </summary>
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
