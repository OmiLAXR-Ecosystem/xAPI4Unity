#if UNITY_EDITOR
using System.IO;

namespace xAPI4Unity.Editor.Parser
{
    internal static class CoreFiles
    {
        private static string[] Files = new[]
        {
            "xAPI_Activities.cs", "xAPI_Activity.cs", "xAPI_Actor.cs", "xAPI_Body.cs",
            "xAPI_Context.cs", "xAPI_Definition.cs", "xAPI_Extension.cs", "xAPI_Extensions.cs",
            "xAPI_Verb.cs", "xAPI_Verbs.cs"
        };

        const string RelativePackagePath = "Packages/com.rwth.unity.omilaxr.xapi4unity";
        private static string RelativeEditorPath => Path.Combine(RelativePackagePath, "Editor");
        private static string RelativeAssetPath => Path.Combine(RelativeEditorPath, "Assets");
        private static string CorePath => Path.GetFullPath(Path.Combine(RelativeAssetPath, "Core"));

        public static void CopyFiles(string @namespace, string destination)
        {
            foreach (var f in Files)
            {
                var fullPath = Path.Combine(CorePath, f + ".tmpl");
                var targetPath = Path.Combine(destination, f);
                var content = File.ReadAllText(fullPath);
                content = content.Replace("xAPI.Registry", @namespace);
                File.WriteAllText(targetPath, content);
            }
        }

        public static void CreateAsmDefFile(string @namespace, string destination)
        {
            var path = Path.Combine(destination, @namespace + ".asmdef");
            var metaPath = Path.Combine(destination, @namespace + ".asmdef.meta");
            
            if (!File.Exists(path))
                File.WriteAllText(path, AsmDefFile.Replace("xAPI.Registry", @namespace));
            
            if (!File.Exists(metaPath))
                File.WriteAllText(metaPath, AsmDefFileMeta);
        }

        const string AsmDefFile =
            "{\n  \"name\": \"xAPI.Registry\",\n  \"rootNamespace\": \"xAPI.Registry\",\n  \"references\": [\n    \"GUID:9b29afca86c7f314eb2f13b22ca1967d\"\n  ],\n  \"includePlatforms\": [],\n  \"excludePlatforms\": [],\n  \"allowUnsafeCode\": false,\n  \"overrideReferences\": false,\n  \"precompiledReferences\": [],\n  \"autoReferenced\": true,\n  \"defineConstraints\": [],\n  \"versionDefines\": [],\n  \"noEngineReferences\": false\n}";

        const string AsmDefFileMeta = "fileFormatVersion: 2\nguid: 5b80f9e8c84ce594bbd1284cff4292dd\nAssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n\n";
    }
}
#endif