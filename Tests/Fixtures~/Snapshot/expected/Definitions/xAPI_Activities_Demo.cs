using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides 3 items of the xAPI_Activities of the context demo as public properties.
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

        /// <summary>
        /// Saved in C:\temp\new.
        /// Second line with a	tab.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/activities/folder
        /// </summary>
        public readonly xAPI_Activity folder = new xAPI_Activity(
            context: "demo",
            key: "folder",
            names: new Dictionary<string, string> {
                ["en-US"] = "folder" },
            descriptions: new Dictionary<string, string> {
                ["en-US"] = "Saved in C:\\temp\\new.\nSecond line with a\ttab." });

        public xAPI_Activities_Demo() 
            : base("demo") {
        }
    }
}