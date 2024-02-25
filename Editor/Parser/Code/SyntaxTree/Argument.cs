#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// An argument in the syntax tree
    /// </summary>
    internal class Argument {
        private object value;

        /// <summary>
        /// Name of the argument
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Value of the argument
        /// </summary>
        public ref readonly object Value => ref value;

        public Argument() { }

        public Argument(object value, string name = default) {
            this.value = value;
            Name = name;
        }

        public Argument WithName(string name) {
            Name = name;
            return this;
        }

        public Argument WithValue(object value) {
            this.value = value;
            return this;
        }
    }
}
#endif