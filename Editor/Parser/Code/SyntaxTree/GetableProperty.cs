#if UNITY_EDITOR
namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A property in the syntax tree with a getter
    /// </summary>
    internal sealed class GetableProperty : TypedNode<GetableProperty> {
        private Getter getter;
        
        /// <summary>
        /// Getter of the property
        /// </summary>
        public ref readonly Getter Getter => ref getter;

        public GetableProperty() : base() { }

        public GetableProperty(string name, string type, Getter getter = default, string comment = default) : base(name, type, comment) {
            this.getter = getter;
        }

        public GetableProperty WithGetter(Getter g) {
            getter = g;
            return this;
        }
    }
}
#endif