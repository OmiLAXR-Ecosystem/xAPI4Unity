using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides all extensions of the context demo of type context as public properties.
    /// </summary>
    public sealed class xAPI_Extensions_Context_Demo : xAPI_Extensions_Context {

        public xAPI_Extensions_Context_Demo() 
            : base("demo") {
        }

        /// <summary>
        /// Where it happened.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/context/place
        /// </summary>
        public xAPI_Extensions_Context_Demo place(UnityEngine.Vector3? value) {
            Add(new xAPI_Extension(
                    context: Context,
                    extensionType: ExtensionType,
                    key: "place",
                    names: new Dictionary<string, string> {
                        ["en-US"] = "place" },
                    descriptions: new Dictionary<string, string> {
                        ["en-US"] = "Where it happened." }),
                 value);
            return this;
        }
    }
}