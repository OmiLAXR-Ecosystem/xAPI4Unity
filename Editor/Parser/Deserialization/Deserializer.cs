using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.Definitions;
using xAPI4Unity.Editor.Parser.IO;
using CodeGenerator = xAPI4Unity.Editor.Parser.Code.CodeGenerator;

namespace xAPI4Unity.Editor.Parser.Deserialization {
    /// <summary>
    /// Processes xAPI input from JSON and turns it into corresponding classes
    /// </summary>
    public static class Deserializer
    {
        /// <summary>
        /// Deserializes xAPI contexts from JSON
        /// </summary>
        /// <param name="contexts">List of xAPI contexts</param>
        /// <returns>List of deserialized contexts</returns>
        public static List<xAPIContext> DeserializeContexts(List<Context> contexts) {
            if (contexts == null) {
                throw new ArgumentNullException(nameof(contexts));
            }

            return contexts.Select(DeserializeContext).ToList();
        }

        

        /// <summary>
        /// Deserializes an xAPI context from JSON
        /// </summary>
        /// <param name="context">Context</param>
        /// <returns>Deserialized context</returns>
        public static xAPIContext DeserializeContext(Context context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            
            var description = $"Entries of the context '{context.Name}' (verbs: {context.Verbs.Count}, activities: {context.Activities.Count}, extensions: {context.Extensions.Count}).";
            var deserializedContext = new xAPIContext(context.Name, description)
            {
                Activities = DeserializeDefinitions<xAPIActivity>(context.Name, context.Activities)
            };
            foreach (var extType in context.Extensions) {
                deserializedContext.Extensions[extType.Key] = DeserializeDefinitions<xAPIExtension>(context.Name, extType.Value);
            }
            deserializedContext.Verbs = DeserializeDefinitions<xAPIVerb>(context.Name, context.Verbs);

            return deserializedContext;
        }
        
        /// <summary>
        /// Deserializes xAPI definitions from JSON
        /// </summary>
        /// <typeparam name="T">Type of definition</typeparam>
        /// <param name="context">Context</param>
        /// <param name="files">Files of the definitions</param>
        /// <returns>Deserialized definitions</returns>
        public static xAPIDefinitionsList<T> DeserializeDefinitions<T>(string context, List<FileMeta> files) where T : xAPIDefinition {
            if (files == null) {
                throw new ArgumentNullException(nameof(files));
            }

            var definitions = new xAPIDefinitionsList<T>();
            foreach (var file in files) {
                if (file.IsDirectory) {
                    definitions.AddDefinitions(file.Name, DeserializeDefinitions<T>(context, file.DirContent));
                } else {
                    definitions.AddDefinition(DeserializeDefinition<T>(context, file));
                }
            }
            return definitions;
        }

        /// <summary>
        /// Deserializes an xAPI definition from JSON
        /// </summary>
        /// <typeparam name="T">Type of definition</typeparam>
        /// <param name="context">Context</param>
        /// <param name="file">File of the definition</param>
        /// <returns>Deserialized definition</returns>
        public static T DeserializeDefinition<T>(string context, FileMeta file) where T : xAPIDefinition {
            if(file == null) {
                throw new ArgumentNullException(nameof(file));
            }

            var definition = JsonConvert.DeserializeObject<T>(file.FileContent);
            if (definition == null)
                return null;
            definition.Context = context ?? throw new ArgumentNullException(nameof(context));
            definition.DefName = file.Name;
            return definition;
        }
    }
}