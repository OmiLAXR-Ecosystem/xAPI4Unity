using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EduXR.xAPI.Editor
{
    public class xAPI_Fetcher_Window : EditorWindow
    {
        private Settings settings => Settings.instance;
        private string _lastAppliedRepoUrl = "";
        private bool _isDownloading = false;

        private FetcherApiService FetcherService() => new FetcherApiService(settings.fetcher.selectedUrl);

        //[MenuItem("EduXR Tools/xAPI/Quick fetch definitions")]
        public static void QuickFetchDefinitions()
        {
        }

        [MenuItem("EduXR Tools/xAPI/Fetcher")]
        public static void ShowWindow()
        {
            GetWindow<xAPI_Fetcher_Window>("xAPI Fetcher").Show();
        }

        private void ApplyGitLabRepo()
        {
            settings.gitlabSource.AutoDetectProjectId();
            _lastAppliedRepoUrl = settings.gitlabSource.repoUrl;

            settings.gitlabSource.AutoDetectBranches();
        }

        private void GitLabSource_View()
        {
            CustomGUI.BeginHorizontal();
            settings.gitlabSource.repoUrl = CustomGUI.TextField("GitLab Repository", settings.gitlabSource.repoUrl);

            CustomGUI.BeginDisabledGroup(settings.gitlabSource.repoUrl == _lastAppliedRepoUrl);
            if (CustomGUI.Button("Apply"))
            {
                ApplyGitLabRepo();
            }

            CustomGUI.EndDisabledGroup();

            CustomGUI.EndHorizontal();

            CustomGUI.BeginDisabledGroup(settings.gitlabSource.repoUrl != _lastAppliedRepoUrl);

            CustomGUI.BeginHorizontal();
            settings.gitlabSource.accessKey = CustomGUI.TextField("Access token", settings.gitlabSource.accessKey);
            if (CustomGUI.Button("Create one"))
            {
                Process.Start(settings.gitlabSource.AccessTokenUri);
            }

            CustomGUI.EndHorizontal();

            CustomGUI.BeginHorizontal();
            settings.gitlabSource.projectId =
                CustomGUI.TextField("Project ID", settings.gitlabSource.projectId, "", GUILayout.MaxWidth(300));

            CustomGUI.BeginDisabledGroup(settings.gitlabSource.IsProjectIdMatchingRepoUrl());
            if (CustomGUI.Button("Fetch Project ID from repo"))
            {
                settings.gitlabSource.AutoDetectProjectId();
            }

            CustomGUI.EndDisabledGroup();

            CustomGUI.EndHorizontal();

            CustomGUI.BeginDisabledGroup(string.IsNullOrEmpty(settings.gitlabSource.projectId));

            CustomGUI.BeginHorizontal();
            settings.gitlabSource.branch = CustomGUI.Popup("Branch", settings.gitlabSource.Branches, settings.gitlabSource.branch);
            if (CustomGUI.Button("Refresh"))
            {
                settings.gitlabSource.AutoDetectBranches();
            }

            CustomGUI.EndHorizontal();
            CustomGUI.EndDisabledGroup();

            CustomGUI.EndDisabledGroup();
        }

        private void LocalSource_View()
        {
            CustomGUI.BeginHorizontal();
            settings.localSource.path = CustomGUI.TextField("Path to xapi repository", settings.localSource.path);
            if (CustomGUI.Button("Browse path"))
            {
                settings.localSource.path =
                    EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", settings.fetcher.localPath, "xapi");
                GUI.FocusControl(null);
            }

            CustomGUI.EndHorizontal();
        }

        private void SourceType_View()
        {
            switch (settings.fetcher.selectedSourceType)
            {
                case SourceType.Local:
                    LocalSource_View();
                    break;
                case SourceType.GitLab:
                    GitLabSource_View();
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
                        "If you are using the source type 'Local' you need a local copy of the xAPI registry on your computer.");

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
                    if (CustomGUI.StateButton("Download Executable", _isDownloading, "Downloading..."))
                    {
                        _isDownloading = true;
                        EditorCoroutineUtility.StartCoroutineOwnerless(FetcherService().DownloadExe(DefaultValues.ExeRoot, 
                            (_) =>
                            {
                                var zipPath = Path.Join(DefaultValues.ExeRoot, FetcherApiService.ZipName);
                                var outPath = DefaultValues.ExeRoot;

                                ZipFile.ExtractToDirectory(zipPath, outPath, Encoding.UTF8, true);
                                
                                File.Delete(zipPath);

                                var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
                                var exePath = Path.Join(outPath, "xAPI_Definitions_Parser_CLI" + ext);
                                
                                settings.fetcher.exePath = exePath;
                                
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

            CustomGUI.EndHorizontal();

            if (settings.fetcher.selectedType == FetcherType.CommandLine)
            {
                CustomGUI.BeginHorizontal();
                CustomGUI.BeginDisabledGroup(true);
                settings.fetcher.exePath = CustomGUI.TextField("Executable path", settings.fetcher.exePath, DefaultValues.ExePath);
                CustomGUI.EndDisabledGroup();
                if (CustomGUI.Button("Browse executable"))
                {
                    settings.fetcher.exePath =
                        EditorUtility.OpenFilePanel("Browse executable", "", DefaultValues.ExeExtension);
                    GUI.FocusControl(null);
                }
                CustomGUI.EndHorizontal();
                CustomGUI.Description("Choose the path of the fetcher tool executable.");
            }
            else
            {
                CustomGUI.BeginHorizontal();
                settings.fetcher.selectedUrl = CustomGUI.TextField("Service Url", settings.fetcher.selectedUrl, DefaultValues.FetcherAPIBase);
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

            settings.@namespace = CustomGUI.TextField("Namespace", settings.@namespace.Replace(" ", "_").Trim(),
                DefaultValues.Namespace);

            CustomGUI.Space();

            CustomGUI.BeginDisabledGroup(true);
            settings.packageType = CustomGUI.Popup("Package Type", settings.packageType);
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.Space();

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
            
            if (settings.fetcher.selectedType == FetcherType.CommandLine) {
                xAPI_Source_View();
                CustomGUI.Divider();
            }
            
            CustomGUI.Space();

            Fetcher_Args_View();
            
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
                _lastAppliedRepoUrl = string.Empty;
                Settings.instance.Reset();
                Settings.instance.Load();
            }
            
            CustomGUI.Space();

            if (CustomGUI.Button("Transform JSON => C#"))
            {
                GUI.FocusControl(null);
                ExecuteFetcher();
            }
            
            CustomGUI.EndHorizontal();
        }

        private Dictionary<string, string> GetArguments(Dictionary<string, string> args = null)
        {
            var arguments = new Dictionary<string, string>
            {
                ["source"] = settings.fetcher.selectedType.ToString(),
                ["output"] = settings.fetcher.outputPath,
                ["namespace"] = settings.@namespace
            };

            if (args == null) return arguments;
            foreach (var a in args)
            {
                arguments.Add(a.Key, a.Value);
            }

            return arguments;
        }

        private Dictionary<string, string> GetGitlabArguments()
        {
            return new Dictionary<string, string>
            {
                ["projectId"] = settings.gitlabSource.projectId,
                ["privateKey"] = settings.gitlabSource.accessKey,
                ["branch"] = settings.gitlabSource.branch
            };
        }

        private Dictionary<string, string> GetLocalStorageArguments()
        {
            return new Dictionary<string, string>
            {
                ["local"] = settings.fetcher.localPath
            };
        }

        private string[] PreparedArguments()
        {
            var list = new List<string>();

            // todo
            var arguments = new Dictionary<string, string>();

            foreach (var pair in arguments)
            {
                list.Add("-" + pair.Key);
                list.Add($"\"{pair.Value}\"");
            }

            return list.ToArray();
        }

        private void FetchFromWebService()
        {
            
        }

        private void ExecuteFetcher()
        {
            // prepare arguments
            var preparedArguments = PreparedArguments();
            var arguments = string.Join(" ", preparedArguments);

            var startInfo = new ProcessStartInfo(settings.fetcher.exePath)
            {
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var process = new Process()
            {
                StartInfo = startInfo
            };
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            process.BeginOutputReadLine();
            process.Start();
        }

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