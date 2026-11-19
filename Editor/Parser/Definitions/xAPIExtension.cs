/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Definitions {
    /// <summary>
    /// Class that represents an xAPI extension
    /// </summary>
    internal sealed class xAPIExtension : xAPIDefinition
    {
        /// <summary>
        /// Extension type (<c>activity</c>, <c>context</c> or <c>result</c>). The parser does not set it; the type is the key in <see cref="xAPIContext.Extensions"/>.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Creates an empty xAPIExtension
        /// </summary>
        public xAPIExtension() : base() { }

        /// <summary>
        /// Creates an xAPIExtension
        /// </summary>
        /// <param name="context">Context of the extension</param>
        /// <param name="name">(File) name of the extension</param>
        /// <param name="names">Names of the extension by language</param>
        /// <param name="descriptions">Descriptions of the extension by language.</param>
        public xAPIExtension(string context, string name, IDictionary<string, string> names,
            IDictionary<string, string> descriptions)
            : base(context, name, names, descriptions)
        {
        }

        /// <summary>
        /// Returns <c>extensions</c>, the folder of extensions in a context.
        /// </summary>
        public override string GetDefinitionGroup() => "extensions";
    }
}
#endif