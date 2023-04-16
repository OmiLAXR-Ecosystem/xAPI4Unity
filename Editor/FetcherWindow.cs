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

namespace EduXR.xAPI.Editor
{
    public class FetcherWindow : EditorWindow
    {
        private static Settings settings => Settings.instance;
        private bool _isDownloading;

        public static FetcherWindow Instance { get; private set; }

        private readonly GitLabView _gitLabView = new GitLabView(settings);
        private readonly LocalSourceView _localSourceView = new LocalSourceView(settings);

        private static bool IsTransformationPossible => GitHelper.IsValidRepo(settings.localSource.path) &&
                                                        (settings.fetcher.selectedType == FetcherType.CommandLine &&
                                                         !string.IsNullOrEmpty(settings.fetcher.cliPath));

        private FetcherApiService FetcherService() => new FetcherApiService(settings.fetcher.selectedUrl);

        public event OnTriggeredControlHandler OnTriggeredControl;

        private FetcherWindow()
        {
            _gitLabView.OnTriggeredControl += FireTriggeredControl;
            _localSourceView.OnTriggeredControl += FireTriggeredControl;
        }

        private void FireTriggeredControl(Type type, string label, string value)
            => OnTriggeredControl?.Invoke(type, label, value);
        
        [MenuItem("EduXR / xAPI / Open Fetcher Window")]
        private static void ShowWindow()
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
            switch (settings.fetcher.selectedSourceType)
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

