using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
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
            var res = GUILayout.Button(text,  ButtonStyle, (new []
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

        public static T Popup<T>(string label, T value) where T : Enum
            => (T)EditorGUILayout.EnumPopup(label, value, GUILayout.MaxWidth(300));
        
        public static void Divider(int spaceLines = 1)
        {
            Space(spaceLines);
            const float height = 1.0f;
            var rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
            Space(spaceLines);
        }

        public static string TextField(string label, string text, params GUILayoutOption[] options)
            => EditorGUILayout.TextField(label, text, (new []
            {
                GUILayout.MinWidth(250)
            }).Concat(options).ToArray());
    }

    public class xAPI_Definitions_Fetcher_Window_RepoPopup : EditorWindow
    {
        public string gitLabRepoUrl = "";
        private void OnGUI()
        {
            CustomGUI.Title("Get Project ID from GitLab Repository");
            CustomGUI.Description("Paste your GitLab repository URL here and the Project ID will be set automatically.");

            EditorGUILayout.BeginHorizontal();
            gitLabRepoUrl = CustomGUI.TextField("GitLab Repository", gitLabRepoUrl);
            if (CustomGUI.Button("Get Project ID"))
            {
                var mr = UnityWebRequest.Get(gitLabRepoUrl);
            }
            if (CustomGUI.Button("Abort"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();

        }
    }
    
    public class xAPI_Definitions_Fetcher_Window : EditorWindow
    {
        public enum SourceType
        {
            GitLab, Local
        }

        public enum FetcherType
        {
            WebService, CommandLine
        }

        [Serializable]
        public class GitLabSourceSettings
        {
            [SerializeField] public string projectId = "";
            [SerializeField] public string privateKey = "";
            [SerializeField] public string branch = "main";
        }

        [Serializable]
        public class LocalSourceSettings
        {
            [SerializeField] public string path = "";
        }
        
        // public const string Fetcher_API_BASE = "https://localhost:5001/";
        public const string FetcherAPIBase = "https://fetch.xapi.elearn.rwth-aachen.de/";

        private static string DefaultOutputPath => "";
        private delegate Dictionary<string, string> ArgumentsHandler();

        [SerializeField] private string outputPath = "";
        [SerializeField] private string fetcherPath = "";

        [SerializeField] private string localPath = "";

        [SerializeReference] private LocalSourceSettings localSource = new LocalSourceSettings();
        [SerializeReference] private GitLabSourceSettings gitlabSource = new GitLabSourceSettings();
        
        [SerializeField]
        [FormerlySerializedAs("namespace")] 
        private string @namespace = "EduXR.xAPI.Definitions";

        [SerializeField] private bool useShellWindow = false;
        
        [SerializeField] private SourceType selectedSourceType = SourceType.Local;
        [SerializeField] private FetcherType selectedFetcherType = FetcherType.CommandLine;

        private xAPI_Definitions_Fetcher_Window_RepoPopup _repoPopup;

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

        private void Fetcher_View()
        {
            CustomGUI.Subtitle("Select your tool");
            CustomGUI.Description("You can choose between two fetcher tools. The web service can be used directly. The Command Line tool has to downloaded first but can be used for local repositories.");
            selectedFetcherType = CustomGUI.Popup("Fetcher type", selectedFetcherType);
        }
        
        private void xAPI_Source_View()
        {
            EditorGUI.BeginDisabledGroup(selectedFetcherType != FetcherType.CommandLine);
            CustomGUI.Subtitle("xAPI Source");
            CustomGUI.Description("If you are using the source type 'Local' you need a local copy of the xAPI registry on your computer.");

            if (selectedFetcherType == FetcherType.CommandLine)
                selectedSourceType = CustomGUI.Popup("Source type", selectedSourceType);
            else
                CustomGUI.Popup("Source type", SourceType.Local);
            
            SourceType_View();

            EditorGUI.EndDisabledGroup();
        }

        private void OpenRepoPopup()
        {
            if (!_repoPopup)
                _repoPopup = CreateInstance<xAPI_Definitions_Fetcher_Window_RepoPopup>();
            _repoPopup.position = new Rect(Screen.width / 2.0f, Screen.height / 2.0f, 500, 100);
            _repoPopup.ShowPopup();
            GUI.FocusControl(null);
        }
        
        private void GitLabSource_View()
        {
            EditorGUILayout.BeginHorizontal();
            gitlabSource.projectId = CustomGUI.TextField("Project ID", gitlabSource.projectId, GUILayout.MaxWidth(300));
            if (CustomGUI.Button("Get Project ID"))
            {
                OpenRepoPopup();
            }
            EditorGUILayout.EndHorizontal();

            gitlabSource.privateKey = CustomGUI.TextField("Private access key", gitlabSource.privateKey);
            gitlabSource.branch = CustomGUI.TextField("Branch", gitlabSource.branch);
        }

        private void LocalSource_View()
        {
            EditorGUILayout.BeginHorizontal();
            localSource.path = CustomGUI.TextField("Local root path", localSource.path);
            if (CustomGUI.Button("Browse path"))
            {
                localPath = EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", localPath, "xapi");
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
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
                outputPath =DefaultOutputPath;
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

        private Dictionary<string, string> GetArguments(Dictionary<string, string> args = null) {
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
            return new Dictionary<string,string> {
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
