
namespace xAPI.Registry {
    /// <summary>
    /// Provides all extensions of type result as public properties.
    /// </summary>
    public class xAPI_Extensions_Result : xAPI_Extensions {

        public xAPI_Extensions_Result(string context) 
            : base("result", context) {
        }

        public xAPI_Extensions_Result() 
            : base("result", null) {
        }
    }
}