using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides all extensions of the context demo of type result as public properties.
    /// </summary>
    public sealed class xAPI_Extensions_Result_Demo_Vr : xAPI_Extensions_Result {

        public xAPI_Extensions_Result_Demo_Vr() 
            : base("demo") {
        }

        /// <summary>
        /// Height of the head in meters.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/result/height
        /// </summary>
        public xAPI_Extensions_Result_Demo_Vr height(System.Single value) {
            if (value < 0 || value > 3) throw new System.ArgumentOutOfRangeException("value");
            Add(new xAPI_Extension(
                    context: Context,
                    extensionType: ExtensionType,
                    key: "height",
                    names: new Dictionary<string, string> {
                        ["en-US"] = "height" },
                    descriptions: new Dictionary<string, string> {
                        ["en-US"] = "Height of the head in meters." }),
                 value);
            return this;
        }
    }
}