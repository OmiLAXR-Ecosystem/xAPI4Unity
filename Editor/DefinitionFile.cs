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

            foreach (var (key, value) in name)
            {
                dict.Add(key, new Definition(value, description[key]));
            }
            
            return dict;
        }

        public static DefinitionFile FromDefinitionList(Dictionary<string, Definition> definitionList)
        {
            var defFile = new DefinitionFile();
            foreach (var (key, value) in definitionList)
            {
                defFile.name.Add(key, value.name);
                defFile.description.Add(key, value.description);
            }

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