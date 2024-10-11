#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    public class LocalSourceView
    {
        private bool cloningIsOpen;
        private bool downloadLocalCopy;
        private readonly Settings _settings;

        public readonly GitHelper Git = new GitHelper(DefaultValues.RepoUrl, DefaultValues.AssetRoot);

        public event OnTriggeredControlHandler OnTriggeredControl;

        public LocalSourceView(Settings settings)
        {
            _settings = settings;
        }

        private void DownloadLocalCopy()
        {
            Application.OpenURL("https://gitlab.com/learntech-rwth/xapi/-/archive/master/xapi-master.zip");
        }

        public void Show()
        {
            // Local copy
            CustomGUI.Horizontal.Wrap(() =>
            {
                CustomGUI.Disabled(true,
                    () =>
                    {
                        _settings.localSource.path = CustomGUI.TextField("Path of local copy",
                            _settings.localSource.path, Path.GetDirectoryName(_settings.localSource.path),
                            OnTriggeredControl);
                    });

                if (CustomGUI.Button("Browse path", OnTriggeredControl))
                {
                    _settings.localSource.path =
                        EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", _settings.fetcher.localPath,
                            "xapi");
                    if (string.IsNullOrEmpty(_settings.localSource.path))
                        _settings.localSource.path = Git.RepoDirectory;
                    GUI.FocusControl(null);
                }

                CustomGUI.Disabled(!Git.IsCloned, () =>
                {
                    if (CustomGUI.Button("Open folder", OnTriggeredControl))
                    {
                        Process.Start(_settings.localSource.path);
                    }
                });
            });
            
            CustomGUI.Horizontal.Wrap(() =>
            {
                if (CustomGUI.Button("(Option 1) I want to use git", disabled: cloningIsOpen))
                {
                    cloningIsOpen = true;
                    downloadLocalCopy = false;
                }
                
                if (CustomGUI.Button("(Option 2) I want just download a local copy", disabled: downloadLocalCopy))
                {
                    cloningIsOpen = false;
                    downloadLocalCopy = true;
                    DownloadLocalCopy();
                }
            });
            
            CustomGUI.Region(!downloadLocalCopy, () =>
            {
                CustomGUI.Subtitle("Local copy instructions");
                CustomGUI.Bullets(
                    "Download the xAPI repository zip file. (A download of official xAPI Repository was already started. Check your downloads folder.)",
                    "Extract the content.",
                    "Make sure you have placed it in your 'Path of local copy'. The folder 'xapi' should have a directory 'definitions' in its root."
                    );
            });
            
            CustomGUI.Region(!cloningIsOpen, () =>
            {
                CustomGUI.Subtitle("Git Settings");
                CustomGUI.Disabled(Git.IsBusy, () =>
                {
                    _settings.localSource.repoUrl =
                        CustomGUI.TextField("Git repository", _settings.localSource.repoUrl, DefaultValues.RepoUrl,
                            OnTriggeredControl);

                    _settings.localSource.repoBranch =
                        CustomGUI.TextField("Git branch", _settings.localSource.repoBranch, default,
                            OnTriggeredControl);
                    CustomGUI.Description(
                        "If the 'Git branch' is empty, a new branch will be created automatically. You can rename it later.");
                });


                CustomGUI.Horizontal.Wrap(() =>
                {
                    Git.Url = _settings.localSource.repoUrl;
                    Git.Directory = Path.GetDirectoryName(_settings.localSource.path);

                    CustomGUI.Disabled(Git.IsCloned, () =>
                    {
                        var projectRoot = Directory.GetParent(Application.dataPath)!.ToString();
                        var projectHasGit = Directory.Exists(Path.Combine(projectRoot, ".git"));

                        if (projectHasGit)
                        {
                            if (CustomGUI.StateButton("Clone as submodule", Git.IsBusy, "Cloning...",
                                    OnTriggeredControl))
                            {
                                Git.SubmoduleClone(_settings.localSource.GetNewBranchName());
                                _settings.localSource.path = Git.RepoDirectory;
                            }
                        }
                        else
                        {
                            if (CustomGUI.StateButton("Clone repository", Git.IsBusy, "Cloning...", OnTriggeredControl))
                            {
                                Git.Clone(_settings.localSource.GetNewBranchName());
                                _settings.localSource.path = Git.RepoDirectory;
                            }
                        }

                        if (!Git.IsCloned)
                        {
                            const string errorMessage =
                                "You need a local copy of the xAPI registry on your computer. Browse in `xAPI4Unity > Settings` for it by click on 'Browse path'. If you haven't cloned one yet, click on 'Clone repository' or 'Clone as submodule' and it will create an own branch locally which you can use. Make sure you are allowed to access the repository and your have configured the Git CLI on your computer correctly (access key or ssh).";
                            CustomGUI.Error(errorMessage);
                            UnityEngine.Debug.LogError(errorMessage);
                        }
                    });
                });
            });
        }
    }
}
#endif