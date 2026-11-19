using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides 1 items of the xAPI_Verbs of the context other as public properties.
    /// </summary>
    public sealed class xAPI_Verbs_Other : xAPI_Verbs {
        /// <summary>
        /// Waved a hand.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/other/verbs/waved
        /// </summary>
        public readonly xAPI_Verb waved = new xAPI_Verb(
            context: "other",
            key: "waved",
            names: new Dictionary<string, string> {
                ["en-US"] = "waved" },
            descriptions: new Dictionary<string, string> {
                ["en-US"] = "Waved a hand." });

        public xAPI_Verbs_Other() 
            : base("other") {
        }
    }
}