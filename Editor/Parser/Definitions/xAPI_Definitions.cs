#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor.Parser.Definitions
{
    internal class xAPI_Definitions
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
                        var activity = new xAPIActivity(context, name, names, descriptions);
                        if (definition.HasCategory)
                            ctx.AddSubActivity(definition.Category, activity);
                        else 
                            ctx.AddActivity(activity);
                        
                        break;
                    case DefinitionType.Extension:
                        var extension = new xAPIExtension(context, name, names, descriptions);
                        var extType = definition.ExtensionType.ToString().ToLower();

                        if (definition.HasCategory)
                            ctx.AddSubExtension(extType, definition.Category, extension);
                        else 
                            ctx.AddExtension(extType, extension);
                        
                        break;
                    case DefinitionType.Verb:
                        var verb = new xAPIVerb(context, name, names, descriptions);
                        if (definition.HasCategory)
                            ctx.AddSubVerb(definition.Category, verb);
                        else 
                            ctx.AddVerb(verb);
                        
                        break;
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
#endif