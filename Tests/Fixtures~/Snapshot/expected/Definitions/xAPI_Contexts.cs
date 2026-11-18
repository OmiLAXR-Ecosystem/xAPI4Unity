
namespace xAPI.Registry {
    /// <summary>
    /// Class that provides all contexts as public properties.
    /// </summary>
    public class xAPI_Contexts {
        /// <summary>
        /// 
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/demo
        /// </summary>
        public readonly xAPI_Context_Demo demo = new xAPI_Context_Demo();

        /// <summary>
        /// 
        /// URI: https://omilaxr-ecosystem.github.io/xapi/definitions/other
        /// </summary>
        public readonly xAPI_Context_Other other = new xAPI_Context_Other();
    }
}