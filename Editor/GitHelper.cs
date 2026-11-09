/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using Dir = System.IO.Directory;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Handles Git-related operations such as cloning repositories and managing submodules for xAPI4Unity.
    /// </summary>
    public class GitHelper
    {
        private Process _processHandle; // Maintains the process handle for ongoing Git operations.
        public string Url; // The repository URL.
        public string Directory; // The local directory where the repository is cloned.
        public bool IsBusy { get; private set; } // Indicates if a Git operation is in progress.
        
        /// <summary>
        /// Gets the path to the "xapi" directory in the repository.
        /// </summary>
        public string RepoDirectory => Path.Combine(Directory, "xapi");

        /// <summary>
        /// Checks if the repository is valid and contains the required structure.
        /// </summary>
        public bool IsCloned => IsValidRepo(RepoDirectory);

        /// <summary>
        /// Initializes a new instance of <see cref="GitHelper"/> with the specified repository URL and local directory.
        /// </summary>
        /// <param name="repoUrl">The repository URL (e.g., SSH or HTTPS).</param>
        /// <param name="directory">The local directory for cloning the repository.</param>
        public GitHelper(string repoUrl, string directory)
        {
            Url = repoUrl;
            Directory = directory;
        }

        /// <summary>
        /// Determines if the provided repository path contains a "definitions" folder.
        /// </summary>
        /// <param name="repoPath">The path to the repository.</param>
        /// <returns>True if the "definitions" folder exists, otherwise false.</returns>
        public static bool HasDefinitionsFolder(string repoPath) => Dir.Exists(Path.Combine(repoPath, "definitions"));

        /// <summary>
        /// Validates if the provided path is a valid Git repository.
        /// </summary>
        /// <param name="repoPath">The path to the repository.</param>
        /// <returns>True if valid, otherwise false.</returns>
        public static bool IsValidRepo(string repoPath) =>
            Dir.Exists(repoPath) && HasDefinitionsFolder(repoPath);

        /// <summary>
        /// Clones the repository as a submodule into the specified directory.
        /// Optionally checks out a specific branch.
        /// </summary>
        /// <param name="branch">The branch to checkout after cloning. Optional.</param>
        public void SubmoduleClone(string branch = default)
        {
            TerminateOngoingProcess();

            IsBusy = true;

            // Run the "submodule add" command
            var gitCloneProcess = ProcessHelper.Cmd("git", $"submodule add {Url}", false, Directory);
            gitCloneProcess.WaitForExit();

            // Optionally checkout the branch
            if (!string.IsNullOrEmpty(branch))
            {
                var gitCheckoutProcess = ProcessHelper.Cmd("git", $"checkout -b {branch}", false, RepoDirectory);
                gitCheckoutProcess.WaitForExit();
            }

            IsBusy = false;
        }

        /// <summary>
        /// Clones the repository into the specified directory.
        /// Optionally checks out a specific branch.
        /// </summary>
        /// <param name="branch">The branch to checkout after cloning. Optional.</param>
        public void Clone(string branch = default)
        {
            TerminateOngoingProcess();

            IsBusy = true;

            // Run the "clone" command
            var gitCloneProcess = ProcessHelper.Cmd("git", $"clone {Url}", false, Directory);
            gitCloneProcess.WaitForExit();

            // Optionally checkout the branch
            if (!string.IsNullOrEmpty(branch))
            {
                var gitCheckoutProcess = ProcessHelper.Cmd("git", $"checkout -b {branch}", false, RepoDirectory);
                gitCheckoutProcess.WaitForExit();
            }

            IsBusy = false;
        }

        /// <summary>
        /// Pulls the latest changes into an existing clone.
        /// </summary>
        /// <param name="repoPath">The path to the cloned repository.</param>
        internal void Pull(string repoPath)
        {
            TerminateOngoingProcess();

            IsBusy = true;

            var gitPullProcess = ProcessHelper.Cmd("git", "pull", false, repoPath);
            gitPullProcess.WaitForExit();

            IsBusy = false;
        }

        /// <summary>
        /// Terminates any ongoing Git process.
        /// </summary>
        private void TerminateOngoingProcess()
        {
            if (_processHandle != null)
            {
                _processHandle.Kill();
                _processHandle = null;
            }
        }
    }
}
#endif