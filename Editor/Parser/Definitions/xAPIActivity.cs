using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Definitions {
    /// <summary>
    /// Class that represents an xAPI activity
    /// </summary>
    public sealed class xAPIActivity : xAPIDefinition {
        /// <summary>
        /// Creates an empty xAPIActivity
        /// </summary>
        public xAPIActivity() : base() { }

        /// <summary>
        /// Creates an xAPIActivity
        /// </summary>
        /// <param name="context">Context of the activity</param>
        /// <param name="name">(File) name of the activity</param>
        /// <param name="names">Names of the activity by language</param>
        /// <param name="descriptions">Descriptions of the activity by language</param>
        public xAPIActivity(string context, string name, IDictionary<string, string> names, Dictionary<string, string> descriptions)
            : base(context, name, names, descriptions) { }
        
        public override string GetDefinitionGroup() => "activities";

    }
}