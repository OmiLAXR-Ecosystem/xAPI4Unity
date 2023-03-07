using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
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

        [SerializeField] public FetcherType selectedType = DefaultValues.FetcherType;
        [SerializeField] public string selectedUrl = DefaultValues.FetcherAPIBase;
        [SerializeField] public SourceType selectedSourceType = DefaultValues.SourceType;
        [SerializeField] public string localPath = DefaultValues.HomeDirectory;
        [SerializeField] public string exePath = DefaultValues.ExePath;
        [SerializeField] public string outputPath = DefaultValues.OutputPath;
    }

    [Serializable]
    public class GitLabSourceSettings
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
}