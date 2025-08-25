/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Represents a definition file containing mappings for names and descriptions.
    /// Facilitates conversion between name/description dictionaries and structured definitions.
    /// </summary>
    [Serializable]
    internal struct DefinitionFile
    {
        /// <summary>
        /// Dictionary mapping unique keys to names.
        /// </summary>
        public Dictionary<string, string> name;

        /// <summary>
        /// Dictionary mapping unique keys to descriptions.
        /// </summary>
        public Dictionary<string, string> description;

        /// <summary>
        /// Converts the name and description dictionaries into a dictionary of <see cref="Definition"/> objects.
        /// </summary>
        /// <returns>A dictionary where each key corresponds to a <see cref="Definition"/> instance.</returns>
        public Dictionary<string, Definition> ToDefinitionList()
        {
            var dict = new Dictionary<string, Definition>();
            
#if UNITY_2021_1_OR_NEWER
            // Use tuple deconstruction for newer Unity versions (C# 7.0 and later)
            foreach (var (key, value) in name)
            {
                dict.Add(key, new Definition(value, description[key]));
            }
#else
            // Fallback for older Unity versions without tuple deconstruction
            foreach (var kvp in name)
            {
                dict.Add(kvp.Key, new Definition(kvp.Value, description[kvp.Key]));
            }
#endif
            
            return dict;
        }

        /// <summary>
        /// Converts a dictionary of <see cref="Definition"/> objects back into a <see cref="DefinitionFile"/>.
        /// </summary>
        /// <param name="definitionList">The dictionary of definitions to convert.</param>
        /// <returns>A <see cref="DefinitionFile"/> containing the converted name and description dictionaries.</returns>
        public static DefinitionFile FromDefinitionList(Dictionary<string, Definition> definitionList)
        {
            var defFile = new DefinitionFile { name = new Dictionary<string, string>(), description = new Dictionary<string, string>() };
            
#if UNITY_2021_1_OR_NEWER
            // Use tuple deconstruction for newer Unity versions (C# 7.0 and later)
            foreach (var (key, value) in definitionList)
            {
                defFile.name.Add(key, value.name);
                defFile.description.Add(key, value.description);
            }
#else
            // Fallback for older Unity versions without tuple deconstruction
            foreach (var kvp in definitionList)
            {
                defFile.name.Add(kvp.Key, kvp.Value.name);
                defFile.description.Add(kvp.Key, kvp.Value.description);
            }
#endif

            return defFile;
        }

        /// <summary>
        /// Returns a string representation of the definition file, showing the count of names and descriptions.
        /// </summary>
        /// <returns>A string indicating the number of names and descriptions.</returns>
        public override string ToString()
        {
            return $"{name.Count} names, {description.Count} descriptions";
        }
    }

    /// <summary>
    /// Represents a single definition with a name and description.
    /// </summary>
    internal struct Definition
    {
        /// <summary>
        /// Name of the definition.
        /// </summary>
        public string name;

        /// <summary>
        /// Description of the definition.
        /// </summary>
        public string description;

        /// <summary>
        /// Constructs a new instance of <see cref="Definition"/> with the specified name and description.
        /// </summary>
        /// <param name="name">The name of the definition.</param>
        /// <param name="description">The description of the definition.</param>
        public Definition(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }
}
#endif