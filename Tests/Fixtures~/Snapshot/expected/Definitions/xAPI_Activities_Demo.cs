using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides 2 items of the xAPI_Activities of the context demo as public properties.
    /// </summary>
    public sealed class xAPI_Activities_Demo : xAPI_Activities {
        /// <summary>
        /// Provides 1 items xAPI_Activity of the context 'demo' as public properties.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/ui
        /// </summary>
        public readonly xAPI_Activities_Demo_Ui ui = new xAPI_Activities_Demo_Ui();

        /// <summary>
        /// A button in the scene.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/activities/button
        /// </summary>
        public readonly xAPI_Activity button = new xAPI_Activity(
            context: "demo",
            key: "button",
            names: new Dictionary<string, string> {
                ["en-US"] = "button" },
            descriptions: new Dictionary<string, string> {
                ["en-US"] = "A button in the scene." });

        public xAPI_Activities_Demo() 
            : base("demo") {
        }
    }
}