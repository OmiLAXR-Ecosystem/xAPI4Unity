using System;
using System.IO;

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

        public static string ExeExtension => Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX
            ? ""
            : "exe";

        public static string ProjectRoot => Directory.GetCurrentDirectory();
        public static string AssetRoot => Path.Join(ProjectRoot, "Assets");
        public static string ToolsRoot => Path.Join(ProjectRoot, "Tools");
        public static string ExeRoot => Path.Join(ToolsRoot, "xAPI_Fetcher");
        public static string ExePath => Path.Join(ExeRoot, "xAPI_Definitions_Fetcher" + ExeExtension);

        public static string OutputPath => AssetRoot;

        public static void EnsureFolders()
        {
            if (!Directory.Exists(ToolsRoot))
                Directory.CreateDirectory(ToolsRoot);
            if (!Directory.Exists(ExeRoot))
                Directory.CreateDirectory(ExeRoot);
        }
    }
}