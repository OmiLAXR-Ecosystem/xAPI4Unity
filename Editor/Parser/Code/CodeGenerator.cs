/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using xAPI4Unity.Editor.Parser.Code.SyntaxTree;
using xAPI4Unity.Editor.Parser.Definitions;
using xAPI4Unity.Editor.Parser.IO;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor.Parser.Code
{
    /// <summary>
    /// Generates CodeFiles from the xAPI definitions
    /// </summary>
    internal abstract class CodeGenerator
    {
        internal class CoreFiles : List<FileMeta>
        {
            public CoreFiles()
            {
            }

            public CoreFiles(IEnumerable<FileMeta> list) => AddRange(list);
            public static CoreFiles From(IEnumerable<FileMeta> list) => new CoreFiles(list);
        }

        internal class ContextFiles : List<FileMeta>
        {
            public ContextFiles()
            {
            }

            public ContextFiles(IEnumerable<FileMeta> list) => AddRange(list);
            public static ContextFiles From(IEnumerable<FileMeta> list) => new ContextFiles(list);
        }

        public virtual string CoreDirName => "Core";
        public virtual string DefinitionsDirName => "Definitions";

        protected virtual string ActivityClassName => "xAPI_Activity";
        protected virtual string ActivitiesClassName => "xAPI_Activities";
        protected virtual string VerbClassName => "xAPI_Verb";
        protected virtual string VerbsClassName => "xAPI_Verbs";
        protected virtual string ExtensionClassName => "xAPI_Extension";
        protected virtual string ExtensionsClassName => "xAPI_Extensions";
        protected virtual string DefinitionsClassName => "xAPI_Definitions";
        protected virtual string ContextsClassName => "xAPI_Contexts";
        protected virtual string ContextClassName => "xAPI_Context";

        protected virtual string BuildActivitiesClassName(params string[] args)
            => args.Aggregate(ActivitiesClassName, (current, arg) => current + ("_" + arg));

        protected virtual string BuildVerbsClassName(params string[] args)
            => args.Aggregate(VerbsClassName, (current, arg) => current + ("_" + arg));

        protected virtual string ExtendClassName(string baseName, params string[] args)
            => args.Aggregate(baseName, (current, arg) => current + ("_" + arg));

        protected virtual string BuildExtensionTypeClassName(string type) => ExtensionsClassName + "_" + type;

        protected virtual string BuildExtensionsClassName(string type, params string[] args)
            => args.Aggregate(BuildExtensionTypeClassName(type), (current, arg) => current + ("_" + arg));

        protected virtual string BuildContextClassName(string context) => ContextClassName + "_" + context;

        protected virtual string BuildContextExtensionsClassName(string context) =>
            BuildContextClassName(context) + "_Extensions";

        /// <summary>
        /// Namespace of the generated code (can be null)
        /// </summary>
        protected abstract string Namespace { get; }

        /// <summary>
        /// Serializer for the language
        /// </summary>
        protected readonly SyntaxTreeSerializer Serializer;

        /// <summary>
        /// Logging action
        /// </summary>
        public event Action<string> OnLog;

        protected void Log(string message) => OnLog?.Invoke(message);

        /// <summary>
        /// Creates a CodeGenerator
        /// </summary>
        /// <param name="serializer">Serializer to use in the generator</param>
        protected CodeGenerator(SyntaxTreeSerializer serializer)
        {
            Serializer = serializer;
        }

        /// <summary>
        /// Create indentation whitespace of specified depth
        /// </summary>
        /// <param name="depth">Indentation depth</param>
        /// <returns>String with whitespace</returns>
        public static string Indent(int depth)
        {
            return new string(' ', 4 * depth);
        }

        /// <summary>
        /// Generates files from contexts
        /// </summary>
        /// <param name="contexts">List of contexts to generate from</param>
        /// <returns>List of generated files</returns>
        public virtual ContextFiles GenerateContexts(List<xAPIContext> contexts)
        {
            var files = new ContextFiles();
            var extTypes = new[] { "activity", "context", "result" };
            if (contexts.Count <= 0)
                return files;

            foreach (var contextFiles in contexts.Select(GenerateContext))
            {
                files.AddRange(contextFiles);
            }

            files.Add(GenerateDefinitionsFile(contexts));
            files.AddRange(extTypes.Select(GenerateExtensionTypeFile));

            return files;
        }
        
        /// <summary>
        /// Generates files from a context
        /// </summary>
        /// <param name="context">Context to generate from</param>
        /// <returns>List of generated files</returns>
        public virtual List<FileMeta> GenerateContext(xAPIContext context)
        {
            Log($"{GetType().Name} generating context: {context.Name}");
            var files = new List<FileMeta>();
            files.AddRange(GenerateActivitiesFiles(context));
            files.AddRange(GenerateVerbsFiles(context));
            files.AddRange(GenerateExtensions(context));
            files.Add(GenerateContextFile(context));
            files.Add(GenerateContextExtensionsFile(context));
            return files;
        }

        /// <summary>
        /// Generates Extensions files from a context
        /// </summary>
        /// <param name="context">Context to generate from</param>
        /// <param name="typeSchema">Schema of types JSON</param>
        /// <returns>List of generated files</returns>
        public virtual IEnumerable<FileMeta> GenerateExtensions(xAPIContext context, TypeSchema typeSchema = null)
        {
            var codeFiles = new List<FileMeta>();
            foreach (var extType in context.ExtensionTypes)
            {
                codeFiles.AddRange(GenerateExtensionsFiles(context, extType, typeSchema: typeSchema));
            }

            return codeFiles;
        }

        /// <summary>
        /// Creates a class for definition files
        /// </summary>
        /// <typeparam name="T">Type of definition</typeparam>
        /// <param name="context">Context to create a definition file of</param>
        /// <param name="name">Name of the class</param>
        /// <param name="definitions">Definitions to create an object of</param>
        /// <param name="defType">Definition type</param>
        /// <param name="defTypePlural">Plural of the definition type</param>
        /// <returns>Created class</returns>
        protected virtual Class CreateDefinitionsClass<T>(string context, string name,
            xAPIDefinitionsList<T> definitions,
            string defType, string defTypePlural) where T : xAPIDefinition
        {
            var properties = definitions.SubDefinitions
                .Select(subDefinitions
                    => new Property(
                            name: subDefinitions.Key,
                            type: ExtendClassName(name, subDefinitions.Key.Capitalize())
                        )
                        .AddModifier(Modifier.Public)
                        .AddModifier(Modifier.ReadOnly)
                        .WithComment(
                            $"Provides {subDefinitions.Value.Count} items {defType} of the context '{context}' as public properties."
                            + $"\nURI: {GetUri(context, subDefinitions.Key)}"
                        )
                ).ToList();
            properties.AddRange(definitions.Definitions
                .Select(definition => new Property(definition.DefName, defType)
                    .AddModifier(Modifier.Public)
                    .AddModifier(Modifier.ReadOnly)
                    .AddArgument(new Argument($"\"{context}\"", "context"))
                    .AddArgument(new Argument($"\"{definition.DefName}\"", "key"))
                    .AddArgument(new Argument(definition.Names, "names"))
                    .AddArgument(new Argument(definition.Descriptions, "descriptions"))
                    .WithComment(
                        definition.GetDescription("en-US", true)
                        + $"\nURI: {GetUri(definition.Context, definition.GetDefinitionGroup(), definition.DefName)}"
                    )
                ));

            return new Class(name, defTypePlural,
                    $"Provides {definitions.DeepCount} items of the {defTypePlural} of the context {context} as public properties.")
                .AddModifier(Modifier.Public)
                .AddModifier(Modifier.Sealed)
                .AddProperties(properties)
                .AddConstructor(
                    new Constructor(name)
                        .AddModifier(Modifier.Public)
                        .AddBaseArgument(new Argument($"\"{context}\""))
                );
        }

        /// <summary>
        /// Ensures the given string path starts with a slash ('/').
        /// </summary>
        /// <param name="p">The path that may or may not start with a slash.</param>
        /// <returns>A string path that starts with a slash.</returns>
        private static string EnsureBeginningSlash(string p) => p.Length < 1 ? "/" : (p[0] != '/' ? '/' + p : p);

        /// <summary>
        /// Constructs a full URI by combining a base URI and additional path segments.
        /// Removes any leading or trailing slashes from the provided paths
        /// and ensures proper formatting for the resulting URI.
        /// </summary>
        /// <param name="paths">An array of strings representing path segments to be appended.</param>
        /// <returns>A fully constructed URI combining the base URI and path segments.</returns>
        public static Uri GetUri(params string[] paths)
        {
            // Remove any slashes within each path segment to ensure clean formatting
            paths = paths.Select(p => p.Replace("/", "")).ToArray();
            // Join the cleaned path segments with a forward slash
            var path = string.Join("/", paths);
            // Combine the base URI with the joined path to generate the full URI
            return GetUri(BaseUri, path);
        }

        /// <summary>
        /// Constructs a full URI given multiple path segments starting from a base URI.
        /// Automatically normalizes slash characters between segments.
        /// </summary>
        /// <param name="baseUri">The base URI for the prefix.</param>
        /// <param name="path">The relative path to append to the base.</param>
        /// <returns>A new URI representing the combined base and path.</returns>
        public static Uri GetUri(Uri baseUri, string path) =>
            new Uri(baseUri, baseUri.LocalPath + EnsureBeginningSlash(path));

        /// <summary>
        /// Constructs a URI for a specific xAPI context and its path segments.
        /// Prepends the context name to the path before joining all segments.
        /// </summary>
        /// <param name="context">The xAPI context containing the base information.</param>
        /// <param name="paths">Additional path segments to append to the context name.</param>
        /// <returns>A new URI for the specified context and paths.</returns>
        public static Uri GetUri(xAPIContext context, params string[] paths) =>
            GetUri(paths.Prepend(context.Name).ToArray());

        /// <summary>
        /// Constructs a URI for a specific xAPIVerb and its path segments.
        /// Uses the verb's context and definition name to form the URI.
        /// </summary>
        /// <param name="verb">The xAPIVerb containing the context and definition information.</param>
        /// <param name="paths">Additional path segments to append to the URI.</param>
        /// <returns>A new URI for the specified verb and paths.</returns>
        public static Uri GetUri(xAPIVerb verb, params string[] paths) =>
            GetUri((new[] { verb.Context, "verbs", verb.DefName }).Concat(paths).ToArray());

        /// <summary>
        /// Constructs a URI for a specific xAPIActivity and its path segments.
        /// Uses the activity's context and definition name to form the URI.
        /// </summary>
        /// <param name="activity">The xAPIActivity containing the context and definition information.</param>
        /// <param name="paths">Additional path segments to append to the URI.</param>
        /// <returns>A new URI for the specified activity and paths.</returns>
        public static Uri GetUri(xAPIActivity activity, params string[] paths) =>
            GetUri((new[] { activity.Context, "activities", activity.DefName }).Concat(paths).ToArray());

        /// <summary>
        /// Constructs a URI for a specific xAPIExtension, its type, and its path segments.
        /// Uses extension context, type, and definition name to form the URI.
        /// </summary>
        /// <param name="extension">The xAPIExtension containing the base information.</param>
        /// <param name="type">The extension type to include in the URI.</param>
        /// <param name="paths">Additional path segments to append to the URI.</param>
        /// <returns>A new URI for the specified extension, type, and paths.</returns>
        public static Uri GetUri(xAPIExtension extension, string type, params string[] paths) =>
            GetUri((new[] { extension.Context, "extensions", type, extension.DefName }).Concat(paths).ToArray());

        /// <summary>
        /// Gets a shared base URI for all xAPI content paths.
        /// </summary>
        
        public const string XAPI_BASE_URI = "https://omilaxr-ecosystem.github.io/xapi/";
        public static Uri BaseUri => new Uri(new Uri(XAPI_BASE_URI), "definitions");

        /// <summary>
        /// Creates the full file name (override in subclasses to enforce naming conventions)
        /// </summary>
        /// <param name="name">Name of the file without extensions</param>
        /// <returns>File name with extension</returns>
        protected virtual string CreateFileName(string name) => name + ".cs";

        /// <summary>
        /// Generates Activities files from a context
        /// </summary>
        /// <param name="context">Context to generate from</param>
        /// <returns>Generated files</returns>
        public virtual IEnumerable<FileMeta> GenerateActivitiesFiles(xAPIContext context)
        {
            return GenerateActivitiesFiles(context.Name, context.Activities);
        }

        /// <summary>
        /// Generates Activities files from a context and a list of activities
        /// </summary>
        /// <param name="context">Context to create activities files of</param>
        /// <param name="activities">Activities to create files of</param>
        /// <param name="name">Name of the class</param>
        /// <returns>List of generated files</returns>
        protected virtual IEnumerable<FileMeta> GenerateActivitiesFiles(string context,
            xAPIDefinitionsList<xAPIActivity> activities, string name = default)
        {
            var codeFiles = new List<FileMeta>();

            if (string.IsNullOrEmpty(name))
            {
                name = BuildActivitiesClassName(context.Capitalize());
            }

            var fileName = CreateFileName(name);
            Log($"{GetType().Name} generating: {fileName}");

            var classesImport = new List<string>();

            foreach (var subDefinitions in activities.SubDefinitions)
            {
                var subName = ExtendClassName(name, subDefinitions.Key.Capitalize());
                codeFiles.AddRange(GenerateActivitiesFiles(context, subDefinitions.Value, subName));
                classesImport.Add(subName);
            }

            var cls = CreateDefinitionsClass(context, name,
                    activities, ActivityClassName, ActivitiesClassName);
            var root = new Root(cls, Namespace)
                .AddImports(CreateActivitiesImports(classesImport));

            codeFiles.Add(new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root)));

            return codeFiles;
        }

        /// <summary>
        /// Generates Verbs files from a context
        /// </summary>
        /// <param name="context">Context to generate from</param>
        /// <returns>Generated files</returns>
        public virtual IEnumerable<FileMeta> GenerateVerbsFiles(xAPIContext context)
        {
            return GenerateVerbsFiles(context.Name, context.Verbs);
        }

        protected virtual IEnumerable<FileMeta> GenerateVerbsFiles(string context, xAPIDefinitionsList<xAPIVerb> verbs,
            string name = default)
        {
            var codeFiles = new List<FileMeta>();

            if (string.IsNullOrEmpty(name))
            {
                name = BuildVerbsClassName(context.Capitalize());
            }

            var fileName = CreateFileName(name);
            Log($"{GetType().Name} generating: {fileName}");

            var classesImport = new List<string>();

            foreach (var subDefinitions in verbs.SubDefinitions)
            {
                var subName = ExtendClassName(name, subDefinitions.Key.Capitalize());
                codeFiles.AddRange(GenerateVerbsFiles(context, subDefinitions.Value, subName));
                classesImport.Add(subName);
            }

            var cls = CreateDefinitionsClass(context, name,
                verbs, VerbClassName, VerbsClassName);

            var root = new Root(cls, Namespace)
                .AddImports(CreateVerbsImports(classesImport));

            codeFiles.Add(new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root)));

            return codeFiles;
        }

        /// <summary>
        /// Generates Extensions files from a context
        /// </summary>
        /// <param name="context">Context to generate from</param>
        /// <param name="type">Type to generate for</param>
        /// <param name="typeSchema"></param>
        /// <returns>Generated files</returns>
        public virtual IEnumerable<FileMeta> GenerateExtensionsFiles(xAPIContext context, string type, TypeSchema typeSchema = null)
        {
            return !context.Extensions.ContainsKey(type) ? Array.Empty<FileMeta>() : GenerateExtensionsFiles(context.Name, type, context.Extensions[type], typeSchema);
        }

        private IEnumerable<FileMeta> GenerateExtensionsFiles(string context, string type,
            xAPIDefinitionsList<xAPIExtension> extensions, TypeSchema typeSchema = null, string name = null)
        {
            var codeFiles = new List<FileMeta>();

            var typCap = type.Capitalize();
            var baseName = BuildExtensionTypeClassName(typCap);

            if (string.IsNullOrEmpty(name))
            {
                name = BuildExtensionsClassName(typCap, context.Capitalize());
            }

            var fileName = CreateFileName(name);
            Log($"{GetType().Name} generating: {fileName}");

            var classesImport = new List<string>();

            foreach (var subExtensions in extensions.SubDefinitions)
            {
                var subName = ExtendClassName(name, subExtensions.Key.Capitalize());
                codeFiles.AddRange(GenerateExtensionsFiles(context, type, subExtensions.Value, typeSchema, subName));
                classesImport.Add(subName);
            }

            var cls = new Class(name, baseName,
                    $"Provides all extensions of the context {context} of type {type} as public properties.")
                .AddModifier(Modifier.Public)
                .AddModifier(Modifier.Sealed)
                .AddProperties(CreateExtensionsProperties(name, extensions))
                .AddConstructor(new Constructor(name)
                    .AddModifier(Modifier.Public)
                    .AddBaseArgument(new Argument($"\"{context}\""))
                    .WithBody(new MethodBody()));
                
            cls.AddMethods(CreateExtensionsMethods(cls, extensions, type, name, typeSchema: typeSchema));
            
            var root = new Root(cls, Namespace)
                .AddImports(CreateExtensionsImports(type, classesImport));

            codeFiles.Add(new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root)));

            return codeFiles;
        }

        /// <summary>
        /// Creates properties for an extension file
        /// </summary>
        /// <param name="name">Name of the class</param>
        /// <param name="extensions">List of extensions to create properties of</param>
        /// <returns>List of created properties</returns>
        protected virtual IEnumerable<Property> CreateExtensionsProperties(string name,
            xAPIDefinitionsList<xAPIExtension> extensions)
            => extensions.SubDefinitions
                .Select(subExtensions
                    => new Property(
                            name: subExtensions.Key,
                            type: ExtendClassName(name, subExtensions.Key.Capitalize()))
                        .AddModifier(Modifier.Public)
                ).ToList();

        /// <summary>
        /// Creates Methods for an extension file
        /// </summary>
        /// <param name="owner">Owner class of these methods.</param>
        /// <param name="extensions">List of extensions to create methods of</param>
        /// <param name="type"></param>
        /// <param name="name">Name of the class</param>
        /// <param name="typeSchema"></param>
        /// <returns>List of created methods</returns>
        protected virtual IEnumerable<Method> CreateExtensionsMethods(Class owner, xAPIDefinitionsList<xAPIExtension> extensions,
            string type, string name, TypeSchema typeSchema = null)
        {
            var methods = new List<Method>();
            var defaultType = typeof(object);
            foreach (var extension in extensions.Definitions)
            {
               var path = $"{extension.Context}/extensions/{type}/{extension.DefName}";
               var types = typeSchema?.GetTypesFromKey(path);
               if (types != null && (types.Length > 1 || types.Length == 1 && types[0].Type != defaultType))
               {
                   Debug.Log($"[xAPI4Unity]: Recognized types for '{path}': " +
                             string.Join(",", types.Select(t => t.GetTypeSyntax())));
               }
               else
               {
                   types = new [] { new ResolvedType() };
               }
               
               foreach (var t in types)
               {
                   var method = new Method(extension.DefName, name)
                       .AddModifier(Modifier.Public)
                       .AddParameter(new Parameter("value", t.GetTypeSyntax()))
                       .WithComment(extension.GetDescription("en-US", true) + $"\nURI: {GetUri(extension, type)}")
                       .WithBody(CreateExtensionsMethodBody(owner, extension, t));
                   methods.Add(method);
               }
            }

            return methods.ToList();
        }

        /// <summary>
        /// Generates a Context file from a context
        /// </summary>
        /// <param name="context">Context to generate from</param>
        /// <returns>Generated file</returns>
        public virtual FileMeta GenerateContextFile(xAPIContext context)
        {
            var cxtCap = context.Name.Capitalize();
            var name = BuildContextClassName(cxtCap);
            var fileName = CreateFileName(name);
            Log($"{GetType().Name} generating: {fileName}");

            var extensionsSum = context.Extensions.Aggregate(0, (i, pair) => i + pair.Value.DeepCount);
            var extensionsDetails = context.Extensions.Select(e => $"{e.Value.DeepCount} in {e.Key}");
            var cls = new Class(name, ContextClassName,
                    $"Provides the definitions of the context {context.Name} as public properties.")
                .AddModifier(Modifier.Public)
                .AddProperty(
                    new Property("verbs", BuildVerbsClassName(cxtCap))
                        .AddModifier(Modifier.Public)
                        .AddModifier(Modifier.ReadOnly)
                        .WithComment(
                            $"{context.Verbs.Count} verbs of '{context.Name}'.\nURI: {GetUri(context, "verbs")}")
                )
                .AddProperty(
                    new Property("activities", BuildActivitiesClassName(cxtCap))
                        .AddModifier(Modifier.Public)
                        .AddModifier(Modifier.ReadOnly)
                        .WithComment(
                            $"{context.Activities.Count} activities of '{context.Name}'.\nURI: {GetUri(context, "activities")}")
                )
                .AddProperty(
                    new Property("extensions", BuildContextExtensionsClassName(cxtCap))
                        .AddModifier(Modifier.Public)
                        .AddModifier(Modifier.ReadOnly)
                        .WithComment(
                            $"{extensionsSum} extensions of '{context.Name}': {string.Join(", ", extensionsDetails)}.\nURI: {GetUri(context, "extensions")}")
                )
                .AddConstructor(new Constructor(name)
                    .AddModifier(Modifier.Public)
                    .AddBaseArgument(new Argument($"\"{context.Name}\"")));

            var root = new Root(cls, Namespace)
                .AddImports(CreateContextImports(context));

            return new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root));
        }

        /// <summary>
        /// Generates ContextExtensions file from a context
        /// </summary>
        /// <param name="context">Context to generate from</param>
        /// <returns>Generated file</returns>
        public virtual FileMeta GenerateContextExtensionsFile(xAPIContext context)
        {
            var cxtCap = context.Name.Capitalize();
            var name = BuildContextExtensionsClassName(cxtCap);
            var fileName = CreateFileName(name);
            Log($"{GetType().Name} generating: {fileName}");

            var extensionsSum = context.Extensions.Aggregate(0, (i, pair) => i + pair.Value.DeepCount);
            var extensionsDetails = context.Extensions.Select(e => $"{e.Value.DeepCount} in {e.Key}");

            var getableProperties = context.ExtensionTypes
                .Select(extType
                    => new GetableProperty(extType, BuildExtensionsClassName(extType.Capitalize(), cxtCap))
                        .AddModifier(Modifier.Public)
                        .WithComment(
                            $"{extensionsSum} extensions of '{context.Name}': {string.Join(", ", extensionsDetails)}.\nURI: {GetUri(context, "extensions", extType)}")
                        .WithGetter(new Getter(CreateContextExtensionsGetterBody(context, extType))))
                .ToList();

            var cls = new Class(name, null,
                    $"Provides the extensions of the context {context.Name} as public properties.")
                .AddModifier(Modifier.Public)
                .AddModifier(Modifier.Sealed)
                .AddConstructor(new Constructor(name)
                    .AddModifier(Modifier.Public))
                .AddGetableProperties(getableProperties);
            
            var root = new Root(cls, Namespace)
                .AddImports(CreateContextExtensionsImports(context));
            
            return new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root));
        }

        /// <summary>
        /// Generates a non-static Definitions file from a context
        /// </summary>
        /// <param name="contexts">Contexts to generate from</param>
        /// <returns>Generated file</returns>
        public virtual FileMeta GenerateContextsFile(IEnumerable<xAPIContext> contexts)
        {
            var fileName = CreateFileName(ContextsClassName);
            Log($"{GetType().Name} generating: {fileName}");
            var xAPIContexts = contexts.ToList();
            var properties = xAPIContexts
                .Select(context
                    => new Property(context.Name.ClearName(),
                            type: BuildContextClassName(context.Name.Capitalize()))
                        .AddModifier(Modifier.Public)
                        .AddModifier(Modifier.ReadOnly)
                        .WithComment(context.Description + $"\nURI: {GetUri(context.Name)}")
                )
                .ToList();

            var cls = new Class(ContextsClassName, null,
                    $"Class that provides all contexts as public properties.")
                .AddModifier(Modifier.Public)
                .AddProperties(properties);
            var root = new Root(cls, Namespace)
                .AddImports(CreateDefinitionsImports(xAPIContexts));
            return new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root));
        }
        
        /// <summary>
        /// Generates a Definitions file from a context
        /// </summary>
        /// <param name="contexts">Contexts to generate from</param>
        /// <returns>Generated file</returns>
        public virtual FileMeta GenerateDefinitionsFile(IEnumerable<xAPIContext> contexts)
        {
            var fileName = CreateFileName(DefinitionsClassName);
            Log($"{GetType().Name} generating: {fileName}");
            var xAPIContexts = contexts as xAPIContext[] ?? contexts.ToArray();
            var properties = xAPIContexts
                .Select(context
                    => new Property(context.Name.ClearName(),
                            type: BuildContextClassName(context.Name.Capitalize()))
                        .AddModifier(Modifier.Public)
                        .AddModifier(Modifier.Static)
                        .AddModifier(Modifier.ReadOnly)
                        .WithComment(context.Description + $"\nURI: {GetUri(context.Name)}")
                )
                .ToList();

            var cls = new Class(DefinitionsClassName, null,
                    $"Static class that provides all contexts as public properties.")
                .AddModifier(Modifier.Public)
                .AddModifier(Modifier.Static)
                .AddProperties(properties);
            var root = new Root(cls, Namespace)
                .AddImports(CreateDefinitionsImports(xAPIContexts));
            return new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root));
        }

        /// <summary>
        /// Generates ExtensionType file from a context
        /// </summary>
        /// <param name="type">Type to generate from</param>
        /// <returns>Generated file</returns>
        public virtual FileMeta GenerateExtensionTypeFile(string type)
        {
            var name = BuildExtensionTypeClassName(type.Capitalize());
            var fileName = CreateFileName(name);
            Log($"{GetType().Name} generating: {fileName}");

            var cls = new Class(name, ExtensionsClassName,
                    $"Provides all extensions of type {type} as public properties.")
                .AddModifier(Modifier.Public)
                .AddConstructor(new Constructor(name)
                    .AddModifier(Modifier.Public)
                    .AddParameter(new Parameter("context", "string"))
                    .AddBaseArgument(new Argument($"\"{type}\""))
                    .AddBaseArgument(new Argument("context")))
                .AddConstructor(new Constructor(name)
                    .AddModifier(Modifier.Public)
                    .AddBaseArgument(new Argument($"\"{type}\""))
                    .AddBaseArgument(new Argument(new Null())));
            
            var root = new Root(cls, Namespace)
                .AddImports(CreateExtensionTypeImports());
            
            return new FileMeta(
                fileName,
                null,
                Serializer.RootToString(root));
        }

        /// <summary>
        /// Creates the imports for Activities files
        /// </summary>
        /// <returns>Created imports</returns>
        protected abstract IEnumerable<Import> CreateActivitiesImports(List<string> classes);

        /// <summary>
        /// Creates the imports for Verbs files
        /// </summary>
        /// <returns>Created imports</returns>
        protected abstract IEnumerable<Import> CreateVerbsImports(List<string> classes);

        /// <summary>
        /// Creates the imports for Extensions files
        /// </summary>
        /// <param name="type">Type of the extensions</param>
        /// <param name="classes">List of class names.</param>
        /// <returns>Created imports</returns>
        protected abstract IEnumerable<Import> CreateExtensionsImports(string type, List<string> classes);

        /// <summary>
        /// Creates the imports for Context files
        /// </summary>
        /// <param name="context">Context of the file</param>
        /// <returns>Created imports</returns>
        protected abstract IEnumerable<Import> CreateContextImports(xAPIContext context);

        /// <summary>
        /// Creates the imports for ContextExtensions files
        /// </summary>
        /// <param name="context">Context of the file</param>
        /// <returns>Created imports</returns>
        protected abstract IEnumerable<Import> CreateContextExtensionsImports(xAPIContext context);

        /// <summary>
        /// Creates the imports for Definitions files
        /// </summary>
        /// <param name="contexts">Contexts of the definitions</param>
        /// <returns>Created imports</returns>
        protected abstract IEnumerable<Import> CreateDefinitionsImports(IEnumerable<xAPIContext> contexts);

        /// <summary>
        /// Creates the imports for ExtensionType files
        /// </summary>
        /// <returns>Created imports</returns>
        protected abstract IEnumerable<Import> CreateExtensionTypeImports();

        /// <summary>
        /// Creates a MethodBody for Extensions files
        /// </summary>
        /// <param name="owner">Owner class of this method.</param>
        /// <param name="extension">Extension to create the body for</param>
        /// <param name="t">Resolved a type of TypeSchema.</param>
        /// <returns>Created MethodBody</returns>
        protected abstract MethodBody CreateExtensionsMethodBody(Class owner, xAPIExtension extension, ResolvedType t = null);

        /// <summary>
        /// Creates a MethodBody for the getter of a ContextExtensions file
        /// </summary>
        /// <param name="context">Context to create the file for</param>
        /// <param name="type">ExtensionType of the Getter</param>
        /// <returns>Created MethodBody</returns>
        protected abstract MethodBody CreateContextExtensionsGetterBody(xAPIContext context, string type);
    }
}
#endif