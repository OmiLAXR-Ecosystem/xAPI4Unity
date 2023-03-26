using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    public static class DefaultValues
    {
        public const string RepoUrl = "https://gitlab.com/learntech-rwth/xapi";
        //public const string FetcherAPIBase = "https://fetch.xapi.elearn.rwth-aachen.de/";
        public const string FetcherAPIBase = "https://localhost:5000/";
        public const FetcherType FetcherType = Editor.FetcherType.WebService;
        public const string Namespace = "EduXR.xAPI.Registry";
        public static string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public const PackageType PackageType = Editor.PackageType.SimpleAsset;
        public const SourceType SourceType = Editor.SourceType.Local;

        public const string GitLabBranch = "main";
        public const string GitLabProjectId = "";
        public const string GitLabAccessKey = "";

        public static Dictionary<PlatformID, string> Filenames = new Dictionary<PlatformID, string>()
        {
            [PlatformID.MacOSX] = "osx-x64",
            [PlatformID.Unix] = "unix-x64",
            [PlatformID.Win32NT] = "win-x64.exe"
        };

        public static string ExeFilename
        {
            get
            {
                var platform = Environment.OSVersion.Platform;
                return Filenames.ContainsKey(platform) ? Filenames[platform] : "win-x64.exe";
            }
        }

        public static string ProjectRoot => Directory.GetCurrentDirectory();
        public static string AssetRoot => Path.Join(ProjectRoot, "Assets");
        public static string PackageRoot => Path.Join(AssetRoot, "xAPI4Unity");
        public static string CliRoot => Path.Join(PackageRoot, "CLI");
        
        // todo also osx and linux
        public static string CliPath => Path.Join(CliRoot, ExeFilename);
        public static string CliExeExtension => Path.GetExtension(CliPath);

        public static string OutputPath => AssetRoot;

        public static void EnsureFolders()
        {
        }
    }
}