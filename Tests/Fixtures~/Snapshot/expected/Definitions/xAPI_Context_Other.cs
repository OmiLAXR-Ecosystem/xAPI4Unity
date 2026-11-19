
namespace xAPI.Registry {
    /// <summary>
    /// Provides the definitions of the context other as public properties.
    /// </summary>
    public class xAPI_Context_Other : xAPI_Context {
        /// <summary>
        /// 1 verbs of 'other'.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/other/verbs
        /// </summary>
        public readonly xAPI_Verbs_Other verbs = new xAPI_Verbs_Other();

        /// <summary>
        /// 0 activities of 'other'.
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/other/activities
        /// </summary>
        public readonly xAPI_Activities_Other activities = new xAPI_Activities_Other();

        /// <summary>
        /// 0 extensions of 'other': .
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/other/extensions
        /// </summary>
        public readonly xAPI_Context_Other_Extensions extensions = new xAPI_Context_Other_Extensions();

        public xAPI_Context_Other() 
            : base("other") {
        }
    }
}