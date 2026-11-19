using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides 1 items of the xAPI_Verbs of the context demo as public properties.
    /// </summary>
    public sealed class xAPI_Verbs_Demo_Navigation : xAPI_Verbs {
        /// <summary>
        /// Jumped to another place.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/verbs/jumped
        /// </summary>
        public readonly xAPI_Verb jumped = new xAPI_Verb(
            context: "demo",
            key: "jumped",
            names: new Dictionary<string, string> {
                ["en-US"] = "jumped" },
            descriptions: new Dictionary<string, string> {
                ["en-US"] = "Jumped to another place." });

        public xAPI_Verbs_Demo_Navigation() 
            : base("demo") {
        }
    }
}