using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            var opts = new[] { "" };
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

        public static string TextField(string label, string text, string defaultText = "",
            params GUILayoutOption[] options)
            => EditorGUILayout.TextField(label, string.IsNullOrEmpty(text) ? defaultText : text, (new[]
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

    public static class DefaultFetcherValues
    {
        public const string RepoUrl = "https://gitlab.com/learntech-rwth/xapi";
        //public const string FetcherAPIBase = "https://fetch.xapi.elearn.rwth-aachen.de/";
        public const string FetcherAPIBase = "http://localhost:8083/";
        public const FetcherType FetcherType = Editor.FetcherType.WebService;
        public const string Namespace = "EduXR.xAPI.Registry";
        public static string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public const PackageType PackageType = Editor.PackageType.SimpleAsset;
        public const SourceType SourceType = Editor.SourceType.Local;

        public const string GitLabBranch = "main";
        public const string GitLabProjectId = "";
        public const string GitLabAccessKey = "";

        public static string ExeExtension => Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX
            ? ""
            : "exe";

        public static string ProjectRoot => Directory.GetCurrentDirectory();
        public static string AssetRoot => Path.Join(ProjectRoot, "Assets");
        public static string ToolsRoot => Path.Join(ProjectRoot, "Tools");
        public static string ExeRoot => Path.Join(ToolsRoot, "xAPI_Fetcher");
        public static string ExePath => Path.Join(ExeRoot, "xAPI_Definitions_Fetcher" + ExeExtension);

        public static string OutputPath => AssetRoot;

        public static void EnsureFolders()
        {
            if (!Directory.Exists(ToolsRoot))
                Directory.CreateDirectory(ToolsRoot);
            if (!Directory.Exists(ExeRoot))
                Directory.CreateDirectory(ExeRoot);
        }
    }

    [Serializable]
    public class FetcherSettings
    {
        public class FetcherArguments : Dictionary<string, string>
        {
            public FetcherArguments From(Dictionary<string, string> dict)
            {
                var rtn = new FetcherArguments();
                rtn.AddRange(dict);
                return rtn;
            }
        }

        [SerializeField] public FetcherType selectedType = DefaultFetcherValues.FetcherType;
        [SerializeField] public string selectedUrl = DefaultFetcherValues.FetcherAPIBase;
        [SerializeField] public SourceType selectedSourceType = DefaultFetcherValues.SourceType;
        [SerializeField] public string localPath = DefaultFetcherValues.HomeDirectory;
        [SerializeField] public string exePath = DefaultFetcherValues.ExePath;
        [SerializeField] public string outputPath = DefaultFetcherValues.OutputPath;
    }

    [Serializable]
    public class GitLabSourceSettings
    {
        [SerializeField] public string repoUrl = DefaultFetcherValues.RepoUrl;
        [SerializeField] public string projectId = DefaultFetcherValues.GitLabProjectId;
        [SerializeField] public string accessKey = DefaultFetcherValues.GitLabAccessKey;
        [SerializeField] public string branch = DefaultFetcherValues.GitLabBranch;

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
            var response = EditorHttpHelper.Get(uri);
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

    internal interface IObservable
    {
        void RememberStartValue();
        bool IsDirty();
        void ResetToDefault();
    }

    public delegate T ObservableValueHandler<T>();
    public class Observable<T> : IObservable where T : IComparable
    {
        private T _startValue;
        private readonly T _defaultValue;

        public ObservableValueHandler<T> GetValue;

        public Observable<T> SetValue(T value)
        {
            GetValue = () => value;
            return this;
        }

        public Observable(T defaultValue)
        {
            _defaultValue = defaultValue;
            GetValue = () => _defaultValue;
        }

        public void RememberStartValue() => _startValue = GetValue();
        public bool IsDirty() => _startValue.CompareTo(GetValue()) != 0;

        public void ResetToDefault()
        {
            _startValue = _defaultValue;
            GetValue = () => _defaultValue;
        }
    }

    [Serializable]
    public class FetcherWindowSettings
    {
        private List<IObservable> _observables = new List<IObservable>();

        [SerializeReference] public FetcherSettings fetcher = new FetcherSettings();
        [SerializeReference] public LocalSourceSettings localSource = new LocalSourceSettings();
        [SerializeReference] public GitLabSourceSettings gitlabSource = new GitLabSourceSettings();
        
        [SerializeReference] public string @namespace = DefaultFetcherValues.Namespace;
        [SerializeReference] public PackageType packageType = DefaultFetcherValues.PackageType;

        public void RememberValues()
        {
            _observables.Clear();
            _observables.Add(new Observable<string>(DefaultFetcherValues.Namespace).SetValue(@namespace));
            _observables.Add(new Observable<PackageType>(DefaultFetcherValues.PackageType).SetValue(packageType));
            _observables.ForEach(o => o.RememberStartValue());
        }
        public bool IsDirty() => _observables.Any(o => o.IsDirty());
        public void ResetToDefault() => _observables.ForEach(o => o.ResetToDefault());
    }

    public class xAPI_Definitions_Fetcher_Window : EditorWindow
    {
        [SerializeReference] private FetcherWindowSettings settings = new FetcherWindowSettings();
        private string _lastAppliedRepoUrl = "";

        private FetcherApiService FetcherService() => new FetcherApiService(settings.fetcher.selectedUrl);

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
            settings.localSource.path = CustomGUI.TextField("Local root path", settings.localSource.path);
            if (CustomGUI.Button("Browse path"))
            {
                settings.fetcher.localPath =
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
            CustomGUI.Subtitle("xAPI Source");

            switch (settings.fetcher.selectedType)
            {
                case FetcherType.WebService:
                    CustomGUI.Description("Choose the url of the xAPI Fetcher web service.");
                    CustomGUI.BeginHorizontal();
                    settings.fetcher.selectedUrl = CustomGUI.TextField("Service Url", settings.fetcher.selectedUrl, DefaultFetcherValues.FetcherAPIBase);
                    if (CustomGUI.Button("Open in browser"))
                    {
                        Process.Start(settings.fetcher.selectedUrl);
                    }

                    CustomGUI.EndHorizontal();
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
                    if (CustomGUI.Button("Download Executable"))
                    {
                        FetcherService().DownloadExe(DefaultFetcherValues.ExeRoot, (_, _) =>
                        {
                            var zipPath = Path.Join(DefaultFetcherValues.ExeRoot, FetcherApiService.ZipName);
                            var outPath = Path.Join(DefaultFetcherValues.ExeRoot, "xapi-fetcher");
                            ZipFile.ExtractToDirectory(zipPath, outPath);
                        });
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CustomGUI.EndHorizontal();

            CustomGUI.BeginHorizontal();
            settings.fetcher.exePath = CustomGUI.TextField("Executable path", settings.fetcher.exePath, DefaultFetcherValues.ExePath);
            if (CustomGUI.Button("Browse executable"))
            {
                settings.fetcher.exePath =
                    EditorUtility.OpenFilePanel("Browse executable", "", DefaultFetcherValues.ExeExtension);
                GUI.FocusControl(null);
            }

            CustomGUI.EndHorizontal();
        }

        private void Fetcher_Args_View()
        {
            CustomGUI.Subtitle("Fetcher configuration");

            settings.@namespace = CustomGUI.TextField("Namespace", settings.@namespace.Replace(" ", "_").Trim(),
                DefaultFetcherValues.Namespace);

            CustomGUI.Space();

            settings.packageType = CustomGUI.Popup("Package Type", settings.packageType);

            CustomGUI.Space();

            CustomGUI.BeginHorizontal();
            settings.fetcher.outputPath =
                CustomGUI.TextField("Result output path", settings.fetcher.outputPath, DefaultFetcherValues.OutputPath);
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

            xAPI_Source_View();

            CustomGUI.Divider();

            CustomGUI.Space();

            Fetcher_Args_View();
            
            CustomGUI.Divider();
            
            CustomGUI.BeginHorizontal();
            
            CustomGUI.BeginDisabledGroup(!settings.IsDirty());
            if (CustomGUI.Button("Save settings"))
            {
                GUI.FocusControl(null);
                SaveSettings();
            }
            CustomGUI.EndDisabledGroup();
            
            if (CustomGUI.Button("Reset to default"))
            {
                ResetSettings();
                LoadSettings();
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

        private const string SettingsKey = "EduXR.xAPI.Fetcher";
        
        protected void OnEnable() => LoadSettings();

        private void ResetSettings()
        {
            EditorPrefs.DeleteKey(SettingsKey);
            settings = new FetcherWindowSettings();
            _lastAppliedRepoUrl = string.Empty;
        }

        private void LoadSettings()
        {
            DefaultFetcherValues.EnsureFolders();
            
            var data = EditorPrefs.GetString(SettingsKey, JsonUtility.ToJson(settings, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, settings);
            
            settings ??= new FetcherWindowSettings();

            settings.RememberValues();
        }

        private void SaveSettings()
        {
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString(SettingsKey, data);
            
            settings.RememberValues();
        }
        #endregion
    }
}