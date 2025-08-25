/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using xAPI4Unity.Editor.Parser.Code.SyntaxTree;
using xAPI4Unity.Editor.Parser.Definitions;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Editor.Parser.Code.CSharp
{
    /// <summary>
    /// Generates C# code files from the xAPI definitions
    /// </summary>
    internal sealed class CSharpGenerator : CodeGenerator
    {
        /// <summary>
        /// Target namespace to wrap generated types into.
        /// </summary>
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

        /// <summary>
        /// Creates the set of using directives required for generated Activities code.
        /// Extend this list if Activities generation starts depending on more framework or project types.
        /// </summary>
        /// <param name="classes">List of class names being generated for Activities.</param>
        /// <returns>Sequence of imports to be emitted at the top of the file.</returns>
        protected override IEnumerable<Import> CreateActivitiesImports(List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        /// <summary>
        /// Creates the set of using directives required for generated Verbs code.
        /// Extend this list if Verbs generation starts depending on more framework or project types.
        /// </summary>
        /// <param name="classes">List of class names being generated for Verbs.</param>
        /// <returns>Sequence of imports to be emitted at the top of the file.</returns>
        protected override IEnumerable<Import> CreateVerbsImports(List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        /// <summary>
        /// Creates the set of using directives required for generated Extensions code for a specific type.
        /// </summary>
        /// <param name="type">Extensions type/category (e.g., Activity, Verb, etc.).</param>
        /// <param name="classes">List of class names being generated for this extensions group.</param>
        /// <returns>Sequence of imports to be emitted at the top of the file.</returns>
        protected override IEnumerable<Import> CreateExtensionsImports(string type, List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        /// <summary>
        /// Creates using directives needed for a specific xAPI context file (if any are required).
        /// Currently, returns an empty set (no extra imports).
        /// </summary>
        /// <param name="context">The xAPI context for which code is being generated.</param>
        /// <returns>Sequence of imports to be emitted at the top of the file.</returns>
        protected override IEnumerable<Import> CreateContextImports(xAPIContext context)
        {
            return new List<Import>();
        }

        /// <summary>
        /// Creates using directives needed for the context extensions file (if any are required).
        /// Currently, returns an empty set (no extra imports).
        /// </summary>
        /// <param name="context">The xAPI context for which extension code is being generated.</param>
        /// <returns>Sequence of imports to be emitted at the top of the file.</returns>
        protected override IEnumerable<Import> CreateContextExtensionsImports(xAPIContext context)
        {
            return new List<Import>();
        }

        /// <summary>
        /// Creates using directives needed for the aggregated definitions file.
        /// Currently, returns an empty set (no extra imports).
        /// </summary>
        /// <param name="contexts">All contexts that are part of the generation run.</param>
        /// <returns>Sequence of imports to be emitted at the top of the file.</returns>
        protected override IEnumerable<Import> CreateDefinitionsImports(IEnumerable<xAPIContext> contexts)
        {
            return new List<Import>();
        }

        /// <summary>
        /// Creates using directives needed for the generated ExtensionType enum/class (if any).
        /// Currently, returns an empty set (no extra imports).
        /// </summary>
        /// <returns>Sequence of imports to be emitted at the top of the file.</returns>
        protected override IEnumerable<Import> CreateExtensionTypeImports()
        {
            return new List<Import>();
        }

        /// <summary>
        /// Builds the method body that adds a new extension instance to the owning collection,
        /// optionally emitting validation logic based on a strict resolved type.
        /// - For regex types: creates a cached static, readonly Regex field on the owning class,
        ///   validates the 'value' against it, and throws ArgumentException if it doesn't match.
        /// - For ranged types: validates 'value' is within the specified bounds and throws
        ///   ArgumentOutOfRangeException if it isn't.
        /// Finally, calls Add (new Extension(...), value) and returns 'this' for chaining.
        /// </summary>
        /// <param name="owner">The class that will receive additional members (e.g., the cached Regex field).</param>
        /// <param name="extension">The extension definition being added.</param>
        /// <param name="t">Optional resolved type used for enforcing validation rules (regex/range).</param>
        /// <returns>A MethodBody containing validation (if applicable), the Add call, and a fluent return.</returns>
        protected override MethodBody CreateExtensionsMethodBody(Class owner, xAPIExtension extension, ResolvedType t = null)
        {
            var args = $"{Indent(2)}context: Context,{Environment.NewLine}"
                       + $"{Indent(2)}extensionType: ExtensionType,{Environment.NewLine}"
                       + $"{Indent(2)}key: \"{extension.DefName}\",{Environment.NewLine}"
                       + $"{Indent(2)}names: {Serializer.DictionaryToString(extension.Names, 2)},{Environment.NewLine}"
                       + $"{Indent(2)}descriptions: {Serializer.DictionaryToString(extension.Descriptions, 2)}";
            var methodBody = new MethodBody();

            // If a strict type is provided, generate validation statements.
            if (t != null)
            {
                if (t.IsRegexExpr)
                {
                    var fieldName = "Regex_" + Math.Abs(t.GetHashCode());

                    // Clean pattern (remove surrounding slashes if present)
                    var pattern = t.Definition;
                    if (pattern.StartsWith("/") && pattern.EndsWith("/") && pattern.Length > 1)
                        pattern = pattern.Substring(1, pattern.Length - 2);

                    // Escape for use inside a C# string literal
                    var escapedPattern = pattern.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    
                    // Create a compiled regex and add it as a static, readonly field on the owning class
                    var regexCode = $"new System.Text.RegularExpressions.Regex(@\"{escapedPattern}\", System.Text.RegularExpressions.RegexOptions.Compiled)";
                    var regexProperty = new Property(fieldName, "System.Text.RegularExpressions.Regex")
                        .AddModifier(Modifier.Private)
                        .AddModifier(Modifier.Static)
                        .AddModifier(Modifier.ReadOnly);

                    regexProperty.Initializer = regexCode;

                    owner.AddProperty(regexProperty);

                    // Validate the incoming 'value' using the cached regex
                    var expr = $"if (!{fieldName}.IsMatch(value)) throw new System.ArgumentException(\"value does not match pattern\", \"value\");";
                    methodBody.AddStatement(new Statement(expr));
                }
                else if (t is ResolvedRange range)
                {
                    // Build inclusive/exclusive operators and validate bounds
                    var conditions = new List<string>();

                    // Check if RawMin is defined (not null or "_")
                    if (!string.IsNullOrEmpty(range.RawMin) && range.RawMin != "_")
                    {
                        var minOp = range.MinInclusive ? "<" : "<=";
                        conditions.Add($"value {minOp} {range.RawMin}");
                    }

                    // Check if RawMax is defined (not null or "_")
                    if (!string.IsNullOrEmpty(range.RawMax) && range.RawMax != "_")
                    {
                        var maxOp = range.MaxInclusive ? ">" : ">=";
                        conditions.Add($"value {maxOp} {range.RawMax}");
                    }

                    // If at least one bound is present, emit the validation statement
                    if (conditions.Count > 0)
                    {
                        var expr = $"if ({string.Join(" || ", conditions)}) " +
                                   "throw new System.ArgumentOutOfRangeException(\"value\");";

                        methodBody.AddStatement(new Statement(expr));
                    }
                }
            }
            
            // Add the extension with its metadata and supplied value, then return 'this' for fluent API
            methodBody.AddStatements(
                new Statement($"Add(new {ExtensionClassName}({Environment.NewLine}{args}),{Environment.NewLine}{Indent(1)} value);"), 
                new Statement("return this;"));

            return methodBody;
        }

        /// <summary>
        /// Generates the body of a getter method that provides a new extensions collection instance
        /// for a given context and base type.
        /// </summary>
        /// <param name="context">The xAPI context from which the extension collection is derived.</param>
        /// <param name="type">The base type for which the extension collection class name is built.</param>
        /// <returns>A MethodBody returning a new instance of the computed extensions class.</returns>
        protected override MethodBody CreateContextExtensionsGetterBody(xAPIContext context, string type)
        {
            return new MethodBody()
                .AddStatement(new Statement(
                    $"return new {BuildExtensionsClassName(type.Capitalize(), context.Name.Capitalize())}();"));
        }
        
    }
}
#endif