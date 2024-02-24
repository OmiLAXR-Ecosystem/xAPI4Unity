using System;
using System.Collections.Generic;
using System.Linq;

namespace xAPI.Registry
{
    /// <summary>
    /// Version: 1.0.0
    /// </summary>
    public abstract class xAPI_Definition
    {
        public readonly string Context;
        public readonly string Key;
        public readonly Dictionary<string, string> Names;
        public readonly Dictionary<string, string> Descriptions;

        public xAPI_Definition(string context, string key, Dictionary<string, string> names,
            Dictionary<string, string> descriptions)
        {
            Context = context;
            Key = key;
            Names = names;
            Descriptions = descriptions;
        }

        public string GetName(string language)
        {
            if (!Names.ContainsKey(language))
            {
                throw new ArgumentException($"There is no name for the language {language}.");
            }

            return Names[language];
        }

        public string GetDescription(string language)
        {
            if (!Descriptions.ContainsKey(language))
            {
                throw new ArgumentException($"There is no description for the language {language}.");
            }

            return Descriptions[language];
        }

        public KeyValuePair<string, string> GetNameDescription(string language)
        {
            var namesContainLang = Names.ContainsKey(language);
            var descsContainLang = Descriptions.ContainsKey(language);

            if (!namesContainLang)
            {
                if (!descsContainLang)
                {
                    throw new ArgumentException($"There is no name and no description for the language {language}.");
                }

                throw new ArgumentException($"There is no name for the language {language}.");
            }
            else if (!descsContainLang)
            {
                throw new ArgumentException($"There is no description for the language {language}.");
            }

            return new KeyValuePair<string, string>(Names[language], Descriptions[language]);
        }

        public string[] GetLanguages() => Names.Keys.Union(Descriptions.Keys).ToArray();

        public virtual string GetPath() => Key;

        public string CreateValidId(string uri)
        {
            var path = GetPath();
            // remove slash at end of uri
            if (uri[uri.Length - 1] == '/')
            {
                uri = uri.Substring(0, uri.Length - 1);
            }

            // remove slash at start of path
            if (path[0] == '/')
            {
                path = path.Substring(1);
            }

            return $"{uri}/{path}";
        }

        private static string DictToString(Dictionary<string, string> dict) =>
            dict == null ? string.Empty : string.Join(",", dict);

        public override string ToString() =>
            $"[{GetType().Name}: name=[{DictToString(Names)}], description=[{DictToString(Descriptions)}]]";
    }
}