            switch (settings.fetcher.selectedType)
            {
                case FetcherType.WebService:
                    break;
                case FetcherType.CommandLine:
                    CustomGUI.Description(
                        "Here you can select where you want to fetch the xAPI definitions from. You can select between GitLab and Local.");

                    if (settings.fetcher.selectedType == FetcherType.CommandLine)
                    {
                        settings.fetcher.selectedSourceType =
                            CustomGUI.Popup("Source type", settings.fetcher.selectedSourceType, OnTriggeredControl);
                    } 
                    else
                        CustomGUI.Popup("Source type", SourceType.Local, OnTriggeredControl);

                    SourceType_View();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Fetcher_View()
        {
            CustomGUI.Subtitle("1) Select your tool");
            //CustomGUI.Description("You can choose between two fetcher tools. The web service can be used directly. The Command Line tool has to downloaded first but can be used for local repositories.");
            CustomGUI.Description(
                "Here you can choose whether you want to use the CLI or the WebService (currently not available). This tool is needed to transform JSON files to C#.");

            CustomGUI.Horizontal.Begin();
            settings.fetcher.selectedType = CustomGUI.Popup("Fetcher type", settings.fetcher.selectedType, OnTriggeredControl);

            switch (settings.fetcher.selectedType)
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

                                settings.fetcher.cliPath = cliPath;

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
            
            if (settings.fetcher.selectedType == FetcherType.CommandLine)
            {
                CustomGUI.Description(
                    "Choose the path of the fetcher tool executable. Go to step 2 if the path is correct.");

                CustomGUI.Horizontal.Begin();
                CustomGUI.BeginDisabledGroup(true);
                settings.fetcher.cliPath =
                    CustomGUI.TextField("Executable path", settings.fetcher.cliPath, DefaultValues.CliPath, OnTriggeredControl);
                CustomGUI.EndDisabledGroup();
                if (CustomGUI.Button("Open in explorer", OnTriggeredControl))
                {
                    Process.Start(Path.GetDirectoryName(settings.fetcher.cliPath)!);
                }

                GUI.SetNextControlName("BrowseCliButton");
                if (CustomGUI.Button("Browse executable", OnTriggeredControl))
                {
                    // todo changed path
                    settings.fetcher.cliPath =
                        EditorUtility.OpenFilePanel("Browse executable",
                            Path.GetDirectoryName(settings.fetcher.cliPath), DefaultValues.CliExeExtension);
                    GUI.FocusControl(null);
                }

                CustomGUI.Horizontal.End();
            }
            else
            {
                CustomGUI.Warning("Not supported yet.");
                CustomGUI.Horizontal.Begin();

                CustomGUI.BeginDisabledGroup(true);
                settings.fetcher.selectedUrl = CustomGUI.TextField("Service Url", settings.fetcher.selectedUrl,
                    DefaultValues.FetcherAPIBase, OnTriggeredControl);
                CustomGUI.EndDisabledGroup();

                if (CustomGUI.Button("Open in browser", OnTriggeredControl))
                {
                    Process.Start(settings.fetcher.selectedUrl);
                }

                CustomGUI.Horizontal.End();
                CustomGUI.Description("Choose the url of the xAPI Fetcher web service.");
            }
        }

        private void Fetcher_Args_View()
        {
            var csharpLocation =
                SafePath(Path.Join(settings.fetcher.outputPath, settings.@namespace).Replace("\"", ""));

            CustomGUI.Subtitle("3) Fetcher configuration");
            CustomGUI.Description(
                $"You can configure here where your transformed C# files should be placed. Currently your files will be placed at {csharpLocation}. Click on 'Browse folder' to change the path.");

            CustomGUI.Horizontal.Begin();

            CustomGUI.BeginDisabledGroup(true);
            settings.fetcher.outputPath =
                CustomGUI.TextField("C# files output path", settings.fetcher.outputPath, DefaultValues.OutputPath, OnTriggeredControl);
            CustomGUI.EndDisabledGroup();

            if (CustomGUI.Button("Open in explorer", OnTriggeredControl))
            {
                Process.Start(settings.fetcher.outputPath);
            }

            if (CustomGUI.Button("Browse folder", OnTriggeredControl))
            {
                // todo changed
                settings.fetcher.outputPath = EditorUtility.OpenFolderPanel("Browse output folder for C# files",
                    settings.fetcher.outputPath, "");
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

            CustomGUI.BeginDisabledGroup(true || !GitHelper.IsValidRepo(settings.localSource.path));
            if (CustomGUI.Button("Edit xAPI Definitions (coming soon)", OnTriggeredControl))
            {
                DesignerWindow.ShowWindow();
            }

            CustomGUI.EndDisabledGroup();

            CustomGUI.Horizontal.End();
            CustomGUI.EndDisabledGroup();

            CustomGUI.Divider();
            CustomGUI.Space(2);

            Fetcher_View();

            CustomGUI.Divider();

            CustomGUI.BeginDisabledGroup(settings.fetcher.selectedType != FetcherType.CommandLine);

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

            settings.fetcher.asmDef =
                CustomGUI.YesNoPopup("Create .asmdef file", settings.fetcher.asmDef, "Yes (recommended)");
            settings.fetcher.version = CustomGUI.TextField("Assign a version", settings.fetcher.version,
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

            if (settings.fetcher.selectedSourceType == SourceType.Local)
            {
                arguments["path"] = SafePath(settings.localSource.path);
            }
            else
            {
                arguments["projectId"] = settings.gitlabSource.projectId;
                arguments["privateKey"] = settings.gitlabSource.accessKey;
                arguments["branch"] = settings.gitlabSource.branch;
            }

            arguments["source"] = settings.fetcher.selectedSourceType.ToString();
            arguments["output"] = SafePath(settings.fetcher.outputPath);
            arguments["namespace"] = settings.@namespace;
            arguments["asmDef"] = settings.fetcher.asmDef ? "yes" : "no";
            arguments["version"] = settings.fetcher.version;
            arguments["sdk"] = "dotnet4.8";

            // Further settings
            arguments["binary"] = "no";
            arguments["lang"] = "CSharp";

            return arguments;
        }


        private static string ComposeArguments() => string.Join(' ', GetArguments().Select(a => $"-{a.Key} {a.Value}"));

        public static void ExecuteFetcher()
        {
            var p = Path.Combine(settings.fetcher.outputPath, settings.@namespace);
            var f = Path.Combine(p, settings.AsmDefMetaFilename);

            // clear dir
            /*if (Directory.Exists(p))
                Directory.Delete(p, true);

            if (File.Exists(f))
                File.Delete(f);*/

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
               ProcessHelper.AllowExecute(settings.fetcher.cliPath);
            
            ProcessHelper.Cmd(SafePath(settings.fetcher.cliPath), ComposeArguments(), true);

            File.WriteAllText(f, DefaultValues.DefaultAsmDefMeta);
            
            AssetDatabase.Refresh();
        }

        private static string SafePath(string path) => $"\"{path}\"";

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.LogError(e.Data);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.Log(e.Data);
        }

        #region Load, Save & Reset Settings

        protected void OnEnable() => Settings.instance.Load();

        #endregion
    }
}