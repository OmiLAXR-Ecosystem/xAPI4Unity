#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A method in the syntax tree
    /// </summary>
    internal sealed class Method : TypedNode<Method> {
        private MethodBody body;
        private List<Argument> baseArguments = new List<Argument>();
        private List<Parameter> parameters = new List<Parameter>();
        
        /// <summary>
        /// Body of the method
        /// </summary>
        public ref readonly MethodBody Body => ref body;
        /// <summary>
        /// Arguments for the method's base method
        /// </summary>
        public IReadOnlyList<Argument> BaseArguments => baseArguments.AsReadOnly();
        /// <summary>
        /// Parameters of the method
        /// </summary>
        public IReadOnlyList<Parameter> Parameters => parameters.AsReadOnly();

        public Method() { }

        public Method(string name, string type = default, MethodBody body = default, string comment = default) : base(name, type, comment) {
            this.body = body;
        }

        public Method WithBody(MethodBody b) {
            body = b;
            return this;
        }

        public Method AddBaseArgument(Argument arg) {
            baseArguments.Add(arg);
            return this;
        }

        public Method AddBaseArguments(params Argument[] args) {
            baseArguments.AddRange(args);
            return this;
        }

        public Method AddBaseArguments(IEnumerable<Argument> args) {
            baseArguments.AddRange(args);
            return this;
        }

        public Method AddParameter(Parameter param) {
            parameters.Add(param);
            return this;
        }

        public Method AddParameters(params Parameter[] parameters) {
            this.parameters.AddRange(parameters);
            return this;
        }

        public Method AddParameters(IEnumerable<Parameter> parameters) {
            this.parameters.AddRange(parameters);
            return this;
        }
    }
}
#endif