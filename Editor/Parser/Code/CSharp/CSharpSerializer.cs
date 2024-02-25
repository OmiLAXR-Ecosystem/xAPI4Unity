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
        protected override IDictionary<Modifier, string> Modifiers { get; } = new Dictionary<Modifier, string> {
            [Modifier.Public] = "public",
            [Modifier.Private] = "private",
            [Modifier.Protected] = "protected",
            [Modifier.Static] = "static",
            [Modifier.Virtual] = "virtual",
            [Modifier.ReadOnly] = "readonly",
            [Modifier.Sealed] = "sealed"
        };

        public override string DictionaryToString(IDictionary<string, string> dict, int indentDepth) {
            var str = $"new Dictionary<string, string> {{{Environment.NewLine}";
            var count = 0;
            if (dict != null) {
                foreach (var pair in dict) {
                    ++count;
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

        public override string RootToString(Root root) {
            var indent = 0;
            var formatStr = "{0}";
            
            var importStr = string.Empty;
            if (root?.Imports != null)
            {
                importStr = root.Imports
                    .Aggregate(importStr, (current, import) 
                        => current + ImportToString(import, indent));
            }
            importStr += Environment.NewLine;
            
            if (!string.IsNullOrEmpty(root?.Namespace)) {
                formatStr = NamespaceToString(root?.Namespace);
                ++indent;
            }

            var classStr = ClassToString(root?.Class, indent);

            return importStr + string.Format(formatStr, classStr);
        }

        private static string NamespaceToString(string @namespace) {
            return $"namespace {@namespace} {{{{{Environment.NewLine}{{0}}{Environment.NewLine}}}}}";
        }

        protected override string ImportToString(Import import, int indentDepth) {
            return $"{CodeGenerator.Indent(indentDepth)}using {import?.Target};{Environment.NewLine}";
        }

        protected override string ClassToString(Class cls, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var baseStr = string.IsNullOrEmpty(cls?.BaseClass) ? "" : $" : {cls?.BaseClass}";
            var classStr = $"{indentStr}{ModifiersToString(cls?.Modifiers)}class {cls?.Name}{baseStr} {{{Environment.NewLine}" +
                ClassContentsToString(cls, indentDepth + 1) +
                $"{indentStr}}}";
            if (!string.IsNullOrEmpty(cls?.Comment)) {
                classStr = CommentToString(cls.Comment, indentDepth) + classStr;
            }
            return classStr;
        }

        protected override string GetablePropertyToString(GetableProperty property, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var indentStr2 = CodeGenerator.Indent(indentDepth + 1);
            var getableStr = $"{indentStr}{ModifiersToString(property?.Modifiers)}{property?.Type} {property?.Name} {{{Environment.NewLine}" +
                $"{indentStr2}get {{{Environment.NewLine}" +
                MethodBodyToString(property?.Getter?.Body, indentDepth + 2) +
                $"{indentStr2}}}{Environment.NewLine}" +
                $"{indentStr}}}{Environment.NewLine}";
            
            if (!string.IsNullOrEmpty(property?.Comment)) {
                getableStr = CommentToString(property.Comment, indentDepth) + getableStr;
            }

            return getableStr;
        }

        protected override string PropertyToString(Property property, int indentDepth) {
            var argsStr = ArgumentsToString(property?.Arguments, indentDepth + 1, true, true);
            if (argsStr != "") {
                argsStr = $"{Environment.NewLine}{CodeGenerator.Indent(indentDepth + 1)}{argsStr}";
            }
            var propStr = $"{CodeGenerator.Indent(indentDepth)}{ModifiersToString(property?.Modifiers)}{property?.Type} {property?.Name} = " +
                $"new {property?.Type}({argsStr});{Environment.NewLine}";
            if (!string.IsNullOrEmpty(property?.Comment)) {
                propStr = PropertyCommentToString(property, indentDepth) + propStr;
            }
            return propStr;
        }

        protected override string ConstructorToString(Constructor constructor, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var baseStr = constructor?.BaseArguments == null || constructor?.BaseArguments.Count == 0 ?
                "" :
                $"{Environment.NewLine}{CodeGenerator.Indent(indentDepth + 1)}: base({ArgumentsToString(constructor?.BaseArguments, indentDepth + 1)}) ";
            return $"{indentStr}{ModifiersToString(constructor?.Modifiers)}{constructor?.Type}({ParametersToString(constructor?.Parameters)}) " +
                $"{baseStr}" +
                $"{{{Environment.NewLine}{indentStr}}}{Environment.NewLine}";
        }

        protected override string MethodToString(Method method, int indentDepth) {
            var indentStr = CodeGenerator.Indent(indentDepth);
            var mthStr = $"{indentStr}{ModifiersToString(method?.Modifiers)}{method?.Type} {method?.Name}({ParametersToString(method?.Parameters)}) {{{Environment.NewLine}" +
                $"{MethodBodyToString(method?.Body, indentDepth + 1)}{indentStr}}}{Environment.NewLine}";
            
            if (!string.IsNullOrEmpty(method?.Comment)) {
                mthStr = CommentToString(method.Comment, indentDepth) + mthStr;
            }

            return mthStr;
        }

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
                    str += $",{(newLines ? Environment.NewLine + CodeGenerator.Indent(indentDepth) : " ")}";
                }
            }
            return str;
        }

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