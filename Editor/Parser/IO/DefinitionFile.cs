using System;
using UnityEngine;

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
    
    public class DefinitionFile
    {
        public readonly string Path;
        public readonly string Content;
        public readonly string Name;
        public readonly string Context;
        public readonly DefinitionType DefinitionType;
        public readonly ExtensionType ExtensionType;
        
        public DefinitionFile(string path, string content)
        {
            Path = path.Replace('\\', '/');
            Content = content;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);

            var s = Path.Split('/');
            Context = s[0];
            DefinitionType = DetectDefinitionType(s[1]);

            if (DefinitionType == DefinitionType.Extension)
            {
                ExtensionType = DetectExtensionType(s[2]);
            }
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