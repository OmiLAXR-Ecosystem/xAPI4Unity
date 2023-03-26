using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EduXR.xAPI.Editor
{
    public class FetcherWindow : EditorWindow
    {
        private static Settings settings => Settings.instance;
        private bool _isDownloading = false;

        private readonly GitLabView _gitLabView = new GitLabView(settings);
        private readonly LocalSourceView _localSourceView = new LocalSourceView(settings);

        public bool IsTransformationPossible => GitHelper.IsValidRepo(settings.localSource.path) && 
                                                (settings.fetcher.selectedType == FetcherType.CommandLine && !string.IsNullOrEmpty(settings.fetcher.cliPath));
        
        private FetcherApiService FetcherService() => new FetcherApiService(settings.fetcher.selectedUrl);
        
        [MenuItem("EduXR / xAPI / Fetcher")]
        public static void ShowWindow()
        {
            GetWindow<FetcherWindow>("xAPI Fetcher").Show();
        }

        [MenuItem("EduXR / xAPI / Fetcher: Transform JSON => C#")]
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
            CustomGUI.Subtitle("xAPI JSON Definitions Location");
            
            switch (settings.fetcher.selectedType)
            {
                case FetcherType.WebService:
                    break;
                case FetcherType.CommandLine:
                    CustomGUI.Description(
                        "Here you can select where you want to fetch the xAPI definitions from. You can select between GitLab and Local.");

                    if (settings.fetcher.selectedType == FetcherType.CommandLine)
                        settings.fetcher.selectedSourceType = CustomGUI.Popup("Source type", settings.fetcher.selectedSourceType);
                    else
                        CustomGUI.Popup("Source type", SourceType.Local);

                    SourceType_View();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Fetcher_View()
        {
            CustomGUI.Subtitle("Select your tool");
            CustomGUI.Description(
                "You can choose between two fetcher tools. The web service can be used directly. The Command Line tool has to downloaded first but can be used for local repositories.");

            CustomGUI.BeginHorizontal();
           settings.fetcher.selectedType = CustomGUI.Popup("Fetcher type", settings.fetcher.selectedType);

          switch (settings.fetcher.selectedType)
           {
               case FetcherType.WebService:
                   break;
               case FetcherType.CommandLine:
                   /*if (CustomGUI.StateButton("Download Executable", _isDownloading, "Downloading..."))
                   {
                       _isDownloading = true;
                       EditorCoroutineUtility.StartCoroutineOwnerless(FetcherService().DownloadExe(DefaultValues.CliRoot, 
                           (_) =>
                           {
                               var zipPath = Path.Join(DefaultValues.CliRoot, FetcherApiService.ZipName);
                               var outPath = DefaultValues.CliRoot;

                               ZipFile.ExtractToDirectory(zipPath, outPath, Encoding.UTF8, true);
                               
                               File.Delete(zipPath);

                               var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
                               var cliPath = Path.Join(outPath, "xAPI_Definitions_Parser_CLI" + ext);
                               
                               settings.fetcher.cliPath = cliPath;
                               
                               _isDownloading = false;
                               EditorUtility.DisplayDialog("Download fetcher executable successfully",
                                   $"The executable was downloaded successfully at '{outPath}'. The 'executable path' was automatically set.",
                                   "OK");
                           }));
                   }*/

                   break;
               default:
                   throw new ArgumentOutOfRangeException();
           }

            CustomGUI.EndHorizontal();
            

            if (settings.fetcher.selectedType == FetcherType.CommandLine)
            {
                CustomGUI.BeginHorizontal();
                CustomGUI.BeginDisabledGroup(true);
                settings.fetcher.cliPath = CustomGUI.TextField("Executable path", settings.fetcher.cliPath, DefaultValues.CliPath);
                CustomGUI.EndDisabledGroup();
                if (CustomGUI.Button("Open in browser"))
                {
                    Process.Start(Path.GetDirectoryName(settings.fetcher.cliPath));
                }
                if (CustomGUI.Button("Browse executable"))
                {
                    settings.fetcher.cliPath =
                        EditorUtility.OpenFilePanel("Browse executable", "", DefaultValues.CliExeExtension);
                    GUI.FocusControl(null);
                }
                CustomGUI.EndHorizontal();
                CustomGUI.Description("Choose the path of the fetcher tool executable.");
            }
            else
            {
                CustomGUI.Warning("Not supported yet.");
                CustomGUI.BeginHorizontal();
                
                CustomGUI.BeginDisabledGroup(true);
                settings.fetcher.selectedUrl = CustomGUI.TextField("Service Url", settings.fetcher.selectedUrl, DefaultValues.FetcherAPIBase);
                CustomGUI.EndDisabledGroup();
                
                if (CustomGUI.Button("Open in browser"))
                {
                    Process.Start(settings.fetcher.selectedUrl);
                }

                CustomGUI.EndHorizontal();
                CustomGUI.Description("Choose the url of the xAPI Fetcher web service.");
            }

   
        }

        private void Fetcher_Args_View()
        {
            CustomGUI.Subtitle("Fetcher configuration");

            /*
            settings.@namespace = CustomGUI.TextField("Namespace", settings.@namespace.Replace(" ", "_").Trim(),
                DefaultValues.Namespace);

            CustomGUI.Space();

            CustomGUI.BeginDisabledGroup(true);
            settings.packageType = CustomGUI.Popup("Package Type", settings.packageType);
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.Space();
            */
            
            CustomGUI.BeginHorizontal();
            settings.fetcher.outputPath =
                CustomGUI.TextField("Result output path", settings.fetcher.outputPath, DefaultValues.OutputPath);
            if (CustomGUI.Button("Open in explorer"))
            {
                Process.Start(settings.fetcher.outputPath);
            }

            CustomGUI.EndHorizontal();
        }

        private void OnGUI()
        {
            Fetcher_View();

            CustomGUI.Divider();

            CustomGUI.BeginDisabledGroup(settings.fetcher.selectedType != FetcherType.CommandLine);

            xAPI_Source_View();
            
            CustomGUI.Space();

            Fetcher_Args_View();
            
            
            CustomGUI.Divider();
            
            PreviewView();
            
            CustomGUI.Divider();
            
            CustomGUI.BeginHorizontal();
            
            CustomGUI.BeginDisabledGroup(!Settings.IsDirty());
            if (CustomGUI.Button("Save settings"))
            {
                GUI.FocusControl(null);
                Settings.instance.Save();
            }
            CustomGUI.EndDisabledGroup();
            
            if (CustomGUI.Button("Reset to default"))
            {
                _gitLabView.Reset();
                Settings.instance.Reset();
                Settings.instance.Load();
            }
            
            CustomGUI.Space();

            CustomGUI.BeginDisabledGroup(!GitHelper.IsValidRepo(settings.localSource.path));
            if (CustomGUI.Button("Edit xAPI Definitions"))
            {
                DesignerWindow.ShowWindow();
            }
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.BeginDisabledGroup(!IsTransformationPossible);
            if (CustomGUI.Button("Transform JSON => C#"))
            {
                GUI.FocusControl(null);
                ExecuteFetcher();
            }
            CustomGUI.EndDisabledGroup();

            CustomGUI.EndHorizontal();
            CustomGUI.EndDisabledGroup();

        }

        private void PreviewView()
        {
            CustomGUI.Title("Transformation Preview");

            var arguments = GetArguments();

            var csharpLocation = SafePath(Path.Join(arguments["output"], arguments["namespace"]).Replace("\"", ""));
            
            CustomGUI.Label("JSON definitions files: " + arguments["path"]);
            CustomGUI.Label("Target language: " + arguments["lang"]);
            CustomGUI.Label("C# files location: " + csharpLocation);
            CustomGUI.Label("C# Namespace: " + arguments["namespace"]);
            CustomGUI.Label("Binary: DLL will be not created.");

            settings.fetcher.asmDef = CustomGUI.YesNoPopup("Create .asmdef file", settings.fetcher.asmDef);
            settings.fetcher.version = CustomGUI.TextField("Assign a version", settings.fetcher.version, DefaultValues.Version, GUILayout.MaxWidth(200));
            
            var cmdString = ComposeArguments();

            CustomGUI.BeginHorizontal();
            CustomGUI.BeginDisabledGroup(true);
            CustomGUI.TextField("Fetcher CLI Arguments", cmdString);
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.BeginDisabledGroup(cmdString == EditorGUIUtility.systemCopyBuffer);
            if (CustomGUI.Button("Copy to clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = cmdString;
            }
            CustomGUI.EndDisabledGroup();

            CustomGUI.EndHorizontal(); 
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
            => ProcessHelper.Cmd(SafePath(settings.fetcher.cliPath), ComposeArguments(), true);

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