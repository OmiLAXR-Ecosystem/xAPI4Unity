/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.Code.SyntaxTree;

namespace xAPI4Unity.Editor.Parser.Code.CSharp {
    /// <summary>
    /// Serializes C# source code out of a syntax tree
    /// </summary>
    internal class CSharpSerializer : SyntaxTreeSerializer {
        /// <summary>
        /// Maps the modifiers to their C# keywords.
        /// </summary>
        protected override IDictionary<Modifier, string> Modifiers { get; } = new Dictionary<Modifier, string> {
            [Modifier.Public] = "public",
            [Modifier.Private] = "private",
            [Modifier.Protected] = "protected",
            [Modifier.Static] = "static",
            [Modifier.Virtual] = "virtual",
            [Modifier.ReadOnly] = "readonly",
            [Modifier.Sealed] = "sealed"
        };

        /// <summary>
        /// Formats a string-to-string dictionary as a C# <c>Dictionary&lt;string, string&gt;</c> initializer with proper indentation.
        /// </summary>
        public override string DictionaryToString(IDictionary<string, string> dict, int indentDepth) {
            var str = $"new Dictionary<string, string> {{{CodeGenerator.NewLine}";
            var count = 0;
            if (dict != null) {
                foreach (var pair in dict) {
                    ++count;
                    // Emit each entry on its own line, escaping values and adding a trailing comma when needed.
                    str += string.Format("{0}[\"{1}\"] = \"{2}\"{3}",
                        CodeGenerator.Indent(indentDepth + 1),
                        pair.Key,
                        pair.Value.ClearString(),
                        count == dict.Count ? string.Empty : $",{CodeGenerator.NewLine}");
                }
            }
            str += " }";
            return str;
        }

        /// <summary>
        /// Converts the root node into a complete C# file string:
        /// - emits usings
        /// - wraps class into namespace if provided
        /// </summary>
        public override string RootToString(Root root) {
            var indent = 0;
            var formatStr = "{0}";
            
            var importStr = string.Empty;
            if (root?.Imports != null)
            {
                // Aggregate all import lines at the top of a file
                importStr = root.Imports
                    .Aggregate(importStr, (current, import) 
                        => current + ImportToString(import, indent));
            }
            importStr += CodeGenerator.NewLine;
            
            if (!string.IsNullOrEmpty(root?.Namespace)) {
                // If a namespace is provided, adjust format string and indentation for nested content
                formatStr = NamespaceToString(root.Namespace);
                ++indent;
            }

            var classStr = ClassToString(root?.Class, indent);

            return importStr + string.Format(formatStr, classStr);
        }

        /// <summary>
        /// Builds a namespace wrapper with a slot ({0}) for inner content.
        /// </summary>
        private static string NamespaceToString(string @namespace) {
            return $"namespace {@namespace} {{{{{CodeGenerator.NewLine}{{0}}{CodeGenerator.NewLine}}}}}";
        }

        /// <summary>
        /// Renders a single using directive with the requested indentation depth.
        /// </summary>
        protected override string ImportToString(Import import, int indentDepth) {
            return $"{CodeGenerator.Indent(indentDepth)}using {import?.Target};{CodeGenerator.NewLine}";
        }

        /// <summary>
        /// Emits a C# class declaration, including modifiers, optional base class, XML comment if present,
        /// and the serialized class contents (members).
        /// </summary>
        protected override string ClassToString(Class cls, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var baseStr = string.IsNullOrEmpty(cls?.BaseClass) ? "" : $" : {cls.BaseClass}";
            var classStr = $"{indentStr}{ModifiersToString(cls?.Modifiers)}class {cls?.Name}{baseStr} {{{CodeGenerator.NewLine}" +
                ClassContentsToString(cls, indentDepth + 1) +
                $"{indentStr}}}";
            if (!string.IsNullOrEmpty(cls?.Comment)) {
                // Prepend XML doc comment when provided
                classStr = CommentToString(cls.Comment, indentDepth) + classStr;
            }
            return classStr;
        }

        /// <summary>
        /// Serializes an auto-get property with a custom getter body and optional XML comment.
        /// </summary>
        protected override string GetablePropertyToString(GetableProperty property, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var indentStr2 = CodeGenerator.Indent(indentDepth + 1);
            var getableStr = $"{indentStr}{ModifiersToString(property?.Modifiers)}{property?.Type} {property?.Name} {{{CodeGenerator.NewLine}" +
                $"{indentStr2}get {{{CodeGenerator.NewLine}" +
                MethodBodyToString(property?.Getter?.Body, indentDepth + 2) +
                $"{indentStr2}}}{CodeGenerator.NewLine}" +
                $"{indentStr}}}{CodeGenerator.NewLine}";
            
            if (!string.IsNullOrEmpty(property?.Comment)) {
                // Prepend XML doc comment when provided
                getableStr = CommentToString(property.Comment, indentDepth) + getableStr;
            }

            return getableStr;
        }

