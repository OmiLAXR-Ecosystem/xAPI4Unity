
namespace xAPI.Registry {
    /// <summary>
    /// Static class that provides all contexts as public properties.
    /// </summary>
    public static class xAPI_Definitions {
        /// <summary>
        /// 
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo
        /// </summary>
        public static readonly xAPI_Context_Demo demo = new xAPI_Context_Demo();

        /// <summary>
        /// 
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/other
        /// </summary>
        public static readonly xAPI_Context_Other other = new xAPI_Context_Other();
    }
}