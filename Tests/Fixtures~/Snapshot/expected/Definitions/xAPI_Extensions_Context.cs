
namespace xAPI.Registry {
    /// <summary>
    /// Provides all extensions of type context as public properties.
    /// </summary>
    public class xAPI_Extensions_Context : xAPI_Extensions {

        public xAPI_Extensions_Context(string context) 
            : base("context", context) {
        }

        public xAPI_Extensions_Context() 
            : base("context", null) {
        }
    }
}