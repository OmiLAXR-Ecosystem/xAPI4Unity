#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Definitions {
    /// <summary>
    /// An xAPI context
    /// </summary>
    internal class xAPIContext
    {
        /// <summary>
        /// Name of the context
        /// </summary>
        public readonly string Name;
        public readonly string Description;
        /// <summary>
        /// Activities in the context
        /// </summary>
        public xAPIDefinitionsList<xAPIActivity> Activities { get; set; } = new xAPIDefinitionsList<xAPIActivity>();
        /// <summary>
        /// Verbs in the context
        /// </summary>
        public xAPIDefinitionsList<xAPIVerb> Verbs { get; set; } = new xAPIDefinitionsList<xAPIVerb>();
        /// <summary>
        /// Extensions in the context
        /// </summary>
        public IDictionary<string, xAPIDefinitionsList<xAPIExtension>> Extensions { get; set; } = new Dictionary<string, xAPIDefinitionsList<xAPIExtension>>();
        /// <summary>
        /// Extension types in the context
        /// </summary>
        public IEnumerable<string> ExtensionTypes => Extensions.Keys;

        /// <summary>
        /// Creates an xAPIContext
        /// </summary>
        /// <param name="name">Name of the context</param>
        /// <param name="description"></param>
        public xAPIContext(string name, string description = "") {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Adds an activity to the context
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <returns>True, if it was successfully added, false, if not</returns>
        public bool AddActivity(xAPIActivity activity) {
            return Activities.AddDefinition(activity);
        }

        /// <summary>
        /// Adds sub activities to the context
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="activities">Activities</param>
        /// <returns>Number of added activities</returns>
        public bool AddSubActivity(string name, xAPIActivity activity) {
            return Activities.AddSubDefinition(name, activity);
        }

        /// <summary>
        /// Adds a verb to the context
        /// </summary>
        /// <param name="verb">Verb</param>
        /// <returns>True, if it was successfully added, false, if not</returns>
        public bool AddVerb(xAPIVerb verb) {
            return Verbs.AddDefinition(verb);
        }

        /// <summary>
        /// Adds sub verbs to the context
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="verbs">Verbs</param>
        /// <returns>Number of added verbs</returns>
        public bool AddSubVerb(string name, xAPIVerb verb) {
            return Verbs.AddSubDefinition(name, verb);
        }

        /// <summary>
        /// Adds an extension to the context
        /// </summary>
        /// <param name="extension">Extension</param>
        /// <returns>True, if it was successfully added, false, if not</returns>
        public bool AddExtension(string type, xAPIExtension extension) {
            if (!Extensions.ContainsKey(type)) {
                Extensions.Add(type, new xAPIDefinitionsList<xAPIExtension>());
            }
            return Extensions[type].AddDefinition(extension);
        }

        /// <summary>
        /// Adds sub extensions to the context
        /// </summary>
        /// <param name="type">Extension type</param>
        /// <param name="name">Name</param>
        /// <param name="extensions">Extensions</param>
        /// <returns>Number of added extensions</returns>
        public bool AddSubExtension(string type, string name, xAPIExtension extension) {
            if (!Extensions.ContainsKey(type)) {
                Extensions.Add(type, new xAPIDefinitionsList<xAPIExtension>());
            }
            return Extensions[type].AddSubDefinition(name, extension);
        }
    }
}
#endif