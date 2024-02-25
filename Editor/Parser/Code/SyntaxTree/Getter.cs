#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A getter in the syntax tree
    /// </summary>
    internal sealed class Getter {
        private MethodBody body;

        /// <summary>
        /// Method body of the getter
        /// </summary>
        public ref readonly MethodBody Body => ref body;

        public Getter() { }

        public Getter(MethodBody body) {
            this.body = body;
        }

        public Getter WithMethodBody(MethodBody body) {
            this.body = body;
            return this;
        }
    }
}
#endif