#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace xAPI4Unity.Editor
{
    [Serializable]
    internal struct DefinitionFile
    {
        public Dictionary<string, string> name;
        public Dictionary<string, string> description;

        public Dictionary<string, Definition> ToDefinitionList()
        {
            var dict = new Dictionary<string, Definition>();
            
#if UNITY_2021_1_OR_NEWER
            // C# 7.0 tuple deconstruction supported
            foreach (var (key, value) in name)
            {
                dict.Add(key, new Definition(value, description[key]));
            }
#else
            // Fallback for older Unity versions (pre-2021)
            foreach (var kvp in name)
            {
                dict.Add(kvp.Key, new Definition(kvp.Value, description[kvp.Key]));
            }
#endif
            
            return dict;
        }

        public static DefinitionFile FromDefinitionList(Dictionary<string, Definition> definitionList)
        {
            var defFile = new DefinitionFile();
#if UNITY_2021_1_OR_NEWER
            // C# 7.0 tuple deconstruction supported
            foreach (var (key, value) in definitionList)
            {
                defFile.name.Add(key, value.name);
                defFile.description.Add(key, value.description);
            }
#else
            // Older Unity versions — no tuple deconstruction
            foreach (var kvp in definitionList)
            {
                defFile.name.Add(kvp.Key, kvp.Value.name);
                defFile.description.Add(kvp.Key, kvp.Value.description);
            }
#endif

            return defFile;
        }

        public override string ToString()
        {
            return name.Count + " " + description.Count;
        }
    }

    internal struct Definition
    {
        public string name;
        public string description;

        public Definition(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }
}
#endif