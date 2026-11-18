
namespace xAPI.Registry {
    /// <summary>
    /// Provides the definitions of the context demo as public properties.
    /// </summary>
    public class xAPI_Context_Demo : xAPI_Context {
        /// <summary>
        /// 1 verbs of 'demo'.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/verbs
        /// </summary>
        public readonly xAPI_Verbs_Demo verbs = new xAPI_Verbs_Demo();

        /// <summary>
        /// 1 activities of 'demo'.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/activities
        /// </summary>
        public readonly xAPI_Activities_Demo activities = new xAPI_Activities_Demo();

        /// <summary>
        /// 4 extensions of 'demo': 1 in activity, 1 in context, 2 in result.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo/extensions
        /// </summary>
        public readonly xAPI_Context_Demo_Extensions extensions = new xAPI_Context_Demo_Extensions();

        public xAPI_Context_Demo() 
            : base("demo") {
        }
    }
}