using System.Collections.Generic;

namespace xAPI.Registry
{
    /// <summary>
    /// Version: 1.0.0
    /// </summary>
    public sealed class xAPI_Extension : xAPI_Definition
    {
        public readonly string extensionType;

        public xAPI_Extension(string context, string extensionType, string key, Dictionary<string, string> names,
            Dictionary<string, string> descriptions)
            : base(context, key, names, descriptions)
        {
            this.extensionType = extensionType;
        }

        public override string GetPath() => $"/{Context}/extensions/{extensionType}/{Key}";
    }
}