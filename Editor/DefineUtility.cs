using UnityEditor;

namespace xAPI4Unity.Editor
{
    public static class DefineUtility
    {
        public static void RemoveXapiRegistryExistsDefine(string define)
        {
#if UNITY_2021_2_OR_NEWER
            var groups = new[]
            {
                UnityEditor.Build.NamedBuildTarget.Standalone,
                UnityEditor.Build.NamedBuildTarget.Android,
                UnityEditor.Build.NamedBuildTarget.iOS,
                UnityEditor.Build.NamedBuildTarget.WebGL
            };
            foreach (var namedBuildTarget in groups)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
                var defineList = new System.Collections.Generic.List<string>(defines.Split(';'));
                if (defineList.Remove(define))
                {
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.Join(";", defineList));
                }
            }
#else
        var groups = new[]
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.WebGL
        };
        foreach (var group in groups)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defineList = new System.Collections.Generic.List<string>(defines.Split(';'));
            if (defineList.Remove(define))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defineList));
            }
        }
#endif
        }
        
        public static void AddXapiRegistryExistsDefine(string define)
        {
#if UNITY_2021_2_OR_NEWER
            var groups = new[]
            {
                UnityEditor.Build.NamedBuildTarget.Standalone,
                UnityEditor.Build.NamedBuildTarget.Android,
                UnityEditor.Build.NamedBuildTarget.iOS,
                UnityEditor.Build.NamedBuildTarget.WebGL
            };
            foreach (var namedBuildTarget in groups)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
                if (!defines.Contains(define))
                {
                    if (!string.IsNullOrEmpty(defines))
                        defines += ";";
                    defines += define;
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
                }
            }
#else
        var groups = new[]
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.WebGL
        };
        foreach (var group in groups)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (!defines.Contains(define))
            {
                if (!string.IsNullOrEmpty(defines))
                    defines += ";";
                defines += define;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
            }
        }
#endif
        }
    }
}