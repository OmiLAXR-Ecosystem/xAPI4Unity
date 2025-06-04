using UnityEditor;
using System.Linq;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build; 
#endif

namespace xAPI4Unity.Editor
{
    [InitializeOnLoad]
    internal static class Startup
    {
        private static bool DefineExist(string define)
        {
            string[] defines;

#if UNITY_2021_2_OR_NEWER
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            defines = PlayerSettings
                .GetScriptingDefineSymbols(namedBuildTarget)
                .Split(';');
#else
        defines = PlayerSettings
            .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
            .Split(';');
#endif

            return defines.Contains(define);
        }
        
        static Startup()
        {
            var settings = Settings.Instance;
            if (!settings.isOpeningOnStartup)
                return;
            EditorApplication.delayCall += () =>
            {
                if (!DefineExist("XAPI_REGISTRY_EXISTS") || !GitHelper.HasDefinitionsFolder(settings.localSource.path))
                    FetcherWindow.ShowWindow();
            };
        }
    }
}