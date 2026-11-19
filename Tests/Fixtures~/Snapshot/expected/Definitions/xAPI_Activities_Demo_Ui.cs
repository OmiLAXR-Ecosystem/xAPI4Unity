using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides 1 items of the xAPI_Activities of the context demo as public properties.
    /// </summary>
    public sealed class xAPI_Activities_Demo_Ui : xAPI_Activities {
        /// <summary>
        /// A slider of the user interface.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/activities/slider
        /// </summary>
        public readonly xAPI_Activity slider = new xAPI_Activity(
            context: "demo",
            key: "slider",
            names: new Dictionary<string, string> {
                ["en-US"] = "slider" },
            descriptions: new Dictionary<string, string> {
                ["en-US"] = "A slider of the user interface." });

        public xAPI_Activities_Demo_Ui() 
            : base("demo") {
        }
    }
}