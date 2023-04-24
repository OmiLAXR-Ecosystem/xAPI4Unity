using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    public class FetcherWindow : EditorWindow
    {
        private static Settings Settings => Settings.instance;
        private bool _isDownloading;
        
        private Vector2 _scrollPosition = Vector2.zero;

        public static FetcherWindow Instance { get; private set; }

        private readonly GitLabView _gitLabView = new GitLabView(Settings);
        private readonly LocalSourceView _localSourceView = new LocalSourceView(Settings);

        private static bool IsTransformationPossible => GitHelper.IsValidRepo(Settings.localSource.path) &&
                                                        (Settings.fetcher.selectedType == FetcherType.CommandLine &&
                                                         !string.IsNullOrEmpty(Settings.fetcher.cliPath));

        private static FetcherApiService FetcherService() => new FetcherApiService(Settings.fetcher.selectedUrl);

        public event OnTriggeredControlHandler OnTriggeredControl;

        private FetcherWindow()
        {
            _gitLabView.OnTriggeredControl += FireTriggeredControl;
            _localSourceView.OnTriggeredControl += FireTriggeredControl;
        }

        private void FireTriggeredControl(Type type, string label, string value)
            => OnTriggeredControl?.Invoke(type, label, value);
        
        [MenuItem("EduXR / xAPI / Open Fetcher Window")]
        public static void ShowWindow()
        {
#if UNITY_STANDALONE
            Instance = GetWindow<FetcherWindow>("xAPI Fetcher");
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
            Instance.titleContent = new GUIContent("xAPI Fetcher");
#else
            _window.title = "xAPI Fetcher";
#endif
            Instance.minSize = new Vector2(1000, 770);
#endif
            Instance.Show(true);
        }

        [MenuItem("EduXR / xAPI / Quick transform JSON => C#")]
        public static void QuickFetch()
            => ExecuteFetcher();

        private void SourceType_View()
        {
            switch (Settings.fetcher.selectedSourceType)
            {
                case SourceType.Local:
                    _localSourceView.Show();
                    break;
                case SourceType.GitLab:
                    _gitLabView.Show();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void xAPI_Source_View()
        {
            CustomGUI.Subtitle("2) xAPI JSON Definitions Location");

            switch (Settings.fetcher.selectedType)
            {
                case FetcherType.WebService:
                    break;
                case FetcherType.CommandLine:
                    CustomGUI.Description(
                        "Here you can select where you want to fetch the xAPI definitions from. You can select between GitLab and Local.");

                    if (Settings.fetcher.selectedType == FetcherType.CommandLine)
                    {
                        Settings.fetcher.selectedSourceType =
                            CustomGUI.Popup("Source type", Settings.fetcher.selectedSourceType, OnTriggeredControl);
                    } 
                    else
                        CustomGUI.Popup("Source type", SourceType.Local, OnTriggeredControl);

                    SourceType_View();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Fetcher_View()
        {
            CustomGUI.Subtitle("1) Select your tool");
            //CustomGUI.Description("You can choose between two fetcher tools. The web service can be used directly. The Command Line tool has to downloaded first but can be used for local repositories.");
            CustomGUI.Description(
                "Here you can choose whether you want to use the CLI or the WebService (currently not available). This tool is needed to transform JSON files to C#.");

            CustomGUI.Horizontal.Begin();
            Settings.fetcher.selectedType = CustomGUI.Popup("Fetcher type", Settings.fetcher.selectedType, OnTriggeredControl);

            switch (Settings.fetcher.selectedType)
            {
                case FetcherType.WebService:
                    break;
                case FetcherType.CommandLine:
                    if (CustomGUI.StateButton("Download Executable", _isDownloading, "Downloading...", OnTriggeredControl))
                    {
                        _isDownloading = true;
                        EditorCoroutineUtility.StartCoroutineOwnerless(FetcherService().DownloadExe(
                            DefaultValues.CliRoot,
                            (_) =>
                            {
                                var zipPath = Path.Join(DefaultValues.CliRoot, FetcherApiService.ZipName);
                                var outPath = DefaultValues.CliRoot;

                                ZipFile.ExtractToDirectory(zipPath, outPath, Encoding.UTF8, true);

                                File.Delete(zipPath);

                                var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
                                var cliPath = Path.Join(outPath, "xAPI_Definitions_Fetcher.CLI" + ext);

                                Settings.fetcher.cliPath = cliPath;

                                _isDownloading = false;
                                
                                EditorUtility.DisplayDialog("Download fetcher executable successfully",
                                    $"The executable was downloaded successfully at '{outPath}'. The 'executable path' was automatically set.",
                                    "OK");
                            }));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CustomGUI.Horizontal.End();
            
            if (Settings.fetcher.selectedType == FetcherType.CommandLine)
            {
                CustomGUI.Description(
                    "Choose the path of the fetcher tool executable. Go to step 2 if the path is correct.");

                CustomGUI.Horizontal.Begin();
                CustomGUI.BeginDisabledGroup(true);
                Settings.fetcher.cliPath =
                    CustomGUI.TextField("Executable path", Settings.fetcher.cliPath, DefaultValues.CliPath, OnTriggeredControl);
                CustomGUI.EndDisabledGroup();
                if (CustomGUI.Button("Open in explorer", OnTriggeredControl))
                {
                    Process.Start(Path.GetDirectoryName(Settings.fetcher.cliPath)!);
                }

                GUI.SetNextControlName("BrowseCliButton");
                if (CustomGUI.Button("Browse executable", OnTriggeredControl))
                {
                    // todo changed path
                    Settings.fetcher.cliPath =
                        EditorUtility.OpenFilePanel("Browse executable",
                            Path.GetDirectoryName(Settings.fetcher.cliPath), DefaultValues.CliExeExtension);
                    GUI.FocusControl(null);
                }

                CustomGUI.Horizontal.End();
            }
            else
            {
                CustomGUI.Warning("Not supported yet.");
                CustomGUI.Horizontal.Begin();

                CustomGUI.BeginDisabledGroup(true);
                Settings.fetcher.selectedUrl = CustomGUI.TextField("Service Url", Settings.fetcher.selectedUrl,
                    DefaultValues.FetcherAPIBase, OnTriggeredControl);
                CustomGUI.EndDisabledGroup();

                if (CustomGUI.Button("Open in browser", OnTriggeredControl))
                {
                    Process.Start(Settings.fetcher.selectedUrl);
                }

                CustomGUI.Horizontal.End();
                CustomGUI.Description("Choose the url of the xAPI Fetcher web service.");
            }
        }

        private void Fetcher_Args_View()
        {
            var csharpLocation =
                SafePath(Path.Join(Settings.fetcher.outputPath, Settings.@namespace).Replace("\"", ""));

            CustomGUI.Subtitle("3) Fetcher configuration");
            CustomGUI.Description(
                $"You can configure here where your transformed C# files should be placed. Currently your files will be placed at {csharpLocation}. Click on 'Browse folder' to change the path.");

            CustomGUI.Horizontal.Begin();

            CustomGUI.BeginDisabledGroup(true);
            Settings.fetcher.outputPath =
                CustomGUI.TextField("C# files output path", Settings.fetcher.outputPath, DefaultValues.OutputPath, OnTriggeredControl);
            CustomGUI.EndDisabledGroup();

            if (CustomGUI.Button("Open in explorer", OnTriggeredControl))
            {
                Process.Start(Settings.fetcher.outputPath);
            }

            if (CustomGUI.Button("Browse folder", OnTriggeredControl))
            {
                // todo changed
                Settings.fetcher.outputPath = EditorUtility.OpenFolderPanel("Browse output folder for C# files",
                    Settings.fetcher.outputPath, "");
                GUI.FocusControl(null);
            }

            CustomGUI.Horizontal.End();
        }

        private void OnGUI()
        {
            CustomGUI.Title("xAPI Definitions Fetcher");

            CustomGUI.Horizontal.Begin();

            CustomGUI.BeginDisabledGroup(!Settings.IsDirty());
            
            if (CustomGUI.Button("Save settings", OnTriggeredControl))
            {
                GUI.FocusControl(null);
                Settings.instance.Save();
            }

            CustomGUI.EndDisabledGroup();

            if (CustomGUI.Button("Reset to default", OnTriggeredControl))
            {
                _gitLabView.Reset();
                Settings.instance.Reset();
                Settings.instance.Load();
            }

            CustomGUI.Space();

            CustomGUI.BeginDisabledGroup(!GitHelper.IsValidRepo(Settings.localSource.path));
            if (CustomGUI.Button("Edit and Create xAPI Definitions", OnTriggeredControl))
            {
                DesignerWindow.ShowWindow();
            }
            CustomGUI.EndDisabledGroup();

            CustomGUI.Horizontal.End();
            CustomGUI.EndDisabledGroup();

            CustomGUI.Divider();
            CustomGUI.Space(2);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            Fetcher_View();

            CustomGUI.Divider();

            CustomGUI.BeginDisabledGroup(Settings.fetcher.selectedType != FetcherType.CommandLine);

            xAPI_Source_View();

            CustomGUI.Space();

            Fetcher_Args_View();


            CustomGUI.Divider();

            PreviewView();

            CustomGUI.Divider();

            CustomGUI.Subtitle("5) Do transformation!");

            CustomGUI.Horizontal.Begin();

            CustomGUI.BeginDisabledGroup(!IsTransformationPossible);
            if (CustomGUI.Button("Transform JSON => C#", OnTriggeredControl, 
                    GUILayout.MaxWidth(1000), 
                    GUILayout.Height(50), 
                    GUILayout.ExpandWidth(true)))
            {
                GUI.FocusControl(null);
                ExecuteFetcher();
            }

            CustomGUI.EndDisabledGroup();

            CustomGUI.Horizontal.End();
            CustomGUI.EndDisabledGroup();
            GUILayout.EndScrollView();
        }

        private void PreviewView()
        {
            CustomGUI.Subtitle("4) Check your settings");

            var arguments = GetArguments();

            var csharpLocation = SafePath(Path.Join(arguments["output"], arguments["namespace"]).Replace("\"", ""));

            if (arguments.TryGetValue("path", out var argument))
                CustomGUI.Label("JSON definitions files: " + argument);
            
            CustomGUI.Label("Target language: " + arguments["lang"]);
            CustomGUI.Label("C# files location: " + csharpLocation);
            CustomGUI.Label("C# Namespace: " + arguments["namespace"]);
            CustomGUI.Label("Binary: DLL will be not created.");

            Settings.fetcher.asmDef =
                CustomGUI.YesNoPopup("Create .asmdef file", Settings.fetcher.asmDef, "Yes (recommended)");
            Settings.fetcher.version = CustomGUI.TextField("Assign a version", Settings.fetcher.version,
                DefaultValues.Version, OnTriggeredControl, GUILayout.MaxWidth(200));

            var cmdString = ComposeArguments();

            CustomGUI.Horizontal.Begin();
            CustomGUI.BeginDisabledGroup(true);
            CustomGUI.TextField("Fetcher CLI Arguments", cmdString, default, OnTriggeredControl);
            CustomGUI.EndDisabledGroup();

            CustomGUI.BeginDisabledGroup(cmdString == EditorGUIUtility.systemCopyBuffer);
            if (CustomGUI.Button("Copy to clipboard", OnTriggeredControl))
            {
                EditorGUIUtility.systemCopyBuffer = cmdString;
            }

            CustomGUI.EndDisabledGroup();

            CustomGUI.Horizontal.End();
        }

        private static Dictionary<string, string> GetArguments()
        {
            var arguments = new Dictionary<string, string>();

            if (Settings.fetcher.selectedSourceType == SourceType.Local)
            {
                arguments["path"] = SafePath(Settings.localSource.path);
            }
            else
            {
                arguments["projectId"] = Settings.gitlabSource.projectId;
                arguments["privateKey"] = Settings.gitlabSource.accessKey;
                arguments["branch"] = Settings.gitlabSource.branch;
            }

            arguments["source"] = Settings.fetcher.selectedSourceType.ToString();
            arguments["output"] = SafePath(Settings.fetcher.outputPath);
            arguments["namespace"] = Settings.@namespace;
            arguments["asmDef"] = Settings.fetcher.asmDef ? "yes" : "no";
            arguments["version"] = Settings.fetcher.version;
            arguments["sdk"] = "dotnet4.8";

            // Further settings
            arguments["binary"] = "no";
            arguments["lang"] = "CSharp";

            return arguments;
        }

        private void OnDisable()
        {
            // Ask the user if they really want to close the window
            var wantsSave = EditorUtility.DisplayDialog("Saving your settings?", "Do you want to save your settings before closing this window?", "Yes", "No");
        
            if (wantsSave)
                Settings.instance.Save();
        }

        private static string ComposeArguments() => string.Join(' ', GetArguments().Select(a => $"-{a.Key} {a.Value}"));

        /// <summary>
        /// Triggers to run the CLI command to transforms JSON to C#.
        /// </summary>
        public static void ExecuteFetcher()
        {
            var p = Path.Combine(Settings.fetcher.outputPath, Settings.@namespace);
            var f = Path.Combine(p, Settings.AsmDefMetaFilename);

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
               ProcessHelper.AllowExecute(Settings.fetcher.cliPath);
            
            ProcessHelper.Cmd(SafePath(Settings.fetcher.cliPath), ComposeArguments(), true);

            File.WriteAllText(f, DefaultValues.DefaultAsmDefMeta);
            
            AssetDatabase.Refresh();
        }

        private static string SafePath(string path) => $"\"{path}\"";

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.LogError(e.Data);

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.Log(e.Data);

        protected void OnEnable() => Settings.instance.Load();
    }
}