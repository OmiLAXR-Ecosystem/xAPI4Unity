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
        /// <summary>
        /// The git process that is running, if any.
        /// </summary>
        private Process _processHandle;
        /// <summary>
        /// The repository URL.
        /// </summary>
        public string Url;
        /// <summary>
        /// The local folder of the clone.
        /// </summary>
        public string Directory;
        /// <summary>
        /// True while a git command runs.
        /// </summary>
        public bool IsBusy { get; private set; }
        
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
        public void SubmoduleClone(string branch = default) => SubmoduleClone(branch, null);

        /// <summary>
        /// Clones the repository as a submodule into the specified directory and checks out a new local
        /// branch at a branch, tag or commit of the repository.
        /// </summary>
        /// <param name="branch">Name of the new local branch. Optional.</param>
        /// <param name="gitRef">Branch, tag or commit to start from. Null, empty or <c>HEAD</c> for the default branch.</param>
        public void SubmoduleClone(string branch, string gitRef)
            => CloneAndCheckout($"submodule add {Quote(Url)} xapi", branch, gitRef);

        /// <summary>
        /// Clones the repository into the specified directory.
        /// Optionally checks out a specific branch.
        /// </summary>
        /// <param name="branch">The branch to checkout after cloning. Optional.</param>
        public void Clone(string branch = default) => Clone(branch, null);

        /// <summary>
        /// Clones the repository into the specified directory and checks out a new local branch at a branch,
        /// tag or commit of the repository.
        /// </summary>
        /// <param name="branch">Name of the new local branch. Optional.</param>
        /// <param name="gitRef">Branch, tag or commit to start from. Null, empty or <c>HEAD</c> for the default branch.</param>
        public void Clone(string branch, string gitRef)
            => CloneAndCheckout($"clone {Quote(Url)} xapi", branch, gitRef);

        /// <summary>
        /// Runs a clone command in <see cref="Directory"/>, then checks out the local branch in <see cref="RepoDirectory"/>.
        /// </summary>
        private void CloneAndCheckout(string cloneArguments, string branch, string gitRef)
        {
            if (!string.IsNullOrEmpty(gitRef) && !Settings.LocalSourceSettings.IsValidRef(gitRef))
            {
                UnityEngine.Debug.LogError($"[xAPI4Unity] '{gitRef}' is not a valid branch, tag or commit.");
                return;
            }

            TerminateOngoingProcess();
            IsBusy = true;
            try
            {
                if (ProcessHelper.Run("git", cloneArguments, Directory, out var output) != 0)
                {
                    UnityEngine.Debug.LogError($"[xAPI4Unity] git {cloneArguments} failed:\n{output}");
                    return;
                }
                CheckoutLocalBranch(branch, gitRef);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Checks out a new local branch at the start point of <paramref name="gitRef"/>. Without a branch name,
        /// checks out the start point itself.
        /// </summary>
        private void CheckoutLocalBranch(string branch, string gitRef)
        {
            var startPoint = ResolveStartPoint(gitRef);
            string arguments;
            if (!string.IsNullOrEmpty(branch))
                arguments = startPoint != null ? $"checkout -b {branch} {startPoint}" : $"checkout -b {branch}";
            else if (startPoint != null)
                arguments = $"checkout {startPoint}";
            else
                return;

            if (ProcessHelper.Run("git", arguments, RepoDirectory, out var output) != 0)
                UnityEngine.Debug.LogError($"[xAPI4Unity] git {arguments} failed:\n{output}");
        }

        /// <summary>
        /// Where the local branch starts. A branch of the remote becomes <c>origin/&lt;branch&gt;</c>, so the local
        /// branch tracks it and <see cref="Pull"/> works. HEAD becomes the remote's default branch. Tags and commits stay as they are.
        /// </summary>
        /// <returns>The start point, or null for the commit the clone checked out.</returns>
        private string ResolveStartPoint(string gitRef)
        {
            if (string.IsNullOrEmpty(gitRef) || string.Equals(gitRef, "HEAD", System.StringComparison.OrdinalIgnoreCase))
            {
                return ProcessHelper.Run("git", "symbolic-ref --quiet --short refs/remotes/origin/HEAD", RepoDirectory, out var head) == 0 && head.Length > 0
                    ? head
                    : null;
            }
            return ProcessHelper.Run("git", $"rev-parse --verify --quiet refs/remotes/origin/{gitRef}", RepoDirectory, out _) == 0
                ? "origin/" + gitRef
                : gitRef;
        }

        /// <summary>
        /// Quotes a command-line argument such as a URL.
        /// </summary>
        private static string Quote(string argument) => "\"" + (argument ?? "").Replace("\"", "") + "\"";

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