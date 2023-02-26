using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EduXR.xAPI.Editor
{
    public static class CustomGUI
    {
        private static readonly GUIStyle TitleLabelStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 16,
        };

        private static readonly GUIStyle SubtitleLabelStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            fontSize = 14,
        };

        private static readonly GUIStyle ButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(5, 5, -5, 0)
        };

        private static readonly GUIStyle TextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true
        };

        public static bool Button(string text, params GUILayoutOption[] options)
        {
            var res = GUILayout.Button(text, ButtonStyle, (new[]
            {
                GUILayout.Height(30),
                GUILayout.ExpandWidth(false)
            }).Concat(options).ToArray());

            return res;
        }

        public static void Title(string text)
            => GUILayout.Label(text, TitleLabelStyle);

        public static void Subtitle(string text)
            => GUILayout.Label(text, SubtitleLabelStyle);

        public static void Label(string text)
            => GUILayout.Label(text);

        public static void Description(string text)
            => GUILayout.Label(text, TextStyle, GUILayout.ExpandWidth(true));

        public static void Space(int lines = 1)
        {
            for (var i = 0; i < lines; i++)
                EditorGUILayout.Space();
        }

        public static string Popup(string label, IEnumerable<string> options, string value)
        {
            var opts = new [] { "" };
            opts = opts.Concat(options).ToArray();

            if (string.IsNullOrEmpty(value))
                value = "";

            var optsIndex = Array.IndexOf(opts, value);
            if (optsIndex < 0)
                optsIndex = 0;
            
            return opts[EditorGUILayout.Popup(label, optsIndex, opts)];
        }

        public static T Popup<T>(string label, T value) where T : Enum
            => (T)EditorGUILayout.EnumPopup(label, value, GUILayout.MaxWidth(300));

        public static void Divider(int spaceLines = 1)
        {
            Space(spaceLines);
            const float height = 1.0f;
            var rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            Space(spaceLines);
        }

        public static string TextField(string label, string text, params GUILayoutOption[] options)
            => EditorGUILayout.TextField(label, text, (new[]
            {
                GUILayout.MinWidth(250)
            }).Concat(options).ToArray());

        public static void BeginHorizontal() => EditorGUILayout.BeginHorizontal();
        public static void EndHorizontal() => EditorGUILayout.EndHorizontal();
        public static void BeginDisabledGroup(bool conditions) => EditorGUI.BeginDisabledGroup(conditions);
        public static void EndDisabledGroup() => EditorGUI.EndDisabledGroup();
    }

    #region Enums
    public enum SourceType
    {
        GitLab,
        Local
    }
    
    public enum FetcherType
    {
        WebService,
        CommandLine
    }
    
    public enum PackageType
    {
        SimpleAsset,
        Binary,
        UnityPackage
    }
    #endregion

    public static class FetcherWindowDefaults
    {
        // public const string Fetcher_API_BASE = "https://localhost:5001/";
        public const string FetcherAPIBase = "https://fetch.xapi.elearn.rwth-aachen.de/";
        public const string Namespace = "EduXR.xAPI.Registry";
    }
    
    [Serializable]
    public class FetcherSettings
    {
        [SerializeField] private FetcherType selectedFetcherType = FetcherType.CommandLine;
        [SerializeField] private string selectedFetcherUrl = "";
        [SerializeField] private SourceType selectedSourceType = SourceType.Local;

        public void Fetcher_View()
        {
            CustomGUI.Subtitle("Select your tool");
            CustomGUI.Description(
                "You can choose between two fetcher tools. The web service can be used directly. The Command Line tool has to downloaded first but can be used for local repositories.");
            
            CustomGUI.BeginHorizontal();
            selectedFetcherType = CustomGUI.Popup("Fetcher type", selectedFetcherType);

            switch (selectedFetcherType)
            {
                case FetcherType.WebService:
                    break;
                case FetcherType.CommandLine:
                    if (CustomGUI.Button("Download Executable"))
                    {
                        
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            CustomGUI.EndHorizontal();
        }

        private void xAPI_Source_View()
        {
            CustomGUI.Subtitle("xAPI Source");

            switch (selectedFetcherType)
            {
                case FetcherType.WebService:
                    CustomGUI.Description("Choose the url of the xAPI Fetcher web service.");
                    CustomGUI.BeginHorizontal();
                    selectedFetcherUrl = CustomGUI.TextField("Service Url", selectedFetcherUrl);
                    if (CustomGUI.Button("Open"))
                    {
                        Process.Start(selectedFetcherUrl);
                    }
                    CustomGUI.EndHorizontal();
                    break;
                case FetcherType.CommandLine:
                    CustomGUI.Description(
                        "If you are using the source type 'Local' you need a local copy of the xAPI registry on your computer.");

                    if (selectedFetcherType == FetcherType.CommandLine)
                        selectedSourceType = CustomGUI.Popup("Source type", selectedSourceType);
                    else
                        CustomGUI.Popup("Source type", SourceType.Local);

                    SourceType_View();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
        public class GitLabSourceSettings
        {
            [SerializeField] public string repoUrl = "https://gitlab.com/learntech-rwth/xapi";
            [SerializeField] public string projectId = "";
            [SerializeField] public string privateKey = "";
            [SerializeField] public string branch = "main";
            
            private Dictionary<string, int> _projectIds = new Dictionary<string, int>();

            public string[] Branches { get; private set; } = Array.Empty<string>();

            public Uri BaseRepoUri => new Uri(repoUrl);
            public string BaseUri => BaseRepoUri.AbsoluteUri.Replace(BaseRepoUri.AbsolutePath, "");
            public string AccessTokenUri => Path.Join(BaseUri, "-/profile/personal_access_tokens");
            public string BranchesUri => Path.Join(BaseUri, $"api/v4/projects/{projectId}/repository/branches");
            
            public int? FetchProjectId() => FetchProjectId(repoUrl);
            public string[] FetchBranches() => FetchBranches(BranchesUri);

            public void AutoDetectProjectId()
            {
                var pid = FetchProjectId();
                if (!pid.HasValue) return;
                projectId = pid.Value.ToString();
                if (!_projectIds.ContainsKey(repoUrl))
                {
                    _projectIds.Add(repoUrl, pid.Value);
                }
            }

            /// <summary>
            /// Check if the typed repo url is matching with the project id.
            /// </summary>
            /// <returns>true, repo url is matching with the project id</returns>
            public bool IsProjectIdMatchingRepoUrl()
            {
                var allProjectIds = _projectIds.Values.Select(p => p.ToString()).ToArray();
                if (!allProjectIds.Contains(projectId) || !_projectIds.ContainsKey(repoUrl))
                    return false;
                var pid = _projectIds[repoUrl];
                return pid.ToString() == projectId;
            }
            
            private static int? FetchProjectId(string uri)
            {
                var response = EditorHttpHelper.Get(uri, "application/text");
                // Define a regular expression for repeated words.
                var rx = new Regex(@"\b(Project ID: [0-9]+)\b",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    
                // Find matches.
                var matches = rx.Matches(response);
                var match = matches.First().Value;
                var projectId = int.Parse(match.Replace("Project ID:", ""));

                // Report the number of matches found.
                Debug.Log("Project ID " + projectId);
                return projectId;
            }

            private static string[] FetchBranches(string uri)
            {
                var response = EditorHttpHelper.Get(uri, "application/json");
                var rx = new Regex("(\"name\":\"[0-9a-zA-Z-_]+\")", RegexOptions.Compiled | RegexOptions.Multiline);

                var matches = rx.Matches(response);
                return matches
                    .Select(m => m.Value.Replace("\"", "").Replace("name:", ""))
                    .ToArray(); 
            }

            public void AutoDetectBranches()
            {
                Branches = FetchBranches(BranchesUri);
            }
        }
        
        [Serializable]
        public class LocalSourceSettings
        {
            [SerializeField] public string path = "";
        }
    
    public class xAPI_Definitions_Fetcher_Window : EditorWindow
    {
        private delegate Dictionary<string, string> ArgumentsHandler();
        
        [SerializeReference] private FetcherSettings fetcher = new FetcherSettings();
        [SerializeReference] private LocalSourceSettings localSource = new LocalSourceSettings();
        [SerializeReference] private GitLabSourceSettings gitlabSource = new GitLabSourceSettings();
        private string _lastAppliedRepoUrl = "";

        [SerializeField] private string @namespace = FetcherWindowDefaults.Namespace;

        [SerializeField] private bool useShellWindow = false;

        [SerializeField] private string selectedFetcherUrl = "https://fetch.xapi.elearn.rwth-aachen.de";
        
        private Dictionary<string, string> SourceArguments =>
            (new Dictionary<SourceType, ArgumentsHandler>
            {
                [SourceType.GitLab] = GetGitlabArguments,
                [SourceType.Local] = GetLocalStorageArguments
            })[selectedSourceType]?.Invoke();

        [MenuItem("EduXR Tools/xAPI/Quick fetch definitions")]
        public static void QuickFetchDefinitions()
        {
        }

        [MenuItem("EduXR Tools/xAPI/Fetch custom definitions")]
        public static void ShowWindow()
        {
            GetWindow<xAPI_Definitions_Fetcher_Window>("xAPI Definitions Fetcher").Show();
        }
        
        private void ApplyGitLabRepo()
        {
            gitlabSource.AutoDetectProjectId();
            _lastAppliedRepoUrl = gitlabSource.repoUrl;
            
            gitlabSource.AutoDetectBranches();
        }

        private void GitLabSource_View()
        {
            CustomGUI.BeginHorizontal();
            gitlabSource.repoUrl = CustomGUI.TextField("GitLab Repository", gitlabSource.repoUrl);
            
            CustomGUI.BeginDisabledGroup(gitlabSource.repoUrl == _lastAppliedRepoUrl);
            if (CustomGUI.Button("Apply"))
            {
                ApplyGitLabRepo();
            }
            CustomGUI.EndDisabledGroup();

            CustomGUI.EndHorizontal();

            CustomGUI.BeginDisabledGroup(gitlabSource.repoUrl != _lastAppliedRepoUrl);
            
            CustomGUI.BeginHorizontal();
            gitlabSource.privateKey = CustomGUI.TextField("Private access token", gitlabSource.privateKey);
            if (CustomGUI.Button("Create one"))
            {
                Process.Start(gitlabSource.AccessTokenUri);
            }
            CustomGUI.EndHorizontal();
            
            CustomGUI.BeginHorizontal();
            gitlabSource.projectId = CustomGUI.TextField("Project ID", gitlabSource.projectId, GUILayout.MaxWidth(300));
            
            CustomGUI.BeginDisabledGroup(gitlabSource.IsProjectIdMatchingRepoUrl());
            if (CustomGUI.Button("Fetch Project ID from repo"))
            {
                gitlabSource.AutoDetectProjectId();
            }
            CustomGUI.EndDisabledGroup();

            CustomGUI.EndHorizontal();

            CustomGUI.BeginDisabledGroup(string.IsNullOrEmpty(gitlabSource.projectId));

            CustomGUI.BeginHorizontal();
            gitlabSource.branch = CustomGUI.Popup("Branch", gitlabSource.Branches,gitlabSource.branch);
            if (CustomGUI.Button("Refresh"))
            {
                gitlabSource.AutoDetectBranches();
            }
            CustomGUI.EndHorizontal();
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.EndDisabledGroup();
        }

        private void LocalSource_View()
        {
            CustomGUI.BeginHorizontal();
            localSource.path = CustomGUI.TextField("Local root path", localSource.path);
            if (CustomGUI.Button("Browse path"))
            {
                localPath = EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", localPath, "xapi");
                GUI.FocusControl(null);
            }

            CustomGUI.EndHorizontal();
        }

        private void SourceType_View()
        {
            switch (selectedSourceType)
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

        private void Fetcher_Args_View()
        {
            CustomGUI.Subtitle("Fetcher configuration");

            @namespace = CustomGUI.TextField("Namespace", @namespace).Replace(" ", "_");

            fetcherPath = CustomGUI.TextField("Executable path", fetcherPath);
            if (CustomGUI.Button("Browse executable"))
            {
                fetcherPath = EditorUtility.OpenFilePanel("Browse xAPI Definitions Fetcher", "", "exe");
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space();
            outputPath = CustomGUI.TextField("Result output path", outputPath);
            if (CustomGUI.Button("Set default output path"))
            {
                outputPath = DefaultOutputPath;
                GUI.FocusControl(null);
            }
        }

        private void OnGUI()
        {
            Fetcher_View();

            CustomGUI.Divider();

            xAPI_Source_View();

            CustomGUI.Divider();

            EditorGUILayout.Space();

            CustomGUI.Space(4);

            if (GUILayout.Button("Save settings", GUILayout.Width(100), GUILayout.Height(40)))
            {
                GUI.FocusControl(null);
                SaveSettings();
            }

            EditorGUILayout.Separator();

            CustomGUI.Space(2);

            //_sourceSelected = EditorGUILayout.Popup("Source", _sourceSelected, _sources);

            if (CustomGUI.Button("Reset to default"))
            {
                ResetSettings();
                LoadSettings();
            }

            if (CustomGUI.Button("Fetch xAPI Definitions"))
            {
                GUI.FocusControl(null);
                ExecuteFetcher();
            }
        }

        private Dictionary<string, string> GetArguments(Dictionary<string, string> args = null)
        {
            var arguments = new Dictionary<string, string>
            {
                ["source"] = selectedSourceType.ToString(),
                ["output"] = outputPath,
                ["namespace"] = @namespace
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
                ["projectId"] = gitlabSource.projectId,
                ["privateKey"] = gitlabSource.privateKey,
                ["branch"] = gitlabSource.branch
            };
        }

        private Dictionary<string, string> GetLocalStorageArguments()
        {
            return new Dictionary<string, string>
            {
                ["local"] = localPath
            };
        }

        private string[] PreparedArguments()
        {
            var list = new List<string>();

            var arguments = GetArguments(SourceArguments);

            foreach (var pair in arguments)
            {
                list.Add("-" + pair.Key);
                list.Add($"\"{pair.Value}\"");
            }

            return list.ToArray();
        }

        private void ExecuteFetcher()
        {
            var startInfo = new ProcessStartInfo(fetcherPath);

            // prepare arguments
            var preparedArguments = PreparedArguments();
            var arguments = string.Join(" ", preparedArguments);
            startInfo.Arguments = arguments;

            if (!useShellWindow)
            {
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
            }


            var process = new Process();
            process.StartInfo = startInfo;
            if (!useShellWindow)
            {
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
            }

            process.Start();

            if (!useShellWindow)
                process.BeginOutputReadLine();
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.LogError(e.Data);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.Log(e.Data);
        }

        protected void OnEnable() => LoadSettings();

        private void ResetSettings()
        {
            EditorPrefs.DeleteKey("EduXR.xAPI.Fetcher");
        }

        private void LoadSettings()
        {
            var data = EditorPrefs.GetString("EduXR.xAPI.Fetcher", JsonUtility.ToJson(this, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, this);

            gitlabSource ??= new GitLabSourceSettings();
            localSource ??= new LocalSourceSettings();

            if (outputPath == "")
                outputPath = DefaultOutputPath;
        }

        private void SaveSettings()
        {
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString("xAPI_Definitions_Fetcher_Window", data);
        }
    }
}