/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Releases;
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

        /// <summary>
        /// Runs git for the clone option.
        /// </summary>
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
        internal void DownloadToDownloads()
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
        internal void DownloadToAssetsAndPatch() => DownloadToAssetsAndPatch(null);

        /// <summary>
        /// Downloads the archive of the custom repository's branch, tag or commit and merges it into the "Assets/xapi" folder.
        /// </summary>
        /// <param name="done">Gets a summary and whether it worked. Optional.</param>
        internal void DownloadToAssetsAndPatch(Action<string, bool> done)
        {
            var destPath = DefaultValues.RepoPath;
            var mergeReportPath = Path.Combine(destPath, NewReportName());

            _settings.path = destPath;
            MainSettings.Instance.Save();

            if (!Directory.Exists(destPath))
            {
                DownloadToAssets(done);
                return;
            }
            if (!CheckGitRef(done))
                return;

            var url = _settings.GetDownloadUrl();
            var temp = NewTempFolder();
            var zipPath = Path.Combine(temp, "archive.zip");

            WebRequests.DownloadFile(url, zipPath, error =>
            {
                try
                {
                    if (error != null)
                    {
                        Report(done, $"Download failed: {error}", false);
                        return;
                    }
                    Debug.Log($"Downloaded '{url}' to '{zipPath}'.");
                    var root = ExtractArchive(zipPath, Path.Combine(temp, "extracted"));

                    var mergeReport = MergeXapiFolder(root, destPath);
                    File.WriteAllText(mergeReportPath, JsonUtility.ToJson(mergeReport, true));

                    Report(done, $"Patched '{_settings.GitRef}' into 'Assets/xapi'. Copied: {mergeReport.copied}, conflicts: {mergeReport.conflicts}, skipped: {mergeReport.skipped}. Merge report at {mergeReportPath}.", true);
                    EditorApplication.delayCall += AssetDatabase.Refresh;
                }
                catch (Exception ex)
                {
                    Report(done, $"Download or merge failed: {ex.Message}", false);
                }
                finally
                {
                    DeleteTempFolder(temp);
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
                    FileHelper.EnsurePath(destFilePath); // Ensure all directories exist before copying the file
                    File.Copy(sourceFilePath, destFilePath); // Copy new file into place
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
                        File.Copy(sourceFilePath, destFilePath + ReleaseInstaller.ConflictSuffix, true); // Keep the incoming file next to yours
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
        /// <param name="done">Gets a summary and whether it worked. Optional.</param>
        private void DownloadToAssets(Action<string, bool> done)
        {
            var destPath = DefaultValues.RepoPath;

            if (Directory.Exists(destPath))
            {
                Report(done, $"The folder '{destPath}' already exists. Delete it and retry.", false);
                return;
            }
            if (!CheckGitRef(done))
                return;

            var temp = NewTempFolder();
            var zipPath = Path.Combine(temp, "archive.zip");
            _settings.path = destPath;
            MainSettings.Instance.Save();

            WebRequests.DownloadFile(_settings.GetDownloadUrl(), zipPath, error =>
            {
                try
                {
                    if (error != null)
                    {
                        Report(done, $"Download failed: {error}", false);
                        return;
                    }
                    var root = ExtractArchive(zipPath, Path.Combine(temp, "extracted"));
                    Directory.Move(root, destPath);
                    Report(done, $"Downloaded '{_settings.GitRef}' to 'Assets/xapi'.", true);
                    AssetDatabase.Refresh();
                }
                catch (Exception ex)
                {
                    Report(done, $"Download failed: {ex.Message}", false);
                }
                finally
                {
                    DeleteTempFolder(temp);
                }
            });
        }

        /// <summary>
        /// True while an official release is downloading or installing.
        /// </summary>
        internal bool IsInstalling { get; private set; }

        /// <summary>
        /// Most earlier releases downloaded as baselines when the installed release is unknown.
        /// </summary>
        private const int MaxBaselines = 5;

        /// <summary>
        /// Downloads an official release and installs it into 'Assets/xapi'. Earlier releases are downloaded too as
        /// baselines, so files you did not change are updated or removed and only files you changed become conflicts:
        /// the installed release if it is known, otherwise (or when reinstalling) up to <see cref="MaxBaselines"/> earlier ones.
        /// </summary>
        /// <param name="release">The release to install.</param>
        /// <param name="done">Gets a summary and whether it worked. Optional.</param>
        internal void InstallOfficialRelease(ReleaseInfo release, Action<string, bool> done)
        {
            if (IsInstalling || release == null)
                return;
            IsInstalling = true;

            var destination = DefaultValues.RepoPath;
            var baselines = BaselinesFor(release, destination);
            var temp = NewTempFolder();
            var releaseZip = Path.Combine(temp, "release.zip");

            void Finish(string message, bool ok)
            {
                DeleteTempFolder(temp);
                IsInstalling = false;
                Report(done, message, ok);
            }

            void Apply(List<string> baselineZips)
            {
                try
                {
                    var releaseRoot = ExtractArchive(releaseZip, Path.Combine(temp, "release"));
                    var baselineRoots = baselineZips
                        .Select((zip, i) => ExtractArchive(zip, Path.Combine(temp, "baseline" + i)))
                        .ToList();
                    var report = ReleaseInstaller.Install(releaseRoot, baselineRoots, destination, release.tag_name);
                    report.source = $"{DefaultValues.OfficialRepository} {release.tag_name}";
                    if (report.conflicts > 0)
                        File.WriteAllText(Path.Combine(destination, NewReportName()), JsonUtility.ToJson(report, true));

                    _settings.path = destination;
                    MainSettings.Instance.Save();
                    AssetDatabase.Refresh();

                    var summary = $"Installed {release.tag_name} into 'Assets/xapi': {report.copied} added, {report.updated} updated, " +
                                  $"{report.removed} removed, {report.skipped} unchanged.";
                    if (report.conflicts > 0)
                        summary += $" {report.conflicts} file(s) you changed differ from the release. Yours are kept, the release's " +
                                   "versions are next to them as '*.json-conflict'. Resolve them in the 'Use' step.";
                    Finish(summary, true);
                }
                catch (Exception ex)
                {
                    Finish($"Installing {release.tag_name} failed: {ex.Message}", false);
                }
            }

            // Download the baselines one after the other, skipping those that fail
            var baselineZips = new List<string>();
            void DownloadBaseline(int index)
            {
                if (index >= baselines.Count)
                {
                    Apply(baselineZips);
                    return;
                }
                var zip = Path.Combine(temp, $"baseline{index}.zip");
                WebRequests.DownloadFile(DefaultValues.OfficialArchiveUrl(baselines[index].tag_name), zip, error =>
                {
                    if (error == null)
                        baselineZips.Add(zip);
                    else
                        Debug.LogWarning($"[xAPI4Unity] Could not download the earlier release {baselines[index].tag_name}: {error}");
                    DownloadBaseline(index + 1);
                });
            }

            WebRequests.DownloadFile(DefaultValues.OfficialArchiveUrl(release.tag_name), releaseZip, error =>
            {
                if (error != null)
                {
                    Finish($"Download of {release.tag_name} failed: {error}", false);
                    return;
                }
                DownloadBaseline(0);
            });
        }

        /// <summary>
        /// The earlier releases to compare the source folder with: none for a fresh install, the installed release for an
        /// update, and up to <see cref="MaxBaselines"/> releases when the installed one is unknown or is installed again.
        /// </summary>
        private static List<ReleaseInfo> BaselinesFor(ReleaseInfo release, string destination)
        {
            var result = new List<ReleaseInfo>();
            var releases = OfficialReleases.Releases;
            if (releases == null || !Directory.Exists(Path.Combine(destination, ReleaseInstaller.DefinitionsFolder)))
                return result;

            var installed = releases.Find(ReleaseInstaller.ReadVersion(destination));
            if (installed != null && installed != release)
            {
                result.Add(installed);
                return result;
            }
            result.AddRange(releases.All.Where(r => r != release && !r.IsPreRelease).Take(MaxBaselines));
            return result;
        }

        /// <summary>
        /// Logs the outcome of an action and passes it on.
        /// </summary>
        private static void Report(Action<string, bool> done, string message, bool ok)
        {
            if (ok)
                Debug.Log($"[xAPI4Unity] {message}");
            else
                Debug.LogError($"[xAPI4Unity] {message}");
            done?.Invoke(message, ok);
        }

        /// <summary>
        /// Reports an error if the branch, tag or commit of the custom repository has characters git does not allow.
        /// </summary>
        /// <returns>True if it is valid.</returns>
        private bool CheckGitRef(Action<string, bool> done)
        {
            if (_settings.IsValidGitRef)
                return true;
            Report(done, $"'{_settings.GitRef}' is not a valid branch, tag or commit.", false);
            return false;
        }

        /// <summary>
        /// File name of a new merge report, such as <c>20261119143000-xapi-report.json</c>.
        /// </summary>
        private static string NewReportName() => DateTime.Now.ToString("yyyyMMddHHmmss") + "-xapi-report.json";

        /// <summary>
        /// Creates an empty folder in the project's 'Temp' folder. Unity does not import it, and it is on the same drive as 'Assets', so folders can be moved.
        /// </summary>
        private static string NewTempFolder()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var folder = Path.Combine(projectRoot, "Temp", "xAPI4Unity", Path.GetRandomFileName());
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Deletes a folder from <see cref="NewTempFolder"/>, ignoring errors.
        /// </summary>
        private static void DeleteTempFolder(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[xAPI4Unity] Could not delete '{folder}': {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts an archive and returns its root, the folder that GitHub and GitLab put everything into.
        /// </summary>
        private static string ExtractArchive(string zipPath, string extractPath)
        {
            Directory.CreateDirectory(extractPath);
            FileHelper.ExtractZip(zipPath, extractPath);
            return ReleaseInstaller.FindArchiveRoot(extractPath);
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
            if (_settings.IsOfficial)
            {
                // Install the latest release, after asking GitHub for it
                OfficialReleases.Refresh(true, () =>
                {
                    var latest = OfficialReleases.Releases?.Latest();
                    if (latest == null)
                        Debug.LogError($"[xAPI4Unity] Could not find an official release: {OfficialReleases.Error ?? "the list is empty."}");
                    else
                        InstallOfficialRelease(latest, null);
                });
                return;
            }

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
        internal void PullClone()
        {
            // A clone has a .git folder, a submodule has a .git file
            var gitPath = Path.Combine(_settings.path, ".git");
            if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
            {
                Debug.LogError($"[xAPI4Unity] '{_settings.path}' is not a git clone. Clone it first or use 'Patch into Assets' or 'Download zip'.");
                return;
            }

            _git.Pull(_settings.path);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Whether the Unity project is a git repository, so the definitions are added as a submodule.
        /// </summary>
        internal static bool ProjectHasGit
        {
            get
            {
                var parent = Directory.GetParent(Application.dataPath);
                return parent != null && Directory.Exists(Path.Combine(parent.ToString(), ".git"));
            }
        }

        /// <summary>
        /// Whether the repository is already cloned next to the local path.
        /// </summary>
        internal bool IsCloned
        {
            get
            {
                if (string.IsNullOrEmpty(_settings.path))
                    return false;
                SyncGit();
                return _git.IsCloned;
            }
        }

        /// <summary>
        /// Whether a git operation is running.
        /// </summary>
        internal bool IsBusy => _git.IsBusy;

        /// <summary>
        /// Clones the repository next to the local path, as a submodule if the project is a git repository,
        /// checks out a new local branch at the branch, tag or commit of the settings, and points the local path to the clone.
        /// </summary>
        internal void CloneRepository()
        {
            SyncGit();
            var gitRef = _settings.IsHead ? null : _settings.GitRef;
            if (ProjectHasGit)
                _git.SubmoduleClone(_settings.GetNewBranchName(), gitRef);
            else
                _git.Clone(_settings.GetNewBranchName(), gitRef);
            _settings.path = _git.RepoDirectory;
            MainSettings.Instance.Save();
        }

        /// <summary>
        /// Points the git helper to the repository URL and the parent folder of the local path.
        /// </summary>
        private void SyncGit()
        {
            _git.Url = _settings.repoUrl;
            _git.Directory = Path.GetDirectoryName(_settings.path);
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
                        CustomGUI.TextField("Branch, tag or commit", _settings.repoBranch, DefaultValues.RepoBranch);
                }
                CustomGUI.Description("Change this only if you work with a fork of the repository.");
            });
        }

        /// <summary>
        /// Renders the LocalSourceView interface and performs the respective actions based on user interactions.
        /// </summary>
        [Obsolete("The xAPI4Unity wizard (XapiWizardWindow) replaces this view. It will be removed in the next major version.")]
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
                    CustomGUI.Description(ProjectHasGit
                        ? "Adds the repository as a git submodule next to 'Path' and checks out a new local branch."
                        : "Clones the repository next to 'Path' and checks out a new local branch.");
                    CustomGUI.Disabled(IsBusy, () => RepositoryView(true));
                    CustomGUI.Space();

                    if (IsCloned)
                    {
                        if (CustomGUI.StateButton("Pull latest changes", IsBusy, "Pulling...", GUILayout.Height(28)))
                            PullClone();
                        return;
                    }

                    if (CustomGUI.StateButton(ProjectHasGit ? "Clone as submodule" : "Clone repository", IsBusy,
                            "Cloning...", GUILayout.Height(28)))
                    {
                        CloneRepository();
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