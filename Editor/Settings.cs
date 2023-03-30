using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    public interface ISettings
    {
        T Clone<T>();
        bool Equals(ISettings s);
    }

    [Serializable]
    public class Settings : ISettings
    {
        private const string SettingsKey = "EduXR.xAPI";

        public static Settings instance = new Settings();
        private static Settings _savedInstance = new Settings();

        [SerializeReference] public FetcherSettings fetcher = new FetcherSettings();
        [SerializeReference] public LocalSourceSettings localSource = new LocalSourceSettings();
        [SerializeReference] public GitLabSourceSettings gitlabSource = new GitLabSourceSettings();

        [SerializeReference] public string @namespace = DefaultValues.Namespace;
        [SerializeReference] public PackageType packageType = DefaultValues.PackageType;

        public void Reset()
        {
            EditorPrefs.DeleteKey(SettingsKey);
            instance = new Settings();
            _savedInstance = instance.Clone<Settings>();
        }

        public T Clone<T>()
        {
            var s = new Settings
            {
                fetcher = fetcher.Clone<FetcherSettings>(),
                localSource = localSource.Clone<LocalSourceSettings>(),
                gitlabSource = gitlabSource.Clone<GitLabSourceSettings>(),
                @namespace = @namespace,
                packageType = packageType
            };
            return (T)(object)s;
        }

        public bool Equals(ISettings so)
        {
            var s = so as Settings;
            return fetcher.Equals(s.fetcher) && localSource.Equals(s.localSource) &&
                   gitlabSource.Equals(s.gitlabSource) && @namespace == s.@namespace && packageType == s.packageType;
        }

        public void Load()
        {
            DefaultValues.EnsureFolders();

            var data = EditorPrefs.GetString(SettingsKey, JsonUtility.ToJson(instance, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, instance);

            instance ??= new Settings();
            _savedInstance = instance.Clone<Settings>();
        }

        public void Save()
        {
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString(SettingsKey, data);
            _savedInstance = instance.Clone<Settings>();
        }

        public static bool IsDirty() => !instance.Equals(_savedInstance);
    }

    [Serializable]
    public class FetcherSettings : ISettings
    {
        public class FetcherArguments : Dictionary<string, string>
        {
        }

        [SerializeField] public FetcherType selectedType = DefaultValues.FetcherType;
        [SerializeField] public string selectedUrl = DefaultValues.FetcherAPIBase;
        [SerializeField] public SourceType selectedSourceType = DefaultValues.SourceType;
        [SerializeField] public string localPath = DefaultValues.HomeDirectory;
        [SerializeField] public string cliPath = DefaultValues.CliPath;
        [SerializeField] public string outputPath = DefaultValues.OutputPath;
        [SerializeField] public bool asmDef = DefaultValues.AsmDef;
        [SerializeField] public string version = DefaultValues.Version;

        public T Clone<T>()
        {
            var f = new FetcherSettings
            {
                selectedType = selectedType,
                selectedUrl = selectedUrl,
                selectedSourceType = selectedSourceType,
                localPath = localPath,
                cliPath = cliPath,
                outputPath = outputPath,
                asmDef = asmDef
            };
            return (T)(object)f;
        }

        public bool Equals(ISettings s)
        {
            var f = s as FetcherSettings;
            return asmDef == f.asmDef && selectedType == f.selectedType && selectedUrl == f.selectedUrl &&
                   selectedSourceType == f.selectedSourceType && localPath == f.localPath &&
                   cliPath == f.cliPath && outputPath == f.outputPath;
        }
    }

    [Serializable]
    public class GitLabSourceSettings : ISettings
    {
        [SerializeField] public string repoUrl = DefaultValues.RepoUrl;
        [SerializeField] public string projectId = DefaultValues.GitLabProjectId;
        [SerializeField] public string accessKey = DefaultValues.GitLabAccessKey;
        [SerializeField] public string branch = DefaultValues.GitLabBranch;

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

        public T Clone<T>()
        {
            var gb = new GitLabSourceSettings
            {
                repoUrl = repoUrl,
                branch = branch,
                projectId = projectId,
                accessKey = accessKey
            };
            return (T)(object)gb;
        }

        public bool Equals(ISettings s)
        {
            var gb = s as GitLabSourceSettings;
            return gb.repoUrl == repoUrl && gb.branch == branch && gb.projectId == projectId &&
                   gb.accessKey == accessKey;
        }
    }

    [Serializable]
    public class LocalSourceSettings : ISettings
    {
        [SerializeField] public string path = Path.Join(DefaultValues.AssetRoot, "xapi");
        [SerializeField] public int gitCloneAmount = 0;
        
        public T Clone<T>()
        {
            return (T)(object)(new LocalSourceSettings() { path = path });
        }

        public bool Equals(ISettings s)
            => (s as LocalSourceSettings)?.path == path;
        
        public string GetNewBranchName() => $"xapi-4-unity-{Application.productName.Replace(".", "_")}-{gitCloneAmount++}";
    }
}