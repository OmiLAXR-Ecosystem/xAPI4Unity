using System;
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.Code.SyntaxTree;

namespace xAPI4Unity.Editor.Parser.Code
{

    /// <summary>
    /// Serializes source code out of a syntax tree
    /// </summary>
    public abstract class SyntaxTreeSerializer
    {
        protected abstract IDictionary<Modifier, string> Modifiers { get; }

        /// <summary>
        /// Serializes a list of modifiers
        /// </summary>
        /// <param name="modifiers">Modifiers</param>
        /// <returns>Serialized modifiers string</returns>
        protected virtual string ModifiersToString(IReadOnlyList<Modifier> modifiers)
        {
            var str = string.Empty;
            if (modifiers != null)
            {
                str = modifiers
                    .Where(modifier => Modifiers.ContainsKey(modifier) && Modifiers[modifier] != "")
                    .Aggregate(str, (current, modifier) => current + $"{Modifiers[modifier]} ");
            }

            return str;
        }

        /// <summary>
        /// Serializes a class
        /// </summary>
        /// <param name="cls">Class</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized class string</returns>
        protected virtual string ClassContentsToString(Class cls, int indentDepth)
        {
            var propsStr = PropertiesToString(cls.Properties, indentDepth);
            var constStr = ConstructorsToString(cls.Constructors, indentDepth);
            var getablePropsStr = GetablePropertiesToString(cls.GetableProperties, indentDepth);
            var methodsStr = MethodsToString(cls.Methods, indentDepth);

            return $"{propsStr}{(constStr == "" ? "" : Environment.NewLine)}{constStr}" +
                   $"{(getablePropsStr == "" ? "" : Environment.NewLine)}{getablePropsStr}" +
                   $"{(methodsStr == "" ? "" : Environment.NewLine)}{methodsStr}";
        }

        /// <summary>
        /// Serializes properties
        /// </summary>
        /// <param name="properties">Properties</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized properties string</returns>
        protected virtual string PropertiesToString(IReadOnlyList<Property> properties, int indentDepth)
        {
            var propsStr = string.Empty;
            if (properties == null) return propsStr;
            for (var i = 0; i < properties.Count; ++i)
            {
                propsStr += PropertyToString(properties[i], indentDepth);
                if (i < properties.Count - 1)
                {
                    propsStr += Environment.NewLine;
                }
            }

            return propsStr;
        }

        /// <summary>
        /// Serializes constructors
        /// </summary>
        /// <param name="constructors">Constructors</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized constructors string</returns>
        protected virtual string ConstructorsToString(IReadOnlyList<Constructor> constructors, int indentDepth)
        {
            var constStr = string.Empty;
            if (constructors == null) return constStr;
            for (var i = 0; i < constructors.Count; ++i)
            {
                constStr += ConstructorToString(constructors[i], indentDepth);
                if (i < constructors.Count - 1)
                {
                    constStr += Environment.NewLine;
                }
            }

            return constStr;
        }

        /// <summary>
        /// Serializes getable properties
        /// </summary>
        /// <param name="properties">Getable properties</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized getable properties string</returns>
        protected virtual string GetablePropertiesToString(IReadOnlyList<GetableProperty> properties, int indentDepth)
        {
            var getablePropsStr = string.Empty;
            if (properties == null) return getablePropsStr;
            for (var i = 0; i < properties.Count; ++i)
            {
                getablePropsStr += GetablePropertyToString(properties[i], indentDepth);
                if (i < properties.Count - 1)
                {
                    getablePropsStr += Environment.NewLine;
                }
            }

            return getablePropsStr;
        }

        /// <summary>
        /// Serializes methods
        /// </summary>
        /// <param name="methods">Methods</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized methods string</returns>
        protected virtual string MethodsToString(IReadOnlyList<Method> methods, int indentDepth)
        {
            var methodsStr = string.Empty;
            if (methods == null) return methodsStr;
            for (var i = 0; i < methods.Count; ++i)
            {
                methodsStr += MethodToString(methods[i], indentDepth);
                if (i < methods.Count - 1)
                {
                    methodsStr += Environment.NewLine;
                }
            }

            return methodsStr;
        }

        /// <summary>
        /// Serializes an import
        /// </summary>
        /// <param name="import">Import</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized import string</returns>
        protected abstract string ImportToString(Import import, int indentDepth);

        /// <summary>
        /// Serializes a class
        /// </summary>
        /// <param name="cls">Class</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized class string</returns>
        protected abstract string ClassToString(Class cls, int indentDepth);

        /// <summary>
        /// Serializes a getable property
        /// </summary>
        /// <param name="property">Property</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized getable property string</returns>
        protected abstract string GetablePropertyToString(GetableProperty property, int indentDepth);

        /// <summary>
        /// Serializes a property
        /// </summary>
        /// <param name="property">Property</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized property string</returns>
        protected abstract string PropertyToString(Property property, int indentDepth);

        /// <summary>
        /// Serializes a constructor
        /// </summary>
        /// <param name="constructor">Constructor</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized constructor string</returns>
        protected abstract string ConstructorToString(Constructor constructor, int indentDepth);

        /// <summary>
        /// Serializes a method
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized method string</returns>
        protected abstract string MethodToString(Method method, int indentDepth);

        /// <summary>
        /// Serializes a list of arguments
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <param name="newLines">Should every argument be in its own line</param>
        /// <param name="withName">Should the name be included in the arguments list (i.e. "name: value")</param>
        /// <returns>Serialized arguments string</returns>
        protected abstract string ArgumentsToString(IReadOnlyList<Argument> args, int indentDepth,
            bool newLines = default, bool withName = default);

        /// <summary>
        /// Serializes a list of parameters
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <returns>Serialized parameters string</returns>
        protected abstract string ParametersToString(IReadOnlyList<Parameter> parameters);

        /// <summary>
        /// Serializes a property comment
        /// </summary>
        /// <param name="property">Property</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized property comment string</returns>
        protected abstract string PropertyCommentToString(Property property, int indentDepth);

        /// <summary>
        /// Serializes a comment
        /// </summary>
        /// <param name="comment">Comment</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized comment string</returns>
        protected abstract string CommentToString(string comment, int indentDepth);

        /// <summary>
        /// Serializes a dictionary
        /// </summary>
        /// <param name="dict">Dictionary</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized dictionary string</returns>
        public abstract string DictionaryToString(IDictionary<string, string> dict, int indentDepth);

        /// <summary>
        /// Serializes a root
        /// </summary>
        /// <param name="root">Root</param>
        /// <returns>Serialized root string</returns>
        public abstract string RootToString(Root root);

        /// <summary>
        /// Serializes a Null
        /// </summary>
        /// <param name="n">Null</param>
        /// <returns>Serialized null string</returns>
        public virtual string NullToString(Null n) => "null";

        /// <summary>
        /// Serializes a method body
        /// </summary>
        /// <param name="body">Method body</param>
        /// <param name="indentDepth">Indentation depth to use in serialization</param>
        /// <returns>Serialized method body string</returns>
        protected virtual string MethodBodyToString(MethodBody body, int indentDepth)
        {
            var str = string.Empty;
            var indentBodyStr = CodeGenerator.Indent(indentDepth);
            if (body?.Statements == null) return str;

            return body.Statements.Where(statement => statement.Value != null)
                .Aggregate(str, (current1, statement) 
                    => statement.Value.Split(Environment.NewLine.ToCharArray())
                    .Where(line => !string.IsNullOrEmpty(line))
                    .Aggregate(current1, (current, line)
                        => current + $"{indentBodyStr}{line}{Environment.NewLine}"));
        }
    }
}