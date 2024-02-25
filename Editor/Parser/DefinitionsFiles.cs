#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.Code.CSharp;
using xAPI4Unity.Editor.Parser.Definitions;

namespace xAPI4Unity.Editor.Parser
{
    internal static class DefinitionsFiles
    {
        private static readonly string[] ExtensionTypes = { "activity", "context", "result" };
        public static void UpdateFiles(string @namespace, IEnumerable<xAPI4Unity.Editor.Parser.IO.DefinitionFile> definitions, string destPath)
        {
            var xAPIDefinitions = new xAPI_Definitions(definitions);

            var generator = new CSharpGenerator(@namespace);

            foreach (var extensionType in ExtensionTypes)
            {
                var extensionTypeFile = generator.GenerateExtensionTypeFile(extensionType);
                extensionTypeFile.Save(destPath);
            }
            
            foreach (var ctx in xAPIDefinitions.Contexts.Values)
            {
                var contextExtensions = generator.GenerateContextExtensionsFile(ctx);
                contextExtensions.Save(destPath);

                foreach (var extensionType in ExtensionTypes)
                {
                    var extFiles = generator.GenerateExtensionsFiles(ctx, extensionType).ToList();
                    extFiles.ForEach(e => e.Save(destPath));
                }
                
                var verbs = generator.GenerateVerbsFiles(ctx).ToList();
                verbs.ForEach(fm => fm.Save(destPath));

                var activities = generator.GenerateActivitiesFiles(ctx).ToList();
                activities.ForEach(fm => fm.Save(destPath));

                var extensions = generator.GenerateExtensions(ctx).ToList();
                extensions.ForEach(fm => fm.Save(destPath));
                
                var ctxFile = generator.GenerateContextFile(ctx);
                ctxFile.Save(destPath);
            }
            
            // xAPI_Definitions.cs
            var definitionsFile = generator.GenerateDefinitionsFile(xAPIDefinitions.Contexts.Values.Select(c => new xAPIContext(c.Name)));
            definitionsFile.Save(destPath);
        }
    }
}
#endif