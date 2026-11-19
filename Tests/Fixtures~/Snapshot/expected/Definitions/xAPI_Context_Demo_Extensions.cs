
namespace xAPI.Registry {
    /// <summary>
    /// Provides the extensions of the context demo as public properties.
    /// </summary>
    public sealed class xAPI_Context_Demo_Extensions {

        public xAPI_Context_Demo_Extensions() {
        }

        /// <summary>
        /// 4 extensions of 'demo': 1 in activity, 1 in context, 2 in result.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/activity
        /// </summary>
        public xAPI_Extensions_Activity_Demo activity {
            get {
                return new xAPI_Extensions_Activity_Demo();
            }
        }

        /// <summary>
        /// 4 extensions of 'demo': 1 in activity, 1 in context, 2 in result.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/context
        /// </summary>
        public xAPI_Extensions_Context_Demo context {
            get {
                return new xAPI_Extensions_Context_Demo();
            }
        }

        /// <summary>
        /// 4 extensions of 'demo': 1 in activity, 1 in context, 2 in result.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions/result
        /// </summary>
        public xAPI_Extensions_Result_Demo result {
            get {
                return new xAPI_Extensions_Result_Demo();
            }
        }
    }
}