#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Definitions {
    /// <summary>
    /// List of xAPIDefinition objects
    /// </summary>
    /// <typeparam name="T">Type of definition</typeparam>
    internal class xAPIDefinitionsList<T> where T : xAPIDefinition {
        /// <summary>
        /// Name of the list
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Sub lists of definitions
        /// </summary>
        public readonly IDictionary<string, xAPIDefinitionsList<T>> SubDefinitions = new Dictionary<string, xAPIDefinitionsList<T>>();

        /// <summary>
        /// List of definitions
        /// </summary>
        public readonly List<T> Definitions = new List<T>();

        public int Count => Definitions.Count;
        public int SubCount => SubDefinitions.Count;
        public int DeepCount => Count + SubCount;

        /// <summary>
        /// Adds a definition
        /// </summary>
        /// <param name="definition">Definition</param>
        /// <returns>True, if it was successfully added, false, if not</returns>
        public bool AddDefinition(T definition) {
            var added = false;
            if (definition != null && (added = !Definitions.Contains(definition))) {
                Definitions.Add(definition);
            }
            return added;
        }
        
        
        /// <summary>
        /// Adds a list of sub definitions
        /// </summary>
        /// <param name="name">Name of the list</param>
        /// <param name="definitions">Definitions</param>
        /// <returns>Number of added definitions</returns>
        public bool AddDefinitions(string name, xAPIDefinitionsList<T> definitions) {
            var added = false;
            if (definitions == null) return added;
            if (added = !SubDefinitions.ContainsKey(name)) {
                SubDefinitions.Add(name, definitions);
            }
            return added;
        }

        /// <summary>
        /// Adds a list of sub definitions
        /// </summary>
        /// <param name="name">Name of the list</param>
        /// <param name="definitions">Definitions</param>
        /// <returns>Number of added definitions</returns>
        public bool AddSubDefinition(string name, T definition) {
            if (definition == null)
                return false;

            if (SubDefinitions.ContainsKey(name))
                SubDefinitions[name].AddDefinition(definition);
            else
            {
                var list = new xAPIDefinitionsList<T>();
                list.AddDefinition(definition);
                SubDefinitions.Add(name, list);
            }

            return true;
        }
    }
}
#endif