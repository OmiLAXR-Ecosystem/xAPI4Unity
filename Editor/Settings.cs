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
        private static string SettingsKey
        {
            get
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Application.dataPath));
                var shortHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return $"xAPI4Unity::{shortHash}";
            }
        }
        
        private static Settings _instance;
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                    _instance.Load(); // <- ensures values come from EditorPrefs
                }
                return _instance;
            }
        }
        
        [SerializeReference] public FetcherSettings fetcher = new FetcherSettings();
        [SerializeReference] public LocalSourceSettings localSource = new LocalSourceSettings();

        [SerializeField] public string @namespace = DefaultValues.Namespace;
        [SerializeField] public bool isOpeningOnStartup = true;

        public string AsmDefFilename => @namespace + ".asmdef";
        public string AsmDefMetaFilename => AsmDefFilename + ".meta";

        public void Reset()
        {
            EditorPrefs.DeleteKey(SettingsKey);
            _instance = new Settings();
        }

        public T Clone<T>()
        {
            var s = new Settings
            {
                fetcher = fetcher.Clone<FetcherSettings>(),
                localSource = localSource.Clone<LocalSourceSettings>(),
                @namespace = @namespace,
                isOpeningOnStartup = isOpeningOnStartup
            };
            return (T)(object)s;
        }

        public bool Equals(ISettings so)
        {
            if (so == null) return false;
            var s = so as Settings;
            return s!.isOpeningOnStartup == isOpeningOnStartup &&
                   s.@namespace == @namespace &&
                   s.AsmDefFilename == AsmDefFilename &&
                   s.AsmDefMetaFilename == AsmDefMetaFilename &&
                   fetcher.Equals(s.fetcher) &&
                   localSource.Equals(s.localSource);
        }

        public void Load()
        {
            var defaultJson = JsonUtility.ToJson(Instance, false);
            var data = EditorPrefs.GetString(SettingsKey, defaultJson);
            // Debug.Log($"[xAPI4Unity] Load settings from EditorPrefs: {data}");

            try
            {
                JsonUtility.FromJsonOverwrite(data, Instance);
            }
            catch
            {
                // Debug.LogError($"[xAPI4Unity] Failed to load settings: {e.Message}");
                _instance = new Settings(); // Fallback if corrupted
            }

            // Ensure subfields are not null (Unity does not deserialize nulls well)
#if UNITY_2020_1_OR_NEWER
            Instance.fetcher ??= new FetcherSettings();
            Instance.localSource ??= new LocalSourceSettings();
#else
    if (Instance.fetcher == null)
        Instance.fetcher = new FetcherSettings();
    if (Instance.localSource == null)
        Instance.localSource = new LocalSourceSettings();
#endif
        }
        
        public void Save()
        {
            var json = JsonUtility.ToJson(Instance, false);
            EditorPrefs.SetString(SettingsKey, json);
            // Debug.Log($"[xAPI4Unity] Saved:\n{json}");
        }
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
        [SerializeField] public string path = DefaultValues.RepoPath;
        [SerializeField] public int selectedOptionIndex = 0;
        [SerializeField] public string repoUrl = DefaultValues.RepoUrl;
        [SerializeField] public string repoDownloadUrl = DefaultValues.RepoDownloadUrl;
        [SerializeField] public string repoBranch = DefaultValues.RepoBranch;
        [SerializeField] public int gitCloneAmount = 0;

        public string DownloadFilename => "xapi-" + repoBranch;
        public string GetDownloadUrl() => $"{repoDownloadUrl}/-/archive/{repoBranch}/{DownloadFilename}.zip";
        
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