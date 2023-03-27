using System;
using System.Diagnostics;
using System.IO;

using Dir = System.IO.Directory;

namespace EduXR.xAPI.Editor
{
    public class GitHelper
    {
        private Process _processHandle;
        public readonly string Url;
        public readonly string Directory;
        public bool IsBusy { get; private set; }
        public string RepoDirectory => Path.Join(Directory, "xapi");
        public bool IsCloned => IsValidRepo(RepoDirectory);
        
        public GitHelper(string repoUrl, string directory)
        {
            Url = repoUrl;
            Directory = directory;
        }

        public static bool IsValidRepo(string repoPath) =>
            Dir.Exists(repoPath) && Dir.Exists(Path.Join(repoPath, "definitions"));
        
        public void Clone(string branch = default)
        {
            if (_processHandle != null)
            {
                _processHandle.Kill();
                _processHandle = null;
            }

            IsBusy = true;
            var isUnixOrMac = Environment.OSVersion.Platform == PlatformID.Unix ||
                              Environment.OSVersion.Platform == PlatformID.MacOSX;
            
            var gitCloneProcess = ProcessHelper.Cmd("git", $"clone {Url}", false, Directory);
            gitCloneProcess.WaitForExit(5000);

            if (branch != default)
            {
           
                var gitCheckoutProcess = ProcessHelper.Cmd("git",$" checkout -b {branch}", false, RepoDirectory);
                gitCheckoutProcess.WaitForExit();
            }

            IsBusy = false;
        }
    }
}