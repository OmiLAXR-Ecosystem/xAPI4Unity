#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    public class LocalSourceView
    {
        private readonly Settings _settings;
        
        public readonly GitHelper Git = new GitHelper(DefaultValues.RepoUrl, DefaultValues.AssetRoot);

        public event OnTriggeredControlHandler OnTriggeredControl;

        public LocalSourceView(Settings settings)
        {
            _settings = settings;
        }
        public void Show()
        {
            CustomGUI.Horizontal.Begin();
            
            CustomGUI.BeginDisabledGroup(true);
            _settings.localSource.path = CustomGUI.TextField("Path to xAPI repository", _settings.localSource.path, Path.GetDirectoryName(_settings.localSource.path), OnTriggeredControl);
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.BeginDisabledGroup(Git.IsBusy);
            
            CustomGUI.BeginDisabledGroup(!Git.IsCloned);
            if (CustomGUI.Button("Open in browser", OnTriggeredControl))
            {
                Process.Start(_settings.localSource.path);
            }
            CustomGUI.EndDisabledGroup();
            
            if (CustomGUI.Button("Browse path", OnTriggeredControl))
            {
                // todo on changed
                _settings.localSource.path =
                    EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", _settings.fetcher.localPath, "xapi");
                if (string.IsNullOrEmpty(_settings.localSource.path))
                    _settings.localSource.path = Git.RepoDirectory;
                GUI.FocusControl(null);
            }

            Git.Directory = Path.GetDirectoryName(_settings.localSource.path);
            
            CustomGUI.BeginDisabledGroup(Git.IsCloned);
            if (CustomGUI.StateButton("Clone repository", Git.IsBusy, "Cloning...", OnTriggeredControl))
            {
                Git.Clone(_settings.localSource.GetNewBranchName());
                _settings.localSource.path = Git.RepoDirectory;
            }
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.EndDisabledGroup();
            
            CustomGUI.Horizontal.End();
            
            if (!Git.IsCloned)
                CustomGUI.Error("For working with 'Local' you need a local copy of the xAPI registry on your computer. Browse for it by click on 'Browse path'. If you haven't cloned one yet, click on Clone repository and it will create an own branch locally which you can use. Make sure you have Git CLI on your computer.");
        }
    }
}
#endif