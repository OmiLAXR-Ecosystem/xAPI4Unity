using System.Collections.Generic;

namespace xAPI.Registry {
    /// <summary>
    /// Provides all extensions of the context demo of type result as public properties.
    /// </summary>
    public sealed class xAPI_Extensions_Result_Demo : xAPI_Extensions_Result {
        public xAPI_Extensions_Result_Demo_Vr vr = new xAPI_Extensions_Result_Demo_Vr();

        public xAPI_Extensions_Result_Demo() 
            : base("demo") {
        }

        /// <summary>
        /// The score of an attempt.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/result/score
        /// </summary>
        public xAPI_Extensions_Result_Demo score(System.Single value) {
            Add(new xAPI_Extension(
                    context: Context,
                    extensionType: ExtensionType,
                    key: "score",
                    names: new Dictionary<string, string> {
                        ["en-US"] = "score" },
                    descriptions: new Dictionary<string, string> {
                        ["en-US"] = "The score of an attempt." }),
                 value);
            return this;
        }

        /// <summary>
        /// The score of an attempt.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/result/score
        /// </summary>
        public xAPI_Extensions_Result_Demo score((System.Int32,System.Int32)[] value) {
            Add(new xAPI_Extension(
                    context: Context,
                    extensionType: ExtensionType,
                    key: "score",
                    names: new Dictionary<string, string> {
                        ["en-US"] = "score" },
                    descriptions: new Dictionary<string, string> {
                        ["en-US"] = "The score of an attempt." }),
                 value);
            return this;
        }
    }
}