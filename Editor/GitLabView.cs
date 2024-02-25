#if UNITY_EDITOR
using System.Diagnostics;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    public class GitLabView
    {
        private string _lastAppliedRepoUrl = "";

        private readonly Settings _settings;

        public event OnTriggeredControlHandler OnTriggeredControl;
        public GitLabView(Settings settings)
        {
            _settings = settings;
        }

        private void ApplyGitLabRepo()
        {
            _settings.gitlabSource.AutoDetectProjectId();
            _lastAppliedRepoUrl = _settings.gitlabSource.repoUrl;

            _settings.gitlabSource.AutoDetectBranches();
        }
        
        public void Show()
        {
            CustomGUI.Horizontal.Begin();
            _settings.gitlabSource.repoUrl = CustomGUI.TextField("GitLab Repository", _settings.gitlabSource.repoUrl, default, OnTriggeredControl);

            CustomGUI.BeginDisabledGroup(_settings.gitlabSource.repoUrl == _lastAppliedRepoUrl);
            if (CustomGUI.Button("Apply", OnTriggeredControl))
            {
                ApplyGitLabRepo();
            }

            CustomGUI.EndDisabledGroup();

            CustomGUI.Horizontal.End();

            CustomGUI.BeginDisabledGroup(_settings.gitlabSource.repoUrl != _lastAppliedRepoUrl);

            CustomGUI.Horizontal.Begin();
            _settings.gitlabSource.accessKey = CustomGUI.TextField("Access token", _settings.gitlabSource.accessKey, default, OnTriggeredControl);
            if (CustomGUI.Button("Create one", OnTriggeredControl))
            {
                Process.Start(_settings.gitlabSource.AccessTokenUri);
            }

            CustomGUI.Horizontal.End();

            CustomGUI.Horizontal.Begin();
            _settings.gitlabSource.projectId =
                CustomGUI.TextField("Project ID", _settings.gitlabSource.projectId, "", OnTriggeredControl, GUILayout.MaxWidth(300));

            CustomGUI.BeginDisabledGroup(_settings.gitlabSource.IsProjectIdMatchingRepoUrl());
            if (CustomGUI.Button("Fetch Project ID from repo", OnTriggeredControl))
            {
                _settings.gitlabSource.AutoDetectProjectId();
            }

            CustomGUI.EndDisabledGroup();
            CustomGUI.Horizontal.End();

            CustomGUI.BeginDisabledGroup(string.IsNullOrEmpty(_settings.gitlabSource.projectId));

            CustomGUI.Horizontal.Begin();
            _settings.gitlabSource.branch = CustomGUI.Popup("Branch", _settings.gitlabSource.Branches, _settings.gitlabSource.branch, OnTriggeredControl);
            if (CustomGUI.Button("Refresh"))
            {
                _settings.gitlabSource.AutoDetectBranches();
            }

            CustomGUI.Horizontal.End();
            CustomGUI.EndDisabledGroup();

            CustomGUI.EndDisabledGroup();
        }

        public void Reset()
        {
            _lastAppliedRepoUrl = string.Empty;
        }
    }
}
#endif