#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A statement in a method
    /// </summary>
    internal class Statement {
        private string value;

        /// <summary>
        /// Statement as a string (i.e. "int i = 0;")
        /// </summary>
        public string Value => value;

        public Statement() { }

        public Statement(string value) {
            this.value = value;
        }

        public Statement WithValue(string value) {
            this.value = value;
            return this;
        }
    }
}
#endif