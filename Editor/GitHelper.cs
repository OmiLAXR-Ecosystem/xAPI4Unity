#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;

using Dir = System.IO.Directory;

namespace xAPI4Unity.Editor
{
    public class GitHelper
    {
        private Process _processHandle;
        public string Url;
        public string Directory;
        public bool IsBusy { get; private set; }
#if UNITY_2019
        public string RepoDirectory => Path.Combine(Directory, "xapi");
#else
        public string RepoDirectory => Path.Join(Directory, "xapi");
#endif
        public bool IsCloned => IsValidRepo(RepoDirectory);
        
        public GitHelper(string repoUrl, string directory)
        {
            Url = repoUrl;
            Directory = directory;
        }

        #if UNITY_2019
        public static bool HasDefinitionsFolder(string repoPath) => Dir.Exists(Path.Combine(repoPath, "definitions"));
        #else
        public static bool HasDefinitionsFolder(string repoPath) => Dir.Exists(Path.Join(repoPath, "definitions"));
        #endif
        
        public static bool IsValidRepo(string repoPath) =>
            Dir.Exists(repoPath) && HasDefinitionsFolder(repoPath);

        public void SubmoduleClone(string branch = default)
        {
            if (_processHandle != null)
            {
                _processHandle.Kill();
                _processHandle = null;
            }
            
            IsBusy = true;
            var isUnixOrMac = Environment.OSVersion.Platform == PlatformID.Unix ||
                              Environment.OSVersion.Platform == PlatformID.MacOSX;
            
            var gitCloneProcess = ProcessHelper.Cmd("git", $"submodule add {Url}", false, Directory);
            gitCloneProcess.WaitForExit();

            if (branch != default)
            {
           
                var gitCheckoutProcess = ProcessHelper.Cmd("git",$" checkout -b {branch}", false, RepoDirectory);
                gitCheckoutProcess.WaitForExit();
            }

            IsBusy = false;
        }
        
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
            gitCloneProcess.WaitForExit();

            if (branch != default)
            {
           
                var gitCheckoutProcess = ProcessHelper.Cmd("git",$" checkout -b {branch}", false, RepoDirectory);
                gitCheckoutProcess.WaitForExit();
            }

            IsBusy = false;
        }
    }
}
#endif