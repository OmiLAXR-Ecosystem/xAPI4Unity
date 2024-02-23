using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A constructor in the syntax tree
    /// </summary>
    public sealed class Constructor {
        private MethodBody body;
        private readonly List<Argument> baseArguments = new List<Argument>();
        private readonly List<Modifier> modifiers = new List<Modifier>();
        private readonly List<Parameter> parameters = new List<Parameter>();

        /// <summary>
        /// Type of the constructor
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Body of the constructor
        /// </summary>
        public ref readonly MethodBody Body => ref body;
        /// <summary>
        /// Arguments for the constructor's base constructor
        /// </summary>
        public IReadOnlyList<Argument> BaseArguments => baseArguments.AsReadOnly();
        /// <summary>
        /// Access modifiers of the constructor
        /// </summary>
        public IReadOnlyList<Modifier> Modifiers => modifiers.AsReadOnly();
        /// <summary>
        /// Parameters of the constructor
        /// </summary>
        public IReadOnlyList<Parameter> Parameters => parameters.AsReadOnly();

        public Constructor() { }

        public Constructor(string type, MethodBody body = default) {
            Type = type;
            this.body = body;
        }

        public Constructor WithType(string type) {
            this.Type = type;
            return this;
        }

        public Constructor WithBody(MethodBody body) {
            this.body = body;
            return this;
        }

        public Constructor AddBaseArgument(Argument arg) {
            baseArguments.Add(arg);
            return this;
        }

        public Constructor AddBaseArguments(params Argument[] args) {
            baseArguments.AddRange(args);
            return this;
        }

        public Constructor AddBaseArguments(IEnumerable<Argument> args) {
            baseArguments.AddRange(args);
            return this;
        }

        public Constructor AddModifier(Modifier mod) {
            modifiers.Add(mod);
            return this;
        }

        public Constructor AddModifiers(params Modifier[] mods) {
            modifiers.AddRange(mods);
            return this;
        }

        public Constructor AddModifiers(IEnumerable<Modifier> mods) {
            modifiers.AddRange(mods);
            return this;
        }

        public Constructor AddParameter(Parameter param) {
            parameters.Add(param);
            return this;
        }

        public Constructor AddParameters(params Parameter[] parameters) {
            this.parameters.AddRange(parameters);
            return this;
        }

        public Constructor AddParameters(IEnumerable<Parameter> parameters) {
            this.parameters.AddRange(parameters);
            return this;
        }
    }
}