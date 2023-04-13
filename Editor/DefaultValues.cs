using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using Unity.IO.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    public static class DefaultValues
    {
        public const string RepoUrl = "https://gitlab.com/learntech-rwth/xapi";
        //public const string FetcherAPIBase = "http://localhost:8083/";
        public const string FetcherAPIBase = "https://fetch.xapi.elearn.rwth-aachen.de/";
        //public const string FetcherAPIBase = "https://localhost:5000/";
        public const FetcherType FetcherType = Editor.FetcherType.CommandLine;
        public const string Namespace = "xAPI.Registry";
        public static string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public const PackageType PackageType = Editor.PackageType.SimpleAsset;
        public const SourceType SourceType = Editor.SourceType.Local;

        public const string GitLabBranch = "main";
        public const string GitLabProjectId = "";
        public const string GitLabAccessKey = "";

        public const bool AsmDef = true;
        public const string Version = "1.0.0";

        public const string DefaultAsmDefMeta = @"fileFormatVersion: 2
guid: 5b80f9e8c84ce594bbd1284cff4292dd
AssemblyDefinitionImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

        public static string ProjectRoot => Directory.GetCurrentDirectory();
        public static string AssetRoot => Path.Join(ProjectRoot, "Assets");

        public static string CliRoot => Path.Join(ProjectRoot, "Tools");
        
        // todo also osx and linux
        public static string CliPath => Path.Join(CliRoot, "xAPI_Definitions_Fetcher.CLI" + (Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : ""));
        public static string CliExeExtension => Path.GetExtension(CliPath).Replace(".", "");
        public static string OutputPath => AssetRoot;

        [InitializeOnLoadMethod]
        public static void EnsureFolders()
        {
            if (!Directory.Exists(CliRoot))
                Directory.CreateDirectory(CliRoot);
        }
    }
}