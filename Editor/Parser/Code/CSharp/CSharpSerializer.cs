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
        // Maps internal modifier flags to their C# keyword representations (order preserved by usage site).
        protected override IDictionary<Modifier, string> Modifiers { get; } = new Dictionary<Modifier, string> {
            [Modifier.Public] = "public",
            [Modifier.Private] = "private",
            [Modifier.Protected] = "protected",
            [Modifier.Static] = "static",
            [Modifier.Virtual] = "virtual",
            [Modifier.ReadOnly] = "readonly",
            [Modifier.Sealed] = "sealed"
        };

        // Formats a string->string dictionary as a C# Dictionary<string, string> initializer with proper indentation.
        public override string DictionaryToString(IDictionary<string, string> dict, int indentDepth) {
            var str = $"new Dictionary<string, string> {{{Environment.NewLine}";
            var count = 0;
            if (dict != null) {
                foreach (var pair in dict) {
                    ++count;
                    // Emit each entry on its own line, escaping values and adding a trailing comma when needed.
                    str += string.Format("{0}[\"{1}\"] = \"{2}\"{3}",
                        CodeGenerator.Indent(indentDepth + 1),
                        pair.Key,
                        pair.Value.ClearString(),
                        count == dict.Count ? string.Empty : $",{Environment.NewLine}");
                }
            }
            str += " }";
            return str;
        }

        // Converts the root node into a complete C# file string:
        // - emits usings
        // - wraps class into namespace if provided
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
            importStr += Environment.NewLine;
            
            if (!string.IsNullOrEmpty(root?.Namespace)) {
                // If a namespace is provided, adjust format string and indentation for nested content
                formatStr = NamespaceToString(root.Namespace);
                ++indent;
            }

            var classStr = ClassToString(root?.Class, indent);

            return importStr + string.Format(formatStr, classStr);
        }

        // Builds a namespace wrapper with a slot ({0}) for inner content.
        private static string NamespaceToString(string @namespace) {
            return $"namespace {@namespace} {{{{{Environment.NewLine}{{0}}{Environment.NewLine}}}}}";
        }

        // Renders a single using directive with the requested indentation depth.
        protected override string ImportToString(Import import, int indentDepth) {
            return $"{CodeGenerator.Indent(indentDepth)}using {import?.Target};{Environment.NewLine}";
        }

        // Emits a C# class declaration, including modifiers, optional base class, XML comment if present,
        // and the serialized class contents (members).
        protected override string ClassToString(Class cls, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var baseStr = string.IsNullOrEmpty(cls?.BaseClass) ? "" : $" : {cls.BaseClass}";
            var classStr = $"{indentStr}{ModifiersToString(cls?.Modifiers)}class {cls?.Name}{baseStr} {{{Environment.NewLine}" +
                ClassContentsToString(cls, indentDepth + 1) +
                $"{indentStr}}}";
            if (!string.IsNullOrEmpty(cls?.Comment)) {
                // Prepend XML doc comment when provided
                classStr = CommentToString(cls.Comment, indentDepth) + classStr;
            }
            return classStr;
        }

        // Serializes an auto-get property with a custom getter body and optional XML comment.
        protected override string GetablePropertyToString(GetableProperty property, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var indentStr2 = CodeGenerator.Indent(indentDepth + 1);
            var getableStr = $"{indentStr}{ModifiersToString(property?.Modifiers)}{property?.Type} {property?.Name} {{{Environment.NewLine}" +
                $"{indentStr2}get {{{Environment.NewLine}" +
                MethodBodyToString(property?.Getter?.Body, indentDepth + 2) +
                $"{indentStr2}}}{Environment.NewLine}" +
                $"{indentStr}}}{Environment.NewLine}";
            
            if (!string.IsNullOrEmpty(property?.Comment)) {
                // Prepend XML doc comment when provided
                getableStr = CommentToString(property.Comment, indentDepth) + getableStr;
            }

            return getableStr;
        }

        // Serializes a property assignment:
        // - If Initializer is provided, emit a direct assignment.
        // - Otherwise, emit a 'new Type(args)' construction using named/indented arguments.
        protected override string PropertyToString(Property property, int indentDepth)
        {
            string propStr;

            if (!string.IsNullOrWhiteSpace(property?.Initializer))
            {
                propStr = $"{CodeGenerator.Indent(indentDepth)}{ModifiersToString(property.Modifiers)}{property.Type} {property.Name} = {property.Initializer};{Environment.NewLine}";
            }
            else
            {
                var argsStr = ArgumentsToString(property?.Arguments, indentDepth + 1, true, true);
                if (argsStr != "")
                {
                    // Place arguments on the next indented line when present
                    argsStr = $"{Environment.NewLine}{CodeGenerator.Indent(indentDepth + 1)}{argsStr}";
                }

                propStr = $"{CodeGenerator.Indent(indentDepth)}{ModifiersToString(property?.Modifiers)}{property?.Type} {property?.Name} = " +
                          $"new {property?.Type}({argsStr});{Environment.NewLine}";
            }

            if (!string.IsNullOrEmpty(property?.Comment))
            {
                // Prepend XML doc comment for the property
                propStr = PropertyCommentToString(property, indentDepth) + propStr;
            }

            return propStr;
        }
        
        // Serializes a constructor including optional base(...) call when BaseArguments are provided.
        protected override string ConstructorToString(Constructor constructor, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var baseStr = constructor?.BaseArguments == null || constructor.BaseArguments.Count == 0 ?
                "" :
                $"{Environment.NewLine}{CodeGenerator.Indent(indentDepth + 1)}: base({ArgumentsToString(constructor.BaseArguments, indentDepth + 1)}) ";
            return $"{indentStr}{ModifiersToString(constructor?.Modifiers)}{constructor?.Type}({ParametersToString(constructor?.Parameters)}) " +
                $"{baseStr}" +
                $"{{{Environment.NewLine}{indentStr}}}{Environment.NewLine}";
        }

        // Serializes a method with modifiers, signature, XML doc comment (if any), and formatted body.
        protected override string MethodToString(Method method, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var mthStr = $"{indentStr}{ModifiersToString(method?.Modifiers)}{method?.Type} {method?.Name}({ParametersToString(method?.Parameters)}) {{{Environment.NewLine}" +
                $"{MethodBodyToString(method?.Body, indentDepth + 1)}{indentStr}}}{Environment.NewLine}";
            
            if (!string.IsNullOrEmpty(method?.Comment)) {
                // Prepend XML doc comment when present
                mthStr = CommentToString(method.Comment, indentDepth) + mthStr;
            }

            return mthStr;
        }

        // Joins method/constructor arguments into a single string.
        // - When 'withName' is true, prefixes each value with `name: `.
        // - When 'newLines' is true, breaks arguments into separate lines with proper indentation.
        // - Dictionary values are serialized via DictionaryToString; Null via NullToString.
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
                    str += $",{(newLines ? Environment.NewLine + CodeGenerator.Indent(indentDepth) : " ")}";
                }
            }
            return str;
        }

        // Serializes a parameter list, including default values (with proper null literal).
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
        
        // Builds an XML summary comment for a property, preserving line breaks and indentation.
        protected override string PropertyCommentToString(Property property, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var str = $"{indentStr}/// <summary>{Environment.NewLine}";
            var commentLines = property?.Comment.Split(Environment.NewLine.ToCharArray());
            if (commentLines != null)
            {
                str = commentLines.Aggregate(str, (current, line) => current + $"{indentStr}/// {line}{Environment.NewLine}");
            }
            str += $"{indentStr}/// </summary>{Environment.NewLine}";
            return str;
        }

        // Builds an XML summary comment block, preserving line breaks and indentation.
        protected override string CommentToString(string comment, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var str = $"{indentStr}/// <summary>{Environment.NewLine}";
            var commentLines = comment?.Split(Environment.NewLine.ToCharArray());
            if (commentLines != null)
            {
                str = commentLines.Aggregate(str, (current, line) => current + $"{indentStr}/// {line}{Environment.NewLine}");
            }
            str += $"{indentStr}/// </summary>{Environment.NewLine}";
            return str;
        }
    }
}
#endif