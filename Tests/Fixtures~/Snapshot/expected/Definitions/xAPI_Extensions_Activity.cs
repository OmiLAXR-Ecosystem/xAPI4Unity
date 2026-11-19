
namespace xAPI.Registry {
    /// <summary>
    /// Provides all extensions of type activity as public properties.
    /// </summary>
    public class xAPI_Extensions_Activity : xAPI_Extensions {

        public xAPI_Extensions_Activity(string context) 
            : base("activity", context) {
        }

        public xAPI_Extensions_Activity() 
            : base("activity", null) {
        }
    }
}