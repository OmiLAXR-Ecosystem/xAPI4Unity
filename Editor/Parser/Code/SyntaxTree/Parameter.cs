namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A parameter in the syntax tree
    /// </summary>
    public sealed class Parameter {
        /// <summary>
        /// name of the parameter
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Type of the parameter
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Default value
        /// </summary>
        public object DefaultValue { get; private set; }

        public Parameter() { }

        public Parameter(string name, string type = default, object defaultValue = default) {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        public Parameter WithName(string name) { 
            Name = name;
            return this;
        }

        public Parameter WithType(string type) {
            Type = type;
            return this;
        }

        public Parameter WithDefVal(object defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }
    }
}