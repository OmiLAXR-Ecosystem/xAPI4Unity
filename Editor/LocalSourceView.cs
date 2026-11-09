/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Settings;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Manages the local source view for downloading, patching, or cloning xAPI repositories.
    /// </summary>
    public class LocalSourceView
    {
        /// <summary>
        /// Gets the settings for the local source.
        /// </summary>
        private LocalSourceSettings _settings => MainSettings.Instance.localSource;

        // Git helper instance for managing Git operations
        private readonly GitHelper _git = new GitHelper(DefaultValues.RepoUrl, DefaultValues.AssetRoot);

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSourceView"/> class.
        /// </summary>
        public LocalSourceView()
        {
        }

        /// <summary>
        /// Opens the download URL in the default browser, saving the download to the user's downloads folder.
        /// </summary>
        private void DownloadToDownloads()
        {
            Application.OpenURL(_settings.GetDownloadUrl());
        }

        /// <summary>
        /// Downloads the Unity package of the xAPI repository and processes it into the project.
        /// </summary>
        private void DownloadUnityPackage()
        {
            var branch = _settings.repoBranch ?? DefaultValues.RepoBranch;
            var destPath = Path.Combine(Application.dataPath, "xapi");
            var backupPath = Path.Combine(Application.dataPath, "xapi.backup");

            if (Directory.Exists(destPath))
            {
                Directory.Move(destPath, backupPath);
            }

            var zipPath = Path.Combine(Application.dataPath, "xapi.zip");
            var extractPath = Path.Combine(Application.dataPath, "xAPI4Unity");
            _settings.path = destPath;
            MainSettings.Instance.Save();

            var filename = _settings.DownloadFilename;

            // Download the file
            FileHelper.Download(_settings.GetDownloadUrl(), zipPath,
                () =>
                {
                    Debug.Log($"Downloaded '{zipPath}'!");
                    FileHelper.ExtractZip(zipPath, extractPath); // Extract downloaded zip
                    File.Delete(zipPath); // Remove zip file after extraction
                    var ePath = Path.Combine(extractPath, filename);
                    Directory.Move(ePath, destPath); // Move files to "Assets/xapi"

                    // Export as a .unitypackage and handle cleanup
                    var packageFilename = filename + ".unitypackage";
                    AssetDatabase.ExportPackage(destPath, packageFilename, ExportPackageOptions.Recurse);
                    var xapiPackagesPath = Path.Combine(Application.dataPath, "xAPI_Packages");
                    if (!Directory.Exists(xapiPackagesPath))
                        Directory.CreateDirectory(xapiPackagesPath);

                    var packagePath = Path.Combine(xapiPackagesPath, packageFilename);
                    File.Move(packageFilename, packagePath);
                    File.Delete(destPath);
                    Directory.Delete(extractPath, true); // Clean up extraction

                    if (Directory.Exists(backupPath))
                        Directory.Move(backupPath, destPath); // Restore backup if applicable

                    AssetDatabase.ImportPackage(packagePath, true);
                    Debug.Log($"Extraction complete to '{extractPath}'!");
                    AssetDatabase.Refresh();
                });
        }

        /// <summary>
        /// Downloads and merges the xAPI repository into the "Assets/xapi" folder.
        /// </summary>
        private void DownloadToAssetsAndPatch()
        {
            var filename = _settings.DownloadFilename;
            var destPath = Path.Combine(Application.dataPath, "xapi");
            var zipPath = Path.Combine(Application.dataPath, "xapi.zip");
            var extractPath = Path.Combine(Application.dataPath, filename);
            var mergeReportPath = Path.Combine(destPath, DateTime.Now.ToString("yyyyMMddhhmmss") + "-xapi-report.json");

            _settings.path = destPath;
            MainSettings.Instance.Save();

            if (!Directory.Exists(destPath))
            {
                DownloadToAssets();
                return;
            }

            var url = _settings.GetDownloadUrl();
            
            FileHelper.Download(url, zipPath, () =>
            {
                try
                {
                    Debug.Log($"Downloaded '{url}' to '{zipPath}'.");
                    FileHelper.ExtractZip(zipPath, Application.dataPath);
                    File.Delete(zipPath); // Remove the downloaded zip

                    if (!Directory.Exists(destPath))
                        Directory.CreateDirectory(destPath);

                    var mergeReport = MergeXapiFolder(extractPath, destPath);
                    File.WriteAllText(mergeReportPath, JsonUtility.ToJson(mergeReport, true));
                    
                    Directory.Delete(extractPath, true); // Clean up extracted files
                    Debug.Log($"Merge Copied: {mergeReport.copied}, Conflicts: {mergeReport.conflicts}, Skipped: {mergeReport.skipped}. Merge report at {mergeReportPath}.");
                    EditorApplication.delayCall += AssetDatabase.Refresh;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Download or merge failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Merges the contents of the extracted xAPI folder into the destination directory.
        /// </summary>
        private static MergeReport MergeXapiFolder(string sourceDir, string destDir)
        {
            var mergeReport = new MergeReport(Path.GetDirectoryName(destDir));
            foreach (var sourceFilePath in Directory.GetFiles(sourceDir, "*.json", SearchOption.AllDirectories))
            {
                var relativePath = FileHelper.GetRelativePath(sourceDir, sourceFilePath).Replace("\\", "/");
                var destFilePath = Path.Combine(destDir, relativePath);
                
                if (!File.Exists(destFilePath))
                {
                    FileHelper.EnsurePath(destFilePath); // Ensure all directories exist before moving the file
                    File.Move(sourceFilePath, destFilePath); // Move new file into place
                    mergeReport.AddCopy(relativePath);
                }
                else
                {
                    // Check if files have conflict
                    var sourceBytes = File.ReadAllBytes(sourceFilePath);
                    var destBytes = File.ReadAllBytes(destFilePath);

                    if (!sourceBytes.SequenceEqual(destBytes))
                    {
                        mergeReport.AddConflict(relativePath);
                        File.Move(sourceFilePath, destFilePath.Replace(".json", ".json-conflict")); // Rename conflicting files
                    }
                    else
                    {
                        mergeReport.AddSkip(relativePath); // Existing files are skipped if identical
                    }
                }
            }
            return mergeReport;
        }

        /// <summary>
        /// Downloads the xAPI repository directly into the "Assets/xapi" folder for a fresh import.
        /// </summary>
        private void DownloadToAssets()
        {
            var destPath = Path.Combine(Application.dataPath, "xapi");

            if (Directory.Exists(destPath))
            {
                Debug.Log($"The folder '{destPath}' already exists. Delete it and retry.");
                return;
            }

            var zipPath = Path.Combine(Application.dataPath, "xapi.zip");
            var extractPath = Path.Combine(Application.dataPath, "__ExtractedXAPI__");
            _settings.path = destPath;
            MainSettings.Instance.Save();

            FileHelper.Download(_settings.GetDownloadUrl(), zipPath,
                () =>
                {
                    FileHelper.ExtractZip(zipPath, extractPath);
                    File.Delete(zipPath); // Cleanup zip file
                    Directory.Move(Path.Combine(extractPath, _settings.DownloadFilename), destPath);
                    Directory.Delete(extractPath, true); // Cleanup extracted folder
                    Debug.Log($"Extraction complete to '{destPath}'!");
                    AssetDatabase.Refresh();
                });
        }

        /// <summary>
        /// Displays the interface for selecting and managing the local path for the xAPI repository.
        /// </summary>
        private void LocalPathView()
        {
            CustomGUI.Horizontal.Wrap(() =>
            {
                if (string.IsNullOrEmpty(_settings.path))
                    _settings.path = DefaultValues.RepoPath;

                _settings.path = CustomGUI.TextField("Path", _settings.path, DefaultValues.RepoPath);

                if (CustomGUI.Button("Browse"))
                {
                    var browsedPath =
                        EditorUtility.OpenFolderPanel("Browse xAPI Definitions root path", _settings.path,
                            "xapi");
                    // An empty path means the dialog was cancelled
                    if (!string.IsNullOrEmpty(browsedPath))
                        _settings.path = browsedPath;

                    MainSettings.Instance.Save();
                    GUI.FocusControl(null);
                }

                CustomGUI.Disabled(!Directory.Exists(_settings.path), () =>
                {
                    if (CustomGUI.Button("Open folder"))
                    {
                        EditorUtility.RevealInFinder(_settings.path);
                    }
                });
            });
        }

        /// <summary>
        /// Updates the xAPI folder based on the selected option in the settings.
        /// </summary>
        public void UpdateXApiFolder()
        {
            if (_settings.selectedOptionIndex == 0)
            {
                DownloadToAssetsAndPatch();
            } 
            else if (_settings.selectedOptionIndex == 1)
            {
                DownloadToDownloads();
            } 
            else if (_settings.selectedOptionIndex == 2)
            {
                PullClone();
            }
        }

        /// <summary>
        /// Pulls the latest changes into the git clone at the local path.
        /// </summary>
        private void PullClone()
        {
            // A clone has a .git folder, a submodule has a .git file
            var gitPath = Path.Combine(_settings.path, ".git");
            if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
            {
                Debug.LogError($"[xAPI4Unity] '{_settings.path}' is not a git clone. Clone it first or use Option 1 or 2.");
                return;
            }

            _git.Pull(_settings.path);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Displays the repository settings of the selected source option inside a foldout.
        /// </summary>
        /// <param name="git">Whether to show the git URL instead of the download URL and branch.</param>
        private void RepositoryView(bool git)
        {
            var summary = git ? _settings.repoUrl : $"{_settings.repoDownloadUrl}, {_settings.repoBranch}";
            CustomGUI.Foldout($"Repository ({summary})", "LocalSourceView.Repository", () =>
            {
                if (git)
                {
                    _settings.repoUrl =
                        CustomGUI.TextField("Git URL", _settings.repoUrl, DefaultValues.RepoUrl);
                }
                else
                {
                    _settings.repoDownloadUrl = CustomGUI.TextField("Repository URL", _settings.repoDownloadUrl, DefaultValues.RepoDownloadUrl);
                    _settings.repoBranch =
                        CustomGUI.TextField("Branch", _settings.repoBranch, DefaultValues.RepoBranch);
                }
                CustomGUI.Description("Change this only if you work with a fork of the repository.");
            });
        }

        /// <summary>
        /// Renders the LocalSourceView interface and performs the respective actions based on user interactions.
        /// </summary>
        public void Show(Action callback)
        {
            LocalPathView();
            CustomGUI.Space();

            // Display options for managing the xAPI source
            _settings.selectedOptionIndex = CustomGUI.Tabs(_settings.selectedOptionIndex, new[]
            {
                new CustomGUI.Tab("Patch into Assets",
                    () =>
                    {
                        CustomGUI.Description(
                            "Downloads the repository archive and merges it into 'Assets/xapi'. New files are added, identical files are skipped, and files that differ from yours are listed in a merge report. Back up 'Assets/xapi' first if it is not under version control.");
                        RepositoryView(false);
                        CustomGUI.Space();

                        CustomGUI.Horizontal.Wrap(() =>
                        {
                            if (CustomGUI.Button("Download and patch", GUILayout.Height(28)))
                            {
                                DownloadToAssetsAndPatch();
                                callback();
                            }

                            CustomGUI.Disabled(!Directory.Exists(_settings.path), () =>
                            {
                                if (CustomGUI.Button("Resolve conflicts", GUILayout.Height(28)))
                                    XapiMergeWindow.ShowWindow();
                            });
                        });
                    }),
                new CustomGUI.Tab("Download zip", () =>
                {
                    CustomGUI.Description("Downloads the repository archive with your browser.");
                    RepositoryView(false);
                    CustomGUI.Bullets(
                        "Extract the archive.",
                        "Set 'Path' above to the extracted folder, the one that contains 'definitions'."
                    );
                    CustomGUI.Space();

                    if (CustomGUI.Button("Download zip", GUILayout.Height(28)))
                    {
                        DownloadToDownloads();
                    }
                }),
                new CustomGUI.Tab("Git clone", () =>
                {
                    _git.Url = _settings.repoUrl;
                    _git.Directory = Path.GetDirectoryName(_settings.path);

                    var parent = Directory.GetParent(Application.dataPath);
                    var projectHasGit = parent != null && Directory.Exists(Path.Combine(parent.ToString(), ".git"));

                    CustomGUI.Description(projectHasGit
                        ? "Adds the repository as a git submodule next to 'Path' and checks out a new local branch."
                        : "Clones the repository next to 'Path' and checks out a new local branch.");
                    CustomGUI.Disabled(_git.IsBusy, () => RepositoryView(true));
                    CustomGUI.Space();

                    if (_git.IsCloned)
                    {
                        if (CustomGUI.StateButton("Pull latest changes", _git.IsBusy, "Pulling...", GUILayout.Height(28)))
                            PullClone();
                        return;
                    }

                    if (CustomGUI.StateButton(projectHasGit ? "Clone as submodule" : "Clone repository", _git.IsBusy,
                            "Cloning...", GUILayout.Height(28)))
                    {
                        if (projectHasGit)
                            _git.SubmoduleClone(_settings.GetNewBranchName());
                        else
                            _git.Clone(_settings.GetNewBranchName());
                        _settings.path = _git.RepoDirectory;
                        callback();
                    }
                })
            }, newIndex =>
            {
                _settings.selectedOptionIndex = newIndex;
                MainSettings.Instance.Save();
            });
        }
    }
}
#endif