namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// An import of the syntax tree
    /// </summary>
    public sealed class Import {
        private string target;
        private string source;

        /// <summary>
        /// Target to import (i.e. a class)
        /// </summary>
        public string Target => target;
        /// <summary>
        /// Source to import from (i.e. a package)
        /// </summary>
        public string Source => source;

        public Import() { }

        public Import(string target, string source = default) {
            this.target = target;
            this.source = source;
        }

        public Import WithTarget(string target) {
            this.target = target;
            return this;
        }

        public Import WithSource(string source) {
            this.source = source;
            return this;
        }
    }
}