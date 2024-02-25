using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Internal;

namespace xAPI4Unity.Editor.Parser.IO
{
    public enum ExtensionType
    {
        Unknown,
        Activity,
        Context,
        Result
    }

    public enum DefinitionType
    {
        Unknown,
        Extension,
        Verb,
        Activity
    }

    public struct DefinitionFileContent
    {
        [JsonProperty("name")]
        public Dictionary<string, string> Names { get; set; }
        
        [JsonProperty("description")]
        public Dictionary<string, string> Descriptions { get; set; }

        [JsonProperty("relatedMatch", NullValueHandling = NullValueHandling.Ignore)]
        public string[] RelatedMatch { get; set; }
        
        [JsonProperty("exactMatch", NullValueHandling = NullValueHandling.Ignore)]
        public string[] ExactMatch { get; set; }
    }
    
    public class DefinitionFile
    {
        public readonly string Path;
        public readonly string JsonContent;
        public readonly DefinitionFileContent Content;
        public readonly string Name;
        public readonly string Context;
        public readonly string Category;
        
        public readonly DefinitionType DefinitionType;
        public readonly ExtensionType ExtensionType;
        
        public DefinitionFile(string path, string content)
        {
            Path = path.Replace('\\', '/');
            JsonContent = content;
            Name = System.IO.Path.GetFileNameWithoutExtension(path).ClearName();

            var s = Path.Split('/');
            Context = s[0];
            DefinitionType = DetectDefinitionType(s[1]);

            if (DefinitionType == DefinitionType.Extension)
            {
                ExtensionType = DetectExtensionType(s[2]);
            }

            if (!string.IsNullOrEmpty(JsonContent))
            {
                Content = JsonConvert.DeserializeObject<DefinitionFileContent>(JsonContent);
            }

            if (s.Length >= 4 && (DefinitionType == DefinitionType.Verb || DefinitionType == DefinitionType.Activity))
                Category = s[2];
            else if (s.Length >= 5 && DefinitionType == DefinitionType.Extension)
                Category = s[3];
        }
        

        private DefinitionType DetectDefinitionType(string type)
        {
            switch (type)
            {
                case "extensions":
                    return DefinitionType.Extension;
                case "verbs":
                    return DefinitionType.Verb;
                case "activities":
                    return DefinitionType.Activity;
                default:
                    return DefinitionType.Unknown;
            }
        }

        private ExtensionType DetectExtensionType(string type)
        {
            switch (type)
            {
                case "result":
                    return ExtensionType.Result;
                case "context":
                    return ExtensionType.Context;
                case "activity":
                    return ExtensionType.Activity;
                default:
                    return ExtensionType.Unknown;
            }
        }

        /// <summary>
        /// [File: Name={Name}, Path={Path}, Content={Content}]
        /// </summary>
        public override string ToString() => $"[File: Name=\"{Name}\", Context=\"{Context}\", DefinitionType=\"{DefinitionType}\", ExtensionType=\"{ExtensionType}\", Path=\"{Path}\", Content=\"{Content}\"]";
    }
}