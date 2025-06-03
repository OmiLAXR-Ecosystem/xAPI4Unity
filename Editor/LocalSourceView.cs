#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    public class LocalSourceView
    {
        private static int _downloadMethodTabIndex = -1;
        private readonly Settings _settings;

        private readonly GitHelper _git;
        
        public LocalSourceView(Settings settings)
        {
            _settings = settings;
            _git = new GitHelper(DefaultValues.RepoUrl, DefaultValues.AssetRoot);
        }

        private static void DownloadToDownloads()
        {
            Application.OpenURL("https://gitlab.com/learntech-rwth/xapi/-/archive/master/xapi-master.zip");
        }

        private void DownloadToAssets()
        {
            var destPath = Path.Combine(Application.dataPath, "xapi");

            if (Directory.Exists(destPath))
            {
                Debug.Log($"The folder '{destPath}' does already exist. Delete it and repeat this procedure.");
                return;
            }
            
            var zipPath = Path.Combine(Application.dataPath, "xapi.zip");
            var extractPath = Path.Combine(Application.dataPath, "__ExtractedXAPI__");
            _settings.localSource.path = destPath;
            _settings.Save();
            // download
            FileHelper.Download("https://gitlab.com/learntech-rwth/xapi/-/archive/master/xapi-master.zip", zipPath,
                () =>
                {
                    Debug.Log($"Downloaded '{zipPath}'!");
                    // extract
                    FileHelper.ExtractZip(zipPath, extractPath);
                    // delete zip
                    File.Delete(zipPath);
                    // rename
                    var ePath = Path.Combine(extractPath, "xapi-master");
                    Directory.Move(ePath, destPath);
                    Directory.Delete(extractPath, true);
                    Debug.Log($"Extraction complete to '{extractPath}'!");
                    AssetDatabase.Refresh();
                });
        }

        public void Show()
        {
            // Local copy
            CustomGUI.Horizontal.Wrap(() =>
            {
                _settings.localSource.path = CustomGUI.TextField("Path of local copy",
                    _settings.localSource.path, Path.GetDirectoryName(_settings.localSource.path));
                
                if (CustomGUI.Button("Browse path"))
                {
                    _settings.localSource.path =
                        EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", _settings.fetcher.localPath,
                            "xapi");
                    if (string.IsNullOrEmpty(_settings.localSource.path))
                        _settings.localSource.path = _git.RepoDirectory;
                    
                    _settings.Save();
                    
                    GUI.FocusControl(null);
                }

                CustomGUI.Disabled(!_git.IsCloned, () =>
                {
                    if (CustomGUI.Button("Open folder"))
                    {
                        Process.Start(_settings.localSource.path);
                    }
                });
            });

            if (!GitHelper.HasDefinitionsFolder(_settings.localSource.path))
            {
                CustomGUI.Horizontal.Wrap(() =>
                {
                    CustomGUI.Error($"The path '{_settings.localSource.path}' has no 'definitions' folder. Make sure you extract the repository content correctly.");
                });
            }
            
            _downloadMethodTabIndex = CustomGUI.Tabs(_downloadMethodTabIndex,
                new CustomGUI.Tab("(Option 1) I want download a local copy and place it in project 'Assets' folder.", () =>
                {
                    CustomGUI.Subtitle("Local copy in Assets instructions");
                    CustomGUI.Bullets(
                        "The file will be downloaded and extracted automatically. You don't have to do anything more.",
                        "Enjoy!"
                    );
                    if (CustomGUI.Button("Download to Assets folder"))
                    {
                        DownloadToAssets();
                    }
                }),
                new CustomGUI.Tab("(Option 2) I want just download a local copy to selected path", () =>
                {
                    CustomGUI.Subtitle("Local copy instructions");
                    CustomGUI.Bullets(
                        "Download the xAPI repository zip file. (A download of official xAPI Repository was already started. Check your downloads folder.)",
                        "Extract the content.",
                        "Make sure you have placed it in your 'Path of local copy'. The folder 'xapi' should have a directory 'definitions' in its root."
                    );
                    if (CustomGUI.Button("Download xapi.zip"))
                    {
                        DownloadToDownloads();
                    }
                }),
                new CustomGUI.Tab("(Option 3) I want to use git and clone it to my selected path.", () =>
                {
                    CustomGUI.Subtitle("Git Settings");
                CustomGUI.Disabled(_git.IsBusy, () =>
                {
                    _settings.localSource.repoUrl =
                        CustomGUI.TextField("Git repository", _settings.localSource.repoUrl, DefaultValues.RepoUrl);

                    _settings.localSource.repoBranch =
                        CustomGUI.TextField("Git branch", _settings.localSource.repoBranch, default);
                    CustomGUI.Description(
                        "If the 'Git branch' is empty, a new branch will be created automatically. You can rename it later.");
                });


                CustomGUI.Horizontal.Wrap(() =>
                {
                    _git.Url = _settings.localSource.repoUrl;
                    _git.Directory = Path.GetDirectoryName(_settings.localSource.path);

                    CustomGUI.Disabled(_git.IsCloned, () =>
                    {
                        var parent = Directory.GetParent(Application.dataPath);
                        if (parent != null)
                        {
                            var projectRoot = parent.ToString();
                            var projectHasGit = Directory.Exists(Path.Combine(projectRoot, ".git"));

                            if (projectHasGit)
                            {
                                if (CustomGUI.StateButton("Clone as submodule", _git.IsBusy, "Cloning..."))
                                {
                                    _git.SubmoduleClone(_settings.localSource.GetNewBranchName());
                                    _settings.localSource.path = _git.RepoDirectory;
                                }
                            }
                            else
                            {
                                if (CustomGUI.StateButton("Clone repository", _git.IsBusy, "Cloning..."))
                                {
                                    _git.Clone(_settings.localSource.GetNewBranchName());
                                    _settings.localSource.path = _git.RepoDirectory;
                                }
                            }
                        }

                        if (!_git.IsCloned)
                        {
                            const string errorMessage =
                                "You need a local copy of the xAPI registry on your computer. Browse in `xAPI4Unity > Settings` for it by click on 'Browse path'. If you haven't cloned one yet, click on 'Clone repository' or 'Clone as submodule' and it will create an own branch locally which you can use. Make sure you are allowed to access the repository and your have configured the Git CLI on your computer correctly (access key or ssh).";
                            CustomGUI.Error(errorMessage);
                            Debug.LogError(errorMessage);
                        }
                    });
                });
                }));
        }
    }
}
#endif