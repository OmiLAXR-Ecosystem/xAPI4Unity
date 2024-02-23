using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Definitions {
    /// <summary>
    /// Class that represents an xAPI verb
    /// </summary>
    public sealed class xAPIVerb : xAPIDefinition {
        /// <summary>
        /// Creates an empty xAPIVerb
        /// </summary>
        public xAPIVerb() : base() { }

        /// <summary>
        /// Creates an xAPIVerb
        /// </summary>
        /// <param name="context">Context of the verb</param>
        /// <param name="name">(File) name of the verb</param>
        /// <param name="names">Names of the verb by language</param>
        /// <param name="descriptions">Descriptions of the verb by language</param>
        public xAPIVerb(string context, string name, IDictionary<string, string> names, Dictionary<string, string> descriptions)
            : base(context, name, names, descriptions) { }
        
        public override string GetDefinitionGroup() => "verbs";
    }
}