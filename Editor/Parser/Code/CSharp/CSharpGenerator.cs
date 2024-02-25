using System;
using System.Collections.Generic;
using xAPI4Unity.Editor.Parser.Code.SyntaxTree;
using xAPI4Unity.Editor.Parser.Definitions;

namespace xAPI4Unity.Editor.Parser.Code.CSharp
{
    /// <summary>
    /// Generates C# code files from the xAPI definitions
    /// </summary>
    public sealed class CSharpGenerator : CodeGenerator
    {
        protected override string Namespace { get; }
        
        /// <summary>
        /// Creates a CSharpGenerator
        /// </summary>
        /// <param name="namespace">Namespace</param>
        public CSharpGenerator(string @namespace)
            : base(new CSharpSerializer())
        {
            Namespace = @namespace;
        }

        protected override IEnumerable<Import> CreateActivitiesImports(List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        protected override IEnumerable<Import> CreateVerbsImports(List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        protected override IEnumerable<Import> CreateExtensionsImports(string type, List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        protected override IEnumerable<Import> CreateContextImports(xAPIContext context)
        {
            return new List<Import>();
        }

        protected override IEnumerable<Import> CreateContextExtensionsImports(xAPIContext context)
        {
            return new List<Import>();
        }

        protected override IEnumerable<Import> CreateDefinitionsImports(IEnumerable<xAPIContext> contexts)
        {
            return new List<Import>();
        }

        protected override IEnumerable<Import> CreateExtensionTypeImports()
        {
            return new List<Import>();
        }

        protected override MethodBody CreateExtensionsMethodBody(xAPIExtension extension)
        {
            var args = $"{Indent(2)}context: Context,{Environment.NewLine}"
                       + $"{Indent(2)}extensionType: ExtensionType,{Environment.NewLine}"
                       + $"{Indent(2)}key: \"{extension.DefName}\",{Environment.NewLine}"
                       + $"{Indent(2)}names: {Serializer.DictionaryToString(extension.Names, 2)},{Environment.NewLine}"
                       + $"{Indent(2)}descriptions: {Serializer.DictionaryToString(extension.Descriptions, 2)}";
            return new MethodBody()
                .AddStatement(new Statement(
                    $"Add(new {ExtensionClassName}({Environment.NewLine}{args}),{Environment.NewLine}{Indent(1)} value);"))
                .AddStatement(new Statement("return this;"));
        }

        protected override MethodBody CreateContextExtensionsGetterBody(xAPIContext context, string type)
        {
            return new MethodBody()
                .AddStatement(new Statement(
                    $"return new {BuildExtensionsClassName(type.Capitalize(), context.Name.Capitalize())}();"));
        }
        
    }
}