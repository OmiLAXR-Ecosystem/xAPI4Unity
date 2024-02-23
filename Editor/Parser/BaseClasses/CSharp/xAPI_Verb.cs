using System.Collections.Generic;

namespace xAPI.Registry
{
    /// <summary>
    /// Version: 1.0.0
    /// </summary>
    public sealed class xAPI_Verb : xAPI_Definition
    {
        public xAPI_Verb(string context, string key, Dictionary<string, string> names,
            Dictionary<string, string> descriptions)
            : base(context, key, names, descriptions)
        {
        }

        public override string GetPath() => $"/{Context}/verbs/{Key}";
    }
}