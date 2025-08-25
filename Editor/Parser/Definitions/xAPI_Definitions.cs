/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor.Parser.Definitions
{
    /// <summary>
    /// Represents a collection of xAPI contexts, constructed from definition files.
    /// Dynamically populates the contexts with activities, extensions, and verbs.
    /// </summary>
    internal class xAPI_Definitions
    {
        /// <summary>
        /// A dictionary of context names and their respective xAPIContext instances.
        /// </summary>
        public readonly IDictionary<string, xAPIContext> Contexts;

        /// <summary>
        /// Initializes a new instance of the xAPI_Definitions class based on a collection of definition files.
        /// </summary>
        /// <param name="definitionFiles">A collection of definition files to process.</param>
        public xAPI_Definitions(IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitionFiles)
        {
            var defs = definitionFiles as IO.DefinitionFile[] ?? definitionFiles.ToArray();

            // Extract unique contexts from the definition files and create an xAPIContext for each.
            Contexts = GetUniqueContexts(defs).ToDictionary(ctx => ctx, ctx => new xAPIContext(ctx));

            // Populate each context with activities, extensions, and verbs from the definition files.
            FillContexts(defs);
        }

        /// <summary>
        /// Fills the contexts with activities, extensions, and verbs extracted from the definition files.
        /// </summary>
        /// <param name="definitionFiles">The collection of definition files to process.</param>
        private void FillContexts(IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitionFiles)
        {
            foreach (var definition in definitionFiles)
            {
                // Attempt to retrieve the xAPIContext for the current definition's context.
                Contexts.TryGetValue(definition.Context, out var ctx);

                // Skip processing if the context does not exist.
                if (ctx == null)
                    continue;

                // Extract context-specific details from the definition.
                var context = definition.Context;
                var name = definition.Name;
                var names = definition.Content.Names;
                var descriptions = definition.Content.Descriptions;
                
                // Process the definition based on its type.
                switch (definition.DefinitionType)
                {
                    case DefinitionType.Activity:
                        // Create a new xAPIActivity and add it to the context.
                        var activity = new xAPIActivity(context, name, names, descriptions);
                        if (definition.HasCategory)
                            ctx.AddSubActivity(definition.Category, activity);
                        else 
                            ctx.AddActivity(activity);
                        break;

                    case DefinitionType.Extension:
                        // Create a new xAPIExtension and handle its type and category.
                        var extension = new xAPIExtension(context, name, names, descriptions);
                        var extType = definition.ExtensionType.ToString().ToLower();

                        // Validate the extension type before adding it to the context.
                        if (extType != "activity" && extType != "context" && extType != "result")
                        {
                            Debug.LogWarning($"[xAPI4Unity]: Unknown extension type '{extType}' for extension '{name}' in context '{context}'. Skipping extension.");
                            continue;
                        }

                        // Add the extension to the appropriate collection in the context.
                        if (definition.HasCategory)
                            ctx.AddSubExtension(extType, definition.Category, extension);
                        else 
                            ctx.AddExtension(extType, extension);
                        break;

                    case DefinitionType.Verb:
                        // Create a new xAPIVerb and add it to the context.
                        var verb = new xAPIVerb(context, name, names, descriptions);
                        if (definition.HasCategory)
                            ctx.AddSubVerb(definition.Category, verb);
                        else 
                            ctx.AddVerb(verb);
                        break;
                }
            }
        }

        /// <summary>
        /// Extracts a distinct collection of context names from the definition files.
        /// </summary>
        /// <param name="definitionFiles">The collection of definition files to process.</param>
        /// <returns>A collection of unique context names.</returns>
        private IEnumerable<string> GetUniqueContexts(IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitionFiles)
        {
            // Collect all context names from the definition files and return the unique ones.
            var allContexts = definitionFiles.Select(df => df.Context);
            return allContexts.Distinct();
        }
    }
}
#endif