        /// <summary>
        /// Serializes a property assignment:
        /// - If Initializer is provided, emit a direct assignment.
        /// - Otherwise, emit a 'new Type(args)' construction using named/indented arguments.
        /// </summary>
        protected override string PropertyToString(Property property, int indentDepth)
        {
            string propStr;

            var propName = property!.Name;
            
            if (propName.Contains('-'))
                propName = propName.Replace('-', '_');
            
            if (!string.IsNullOrWhiteSpace(property?.Initializer))
            {
                propStr = $"{CodeGenerator.Indent(indentDepth)}{ModifiersToString(property.Modifiers)}{property.Type} {propName} = {property.Initializer};{CodeGenerator.NewLine}";
            }
            else
            {
                var argsStr = ArgumentsToString(property?.Arguments, indentDepth + 1, true, true);
                if (argsStr != "")
                {
                    // Place arguments on the next indented line when present
                    argsStr = $"{CodeGenerator.NewLine}{CodeGenerator.Indent(indentDepth + 1)}{argsStr}";
                }

                propStr = $"{CodeGenerator.Indent(indentDepth)}{ModifiersToString(property.Modifiers)}{property.Type} {propName} = " +
                          $"new {property?.Type}({argsStr});{CodeGenerator.NewLine}";
            }

            if (!string.IsNullOrEmpty(property?.Comment))
            {
                // Prepend XML doc comment for the property
                propStr = PropertyCommentToString(property, indentDepth) + propStr;
            }

            return propStr;
        }
        
        /// <summary>
        /// Serializes a constructor including optional base(...) call when BaseArguments are provided.
        /// </summary>
        protected override string ConstructorToString(Constructor constructor, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var baseStr = constructor?.BaseArguments == null || constructor.BaseArguments.Count == 0 ?
                "" :
                $"{CodeGenerator.NewLine}{CodeGenerator.Indent(indentDepth + 1)}: base({ArgumentsToString(constructor.BaseArguments, indentDepth + 1)}) ";
            return $"{indentStr}{ModifiersToString(constructor?.Modifiers)}{constructor?.Type}({ParametersToString(constructor?.Parameters)}) " +
                $"{baseStr}" +
                $"{{{CodeGenerator.NewLine}{indentStr}}}{CodeGenerator.NewLine}";
        }

        /// <summary>
        /// Serializes a method with modifiers, signature, XML doc comment (if any), and formatted body.
        /// </summary>
        protected override string MethodToString(Method method, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var mthStr = $"{indentStr}{ModifiersToString(method?.Modifiers)}{method?.Type} {method?.Name}({ParametersToString(method?.Parameters)}) {{{CodeGenerator.NewLine}" +
                $"{MethodBodyToString(method?.Body, indentDepth + 1)}{indentStr}}}{CodeGenerator.NewLine}";
            
            if (!string.IsNullOrEmpty(method?.Comment)) {
                // Prepend XML doc comment when present
                mthStr = CommentToString(method.Comment, indentDepth) + mthStr;
            }

            return mthStr;
        }

        /// <summary>
        /// Joins method/constructor arguments into a single string.
        /// - When 'withName' is true, prefixes each value with `name: `.
        /// - When 'newLines' is true, breaks arguments into separate lines with proper indentation.
        /// - Dictionary values are serialized via DictionaryToString; Null via NullToString.
        /// </summary>
        protected override string ArgumentsToString(IReadOnlyList<Argument> args, int indentDepth, bool newLines = false, bool withName = false) {
            var str = string.Empty;
            if (args == null) return str;
            for (var i = 0; i < args.Count; ++i) {
                if (withName) {
                    str += args[i].Name + ": ";
                }
                str += args[i].Value is Dictionary<string, string> dict ? DictionaryToString(dict, indentDepth)
                    : (args[i].Value is Null n ? NullToString(n) : args[i].Value);
                if (i < args.Count - 1) {
                    // Add comma + either space or newline+indent based on 'newLines'
                    str += $",{(newLines ? CodeGenerator.NewLine + CodeGenerator.Indent(indentDepth) : " ")}";
                }
            }
            return str;
        }

        /// <summary>
        /// Serializes a parameter list, including default values (with proper null literal).
        /// </summary>
        protected override string ParametersToString(IReadOnlyList<Parameter> parameters) {
            var str = string.Empty;
            if (parameters == null) return str;
            for (var i = 0; i < parameters.Count; ++i) {
                str += $"{parameters[i].Type} {parameters[i].Name}";
                if (parameters[i].DefaultValue != null) {
                    str += $" = {(parameters[i].DefaultValue is Null n ? NullToString(n) : parameters[i].DefaultValue)}";
                }
                if (i < parameters.Count - 1) {
                    str += ", ";
                }
            }
            return str;
        }
        
        /// <summary>
        /// Builds an XML summary comment for a property, preserving line breaks and indentation.
        /// </summary>
        protected override string PropertyCommentToString(Property property, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var str = $"{indentStr}/// <summary>{CodeGenerator.NewLine}";
            var commentLines = property?.Comment == null ? null : CodeGenerator.SplitLines(property.Comment);
            if (commentLines != null)
            {
                str = commentLines.Aggregate(str, (current, line) => current + $"{indentStr}/// {line}{CodeGenerator.NewLine}");
            }
            str += $"{indentStr}/// </summary>{CodeGenerator.NewLine}";
            return str;
        }

        /// <summary>
        /// Builds an XML summary comment block, preserving line breaks and indentation.
        /// </summary>
        protected override string CommentToString(string comment, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var str = $"{indentStr}/// <summary>{CodeGenerator.NewLine}";
            var commentLines = comment == null ? null : CodeGenerator.SplitLines(comment);
            if (commentLines != null)
            {
                str = commentLines.Aggregate(str, (current, line) => current + $"{indentStr}/// {line}{CodeGenerator.NewLine}");
            }
            str += $"{indentStr}/// </summary>{CodeGenerator.NewLine}";
            return str;
        }
    }
}
#endif