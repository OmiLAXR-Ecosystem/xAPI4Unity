using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides 2 items of the xAPI_Verbs of the context demo as public properties.
    /// </summary>
    public sealed class xAPI_Verbs_Demo : xAPI_Verbs {
        /// <summary>
        /// Provides 1 items xAPI_Verb of the context 'demo' as public properties.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/navigation
        /// </summary>
        public readonly xAPI_Verbs_Demo_Navigation navigation = new xAPI_Verbs_Demo_Navigation();

        /// <summary>
        /// Pressed a "button".
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/verbs/pressed
        /// </summary>
        public readonly xAPI_Verb pressed = new xAPI_Verb(
            context: "demo",
            key: "pressed",
            names: new Dictionary<string, string> {
                ["en-US"] = "pressed",
                ["de-DE"] = "gedrückt" },
            descriptions: new Dictionary<string, string> {
                ["en-US"] = "Pressed a \"button\".",
                ["de-DE"] = "Hat einen Knopf gedrückt." });

        public xAPI_Verbs_Demo() 
            : base("demo") {
        }
    }
}