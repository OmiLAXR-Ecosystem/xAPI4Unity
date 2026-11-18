using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides all extensions of the context demo of type activity as public properties.
    /// </summary>
    public sealed class xAPI_Extensions_Activity_Demo : xAPI_Extensions_Activity {

        public xAPI_Extensions_Activity_Demo() 
            : base("demo") {
        }

        /// <summary>
        /// Size of the activity object.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/activity/size
        /// </summary>
        public xAPI_Extensions_Activity_Demo size(System.Object value) {
            Add(new xAPI_Extension(
                    context: Context,
                    extensionType: ExtensionType,
                    key: "size",
                    names: new Dictionary<string, string> {
                        ["en-US"] = "size" },
                    descriptions: new Dictionary<string, string> {
                        ["en-US"] = "Size of the activity object." }),
                 value);
            return this;
        }
    }
}