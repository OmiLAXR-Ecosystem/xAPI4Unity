#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    public class LocalSourceView
    {
        private static int _downloadMethodTabIndex = -1;
        private Settings Settings => Settings.Instance;

        private readonly GitHelper _git = new GitHelper(DefaultValues.RepoUrl, DefaultValues.AssetRoot);
        
        public LocalSourceView()
        {
        }

        private static void DownloadToDownloads()
        {
            Application.OpenURL("https://gitlab.com/learntech-rwth/xapi/-/archive/master/xapi-master.zip");
        }

        private void DownloadUnityPackage()
        {
            var branch = Settings.localSource.repoBranch ?? DefaultValues.RepoBranch;
            var destPath = Path.Combine(Application.dataPath, "xapi");
            var backupPath = Path.Combine(Application.dataPath, "xapi.backup");

            if (Directory.Exists(destPath))
            {
                Directory.Move(destPath, backupPath);
            }
            
            var zipPath = Path.Combine(Application.dataPath, "xapi.zip");
            var extractPath = Path.Combine(Application.dataPath, "xAPI4Unity");
            Settings.localSource.path = destPath;
            Settings.Save();
            
            var filename = "xapi-" + branch;
            // download
            FileHelper.Download($"https://gitlab.com/learntech-rwth/xapi/-/archive/{branch}/{filename}.zip", zipPath,
                () =>
                {
                    Debug.Log($"Downloaded '{zipPath}'!");
                    // extract
                    FileHelper.ExtractZip(zipPath, extractPath);
                    // delete zip
                    File.Delete(zipPath);
                    // rename
                    var ePath = Path.Combine(extractPath, filename);
                    // move to Assets/xapi
                    Directory.Move(ePath, destPath);

                    var packageFilename = filename + ".unitypackage";
                    AssetDatabase.ExportPackage(destPath, packageFilename, ExportPackageOptions.Recurse);
                    var xapiPackagesPath = Path.Combine(Application.dataPath, "xAPI_Packages");
                    if (!Directory.Exists(xapiPackagesPath))
                        Directory.CreateDirectory(xapiPackagesPath);
                    
                    var packagePath = Path.Combine(xapiPackagesPath, packageFilename);

                    File.Move(packageFilename, packagePath);

                    File.Delete(destPath);
                    
                    // delete extract folder
                    Directory.Delete(extractPath, true);

                    // Restore backup
                    if (Directory.Exists(backupPath))
                        Directory.Move(backupPath, destPath);
                    
                    AssetDatabase.ImportPackage(packagePath, true); 
                    
                    Debug.Log($"Extraction complete to '{extractPath}'!");
                    AssetDatabase.Refresh();
                });
        }
        
        private void DownloadToAssetsAndPatch()
        {
            var branch = Settings.localSource.repoBranch ?? DefaultValues.RepoBranch;
            var filename = $"xapi-{branch}";
            var destPath = Path.Combine(Application.dataPath, "xapi");
            var backupFilename = $"xapi.backup-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var backupPath = Path.Combine(Application.dataPath, backupFilename);
            var zipPath = Path.Combine(Application.dataPath, "xapi.zip");
            var extractPath = Path.Combine(Application.dataPath, "xAPI4Unity_tmp");
            var backupFolder = Path.Combine(Application.dataPath, "xAPI4Unity_Backups");
            var extractedXapiPath = Path.Combine(extractPath, filename);
            var conflictLogPath = Path.Combine(Application.dataPath, "xapi-merge-conflicts.json");

            if (Directory.Exists(destPath))
            {
                Directory.Move(destPath, backupPath);
                if (!Directory.Exists(backupFolder))
                    Directory.CreateDirectory(backupFolder);
                var fullBackupPath = Path.Combine(backupPath, filename);
                FileHelper.CreateZip(backupFolder, fullBackupPath);
                Directory.Delete(backupPath, true);
                Debug.Log($"Created backup: {fullBackupPath}");
            }

            Settings.localSource.path = destPath;
            Settings.Save();

            var url = $"https://gitlab.com/learntech-rwth/xapi/-/archive/{branch}/{filename}.zip";

            FileHelper.Download(url, zipPath, () =>
            {
                try
                {
                    Debug.Log($"Downloaded '{zipPath}'");
                    FileHelper.ExtractZip(zipPath, extractPath);
                    File.Delete(zipPath);

                    Directory.CreateDirectory(destPath);

                    var conflicts = new List<MergeConflict>();

                    MergeXapiFolder(extractedXapiPath, destPath, conflicts);

                    if (conflicts.Count > 0)
                    {
                        File.WriteAllText(conflictLogPath, JsonUtility.ToJson(new MergeConflictLog { conflicts = conflicts }, true));
                        Debug.LogWarning($"Merge conflicts detected. See log at: {conflictLogPath}");
                    }

                    Directory.Delete(extractPath, true);
                    Debug.Log("Extraction and merge complete.");

                    EditorApplication.delayCall += AssetDatabase.Refresh;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Download or merge failed: {ex.Message}");
                }
            });
        }
        
       
        
        private static void MergeXapiFolder(string sourceDir, string destDir, List<MergeConflict> conflictLog)
        {
            foreach (var sourceFilePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = FileHelper.GetRelativePath(sourceDir, sourceFilePath).Replace("\\", "/");
                var destFilePath = Path.Combine(destDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);

                if (!File.Exists(destFilePath))
                {
                    File.Copy(sourceFilePath, destFilePath);
                }
                else
                {
                    var sourceBytes = File.ReadAllBytes(sourceFilePath);
                    var destBytes = File.ReadAllBytes(destFilePath);

                    if (!sourceBytes.SequenceEqual(destBytes))
                    {
                        conflictLog.Add(new MergeConflict
                        {
                            path = relativePath,
                            reason = "File exists and differs"
                        });
                        // Keep original, don't overwrite
                    }
                }
            }
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
            Settings.localSource.path = destPath;
            Settings.Save();
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

        public void Show(Action callback)
        {
            // Local copy
            CustomGUI.Horizontal.Wrap(() =>
            {
                Settings.localSource.path = CustomGUI.TextField("Path of local copy",
                    Settings.localSource.path, Path.GetDirectoryName(Settings.localSource.path));
                
                if (CustomGUI.Button("Browse path"))
                {
                    Settings.localSource.path =
                        EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", Settings.fetcher.localPath,
                            "xapi");
                    if (string.IsNullOrEmpty(Settings.localSource.path))
                        Settings.localSource.path = _git.RepoDirectory;
                    
                    Settings.Save();
                    
                    GUI.FocusControl(null);
                }

                CustomGUI.Disabled(!_git.IsCloned, () =>
                {
                    if (CustomGUI.Button("Open folder"))
                    {
                        Process.Start(Settings.localSource.path);
                    }
                });
            });

            if (!GitHelper.HasDefinitionsFolder(Settings.localSource.path))
            {
                CustomGUI.Horizontal.Wrap(() =>
                {
                    CustomGUI.Error($"The path '{Settings.localSource.path}' has no 'definitions' folder. Make sure you extract the repository content correctly.");
                });
            }

            _downloadMethodTabIndex = CustomGUI.Tabs(_downloadMethodTabIndex,
                new CustomGUI.Tab("(Option 1) I want to download a local copy and place it in project 'Assets' folder.", () =>
                {
                    CustomGUI.Subtitle("Local copy in Assets instructions");
                    Settings.localSource.repoBranch =
                        CustomGUI.TextField("Branch", Settings.localSource.repoBranch, DefaultValues.RepoBranch);
                    CustomGUI.Bullets(
                        "The file will be downloaded and extracted automatically. You don't have to do anything more.",
                        "Enjoy!"
                    );
                    if (CustomGUI.Button("Download to Assets folder"))
                    {
                        // DownloadToAssets();
                        //DownloadUnityPackage();
                        DownloadToAssetsAndPatch();
                        callback();
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
                    Settings.localSource.repoUrl =
                        CustomGUI.TextField("Git repository", Settings.localSource.repoUrl, DefaultValues.RepoUrl);

                    Settings.localSource.repoBranch =
                        CustomGUI.TextField("Git branch", Settings.localSource.repoBranch, default);
                    CustomGUI.Description(
                        "If the 'Git branch' is empty, a new branch will be created automatically. You can rename it later.");
                });


                CustomGUI.Horizontal.Wrap(() =>
                {
                    _git.Url = Settings.localSource.repoUrl;
                    _git.Directory = Path.GetDirectoryName(Settings.localSource.path);

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
                                    _git.SubmoduleClone(Settings.localSource.GetNewBranchName());
                                    Settings.localSource.path = _git.RepoDirectory;
                                    callback();
                                }
                            }
                            else
                            {
                                if (CustomGUI.StateButton("Clone repository", _git.IsBusy, "Cloning..."))
                                {
                                    _git.Clone(Settings.localSource.GetNewBranchName());
                                    Settings.localSource.path = _git.RepoDirectory;
                                    callback();
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