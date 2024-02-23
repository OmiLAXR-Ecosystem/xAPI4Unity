namespace xAPI.Registry
{
    /// <summary>
    /// Version: 1.0.0
    /// </summary>
    public abstract class xAPI_Activities
    {
        public readonly string ContextName;
        public xAPI_Activities(string contextName) {
            ContextName = contextName;
        }
    }
}