using System.Collections.Generic;

namespace xAPI.Registry
{
    /// <summary>
    /// Version: 1.0.0
    /// </summary>
    public sealed class xAPI_Activity : xAPI_Definition
    {
        public xAPI_Activity(string context, string key, Dictionary<string, string> names,
            Dictionary<string, string> descriptions)
            : base(context, key, names, descriptions)
        {
        }

        public override string GetPath() => $"/{Context}/activities/{Key}";
    }
}