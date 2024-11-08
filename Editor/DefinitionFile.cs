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

            #if UNITY_2019 || UNITY_2020
            foreach (var kvp in name)
            {
                dict.Add(kvp.Key, new Definition(kvp.Value, description[kvp.Key]));
            }
            #else 
            foreach (var (key, value) in name)
            {
                dict.Add(key, new Definition(value, description[key]));
            }
            #endif
            
            return dict;
        }

        public static DefinitionFile FromDefinitionList(Dictionary<string, Definition> definitionList)
        {
            var defFile = new DefinitionFile();
            #if UNITY_2019 || UNITY_2020
            foreach (var kvp in definitionList)
            {
                defFile.name.Add(kvp.Key, kvp.Value.name);
                defFile.description.Add(kvp.Key, kvp.Value.description);
            }
            #else 
            foreach (var (key, value) in definitionList)
            {
                defFile.name.Add(key, value.name);
                defFile.description.Add(key, value.description);
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