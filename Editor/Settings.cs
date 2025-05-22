#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using xAPI4Unity.Editor.Parser;

namespace xAPI4Unity.Editor
{
    public interface ISettings
    {
        T Clone<T>();
        bool Equals(ISettings s);
    }

    [Serializable]
    public class Settings : ISettings
    {
        private static string SettingsKey = $"xAPI4Unity_{Application.dataPath.GetHashCode()}";

        public static Settings Instance = new Settings();
        private static Settings _savedInstance = new Settings();

        [SerializeReference] public FetcherSettings fetcher = new FetcherSettings();
        [SerializeReference] public LocalSourceSettings localSource = new LocalSourceSettings();

        [SerializeField] public string @namespace = DefaultValues.Namespace;

        public string AsmDefFilename => @namespace + ".asmdef"; 
        public string AsmDefMetaFilename => AsmDefFilename + ".meta"; 
        
        public void Reset()
        {
            EditorPrefs.DeleteKey(SettingsKey);
            Instance = new Settings();
            _savedInstance = Instance.Clone<Settings>();
        }

        public T Clone<T>()
        {
            var s = new Settings
            {
                fetcher = fetcher.Clone<FetcherSettings>(),
                localSource = localSource.Clone<LocalSourceSettings>(),
                @namespace = @namespace,
            };
            return (T)(object)s;
        }

        public bool Equals(ISettings so)
        {
            var s = so as Settings;
            return fetcher.Equals(s.fetcher) && localSource.Equals(s.localSource);
        }

        public void Load()
        {
            var data = PlayerPrefs.GetString(SettingsKey, JsonUtility.ToJson(Instance, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, Instance);
#if UNITY_2019
            if (Instance == null)
                Instance = new Settings();
#else
            Instance ??= new Settings();
#endif
            _savedInstance = Instance.Clone<Settings>();
        }

        public void Save()
        {
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            PlayerPrefs.SetString(SettingsKey, data);
            _savedInstance = Instance.Clone<Settings>();
        }

        public static bool IsDirty() => !Instance.Equals(_savedInstance);
    }

    [Serializable]
    public class FetcherSettings : ISettings
    {
        public class FetcherArguments : Dictionary<string, string>
        {
        }
        [SerializeField] public string localPath = DefaultValues.HomeDirectory;
        [SerializeField] public bool watcherEnabled = DefaultValues.WatcherEnabled;

        public T Clone<T>()
        {
            var f = new FetcherSettings
            {
                localPath = localPath,
                watcherEnabled = watcherEnabled
            };
            return (T)(object)f;
        }

        public bool Equals(ISettings s)
        {
            var f = s as FetcherSettings;
            return watcherEnabled == f.watcherEnabled && localPath == f.localPath;
        }
    }

    [Serializable]
    public class LocalSourceSettings : ISettings
    {
#if !UNITY_2021_1_OR_NEWER
        [SerializeField] public string path = Path.Combine(DefaultValues.AssetRoot, "xapi");
#else
        [SerializeField] public string path = Path.Join(DefaultValues.AssetRoot, "xapi");
#endif
        [SerializeField] public string repoUrl = DefaultValues.RepoUrl;
        [SerializeField] public string repoBranch = default;
        [SerializeField] public int gitCloneAmount = 0;
        
        public T Clone<T>()
        {
            return (T)(object)(new LocalSourceSettings() { path = path });
        }

        public bool Equals(ISettings s)
            => (s as LocalSourceSettings)?.path == path;
        
        public string GetNewBranchName() => $"xapi-4-unity-{Application.productName.ClearName()}-{gitCloneAmount++}";
    }
}
#endif