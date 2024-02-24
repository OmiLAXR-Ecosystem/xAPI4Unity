namespace xAPI.Registry
{
    /// <summary>
    /// Version: 1.0.0
    /// </summary>
    public abstract class xAPI_Verbs
    {
        public readonly string ContextName;

        public xAPI_Verbs(string contextName)
        {
            ContextName = contextName;
        }
    }
}