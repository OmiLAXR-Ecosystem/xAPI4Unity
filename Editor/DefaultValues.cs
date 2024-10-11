#if UNITY_EDITOR
using System;
using System.IO;

namespace xAPI4Unity.Editor
{
    internal static class DefaultValues
    {
        public const bool WatcherEnabled = true;
        public const string RepoUrl = "git@gitlab.com:learntech-rwth/xapi.git";
        public const string Namespace = "xAPI.Registry";
        public static string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static string ProjectRoot => Directory.GetCurrentDirectory();
        public static string AssetRoot => Path.Join(ProjectRoot, "Assets");
    }
}
#endif