#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace xAPI4Unity.Editor.Parser.Definitions {
    /// <summary>
    /// Class that represents an xAPI definition
    /// </summary>
    internal abstract class xAPIDefinition {
        /// <summary>
        /// Context of the definition
        /// </summary>
        public string Context
        {
            get;
            set;
        }
        /// <summary>
        /// (File) name of the definition
        /// </summary>
        public string DefName
        {
            get;
            set;
        }
        /// <summary>
        /// Names of the definition by language
        /// </summary>
        [JsonProperty("name")]
        public readonly IDictionary<string, string> Names;
        /// <summary>
        /// Descriptions by the definition by language
        /// </summary>
        [JsonProperty("description")]
        public readonly IDictionary<string, string> Descriptions;

        /// <summary>
        /// Creates an empty xAPIDefinition
        /// </summary>
        public xAPIDefinition() { }

        /// <summary>
        /// Creates an xAPIDefinition.
        /// </summary>
        /// <param name="context">Context of the definition</param>
        /// <param name="name">(File) name of the definition</param>
        /// <param name="names">Names of the definition by language</param>
        /// <param name="descriptions">Descriptions of the definition by language</param>
        public xAPIDefinition(string context, string name, IDictionary<string, string> names, IDictionary<string, string> descriptions) {
            Context = context;
            DefName = name;
            Names = names;
            Descriptions = descriptions;
        }

        /// <summary>
        /// Gets the name of the definition by language
        /// </summary>
        public string GetName(string language) {
            if (!Names.ContainsKey(language)) {
                throw new ArgumentException("There is no name for the language " + language + ".");
            }
            return Names[language];
        }

        /// <summary>
        /// Gets the description of the definition by language
        /// </summary>
        public string GetDescription(string language, bool replaceLang = false) {
            if (Descriptions.ContainsKey(language)) return Descriptions[language];
            if (replaceLang) {
                if (Descriptions.Count > 0) {
                    return Descriptions.First().Value;
                }
            } else {
                throw new ArgumentException("There is no description for the language " + language + ".");
            }
            return Descriptions[language];
        }
        
        /// <summary>
        /// Gets the name and the description of the definition by language
        /// </summary>
        public KeyValuePair<string, string> GetNameDescription(string language) {
            var namesContainLang = Names.ContainsKey(language);
            var descsContainLang = Descriptions.ContainsKey(language);

            if (!namesContainLang) {
                if (!descsContainLang) {
                    throw new ArgumentException("There is no name and no description for the language " + language + ".");
                }
                throw new ArgumentException("There is no name for the language " + language + ".");
            } else if (!descsContainLang) {
                throw new ArgumentException("There is no description for the language " + language + ".");
            }
            return new KeyValuePair<string, string>(Names[language], Descriptions[language]);
        }

        /// <summary>
        /// Get the languages of the definition
        /// </summary>
        public string[] GetLanguages() => Names.Keys.Union(Descriptions.Keys).ToArray();

        public virtual string GetDefinitionGroup() => throw new NotImplementedException();
    }
}
#endif