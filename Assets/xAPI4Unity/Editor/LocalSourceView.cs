using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    public class LocalSourceView
    {
        private readonly Settings _settings;
        
        public readonly GitHelper Git = new GitHelper(DefaultValues.RepoUrl, DefaultValues.AssetRoot);

        public LocalSourceView(Settings settings)
        {
            _settings = settings;
        }
        public void Show()
        {
            
            CustomGUI.BeginHorizontal();
            
            CustomGUI.BeginDisabledGroup(true);
            _settings.localSource.path = CustomGUI.TextField("Path to xapi repository", _settings.localSource.path);
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.BeginDisabledGroup(Git.IsBusy);
            
            CustomGUI.BeginDisabledGroup(!Git.IsCloned);
            if (CustomGUI.Button("Open in browser"))
            {
                Process.Start(_settings.localSource.path);
            }
            CustomGUI.EndDisabledGroup();
            
            if (CustomGUI.Button("Browse path"))
            {
                _settings.localSource.path =
                    EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", _settings.fetcher.localPath, "xapi");
                if (string.IsNullOrEmpty(_settings.localSource.path))
                    _settings.localSource.path = Git.RepoDirectory;
                GUI.FocusControl(null);
            }

            CustomGUI.BeginDisabledGroup(Git.IsCloned);
            if (CustomGUI.StateButton("Clone repository", Git.IsBusy, "Cloning..."))
            {
                Git.Clone(_settings.localSource.GetNewBranchName());
                _settings.localSource.path = Git.RepoDirectory;
            }
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.EndHorizontal();
            
            if (!Git.IsCloned)
                CustomGUI.Error("For working with 'Local' you need a local copy of the xAPI registry on your computer. Browse for it by click on 'Browse path'. If you haven't cloned one yet, click on Clone repository and it will create an own branch locally which you can use. Make sure you have Git CLI on your computer.");
        }
    }
}