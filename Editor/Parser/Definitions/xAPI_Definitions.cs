using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor.Parser.Definitions
{
    public class xAPI_Definitions
    {
        public readonly IDictionary<string, xAPIContext> Contexts;
        public xAPI_Definitions(IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitionFiles)
        {
            var defs = definitionFiles as IO.DefinitionFile[] ?? definitionFiles.ToArray();
            Contexts = GetUniqueContexts(defs).ToDictionary(ctx => ctx, ctx => new xAPIContext(ctx));
            FillContexts(defs);
        }

        private void FillContexts(IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitionFiles)
        {
            foreach (var definition in definitionFiles)
            {
                Contexts.TryGetValue(definition.Context, out var ctx);
                if (ctx == null)
                    continue;

                var context = definition.Context;
                var name = definition.Name;
                var names = definition.Content.Names;
                var descriptions = definition.Content.Descriptions;
                
                switch (definition.DefinitionType)
                {
                    case DefinitionType.Activity:
                        ctx.AddActivity(new xAPIActivity(context, name, names, descriptions));
                        break;
                    case DefinitionType.Extension:
                        ctx.AddExtension(definition.ExtensionType.ToString().ToLower(),
                            new xAPIExtension(context, name, names, descriptions));
                        break;
                    case DefinitionType.Verb:
                        ctx.AddVerb(new xAPIVerb(context, name, names, descriptions));
                        break;
                    // todo subverb, subextension
                }
            }
        }
        
        private IEnumerable<string> GetUniqueContexts(IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitionFiles)
        {
            var allContexts = definitionFiles.Select(df => df.Context);
            return allContexts.Distinct();
        }
    }
}