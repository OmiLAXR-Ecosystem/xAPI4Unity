#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree
{
    internal abstract class TypedNode<T> : SyntaxNode<T> where T : SyntaxNode<T>
    {
        private readonly List<Argument> arguments = new List<Argument>();
        
        /// <summary>
        /// Type of the property
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Arguments to initiate the property with
        /// </summary>
        public IReadOnlyList<Argument> Arguments => arguments.AsReadOnly();
        
        public TypedNode() {}

        public TypedNode(string name, string type, string comment = default) : base(name, comment)
        {
            Type = type;
        }
        
        public T WithType(string type) {
            Type = type;
            return (T)(object)this;
        }
        
        public T AddArgument(Argument arg) {
            arguments.Add(arg);
            return (T)(object)this;
        }

        public T AddArguments(params Argument[] args) {
            arguments.AddRange(args);
            return (T)(object)this;
        }

        public T AddArguments(IEnumerable<Argument> args) {
            arguments.AddRange(args);
            return (T)(object)this;
        }
    }
}
#endif