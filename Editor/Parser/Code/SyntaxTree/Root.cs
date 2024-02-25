#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// Root of the syntax tree
    /// </summary>
    internal class Root {
        private Class cls;
        private readonly List<Import> imports = new List<Import>();

        /// <summary>
        /// Class in the syntax tree
        /// </summary>
        public ref readonly Class Class => ref cls;
        /// <summary>
        /// Namespace in the syntax tree
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Imports in the syntax tree
        /// </summary>
        public IReadOnlyList<Import> Imports => imports.AsReadOnly();

        public Root() { }

        public Root(Class cls, string @namespace = default) {
            this.cls = cls;
            this.Namespace = @namespace;
        }

        public Root WithClass(Class cls) {
            this.cls = cls;
            return this;
        }

        public Root WithNamespace(string @namespace) {
            this.Namespace = @namespace;
            return this;
        }

        public Root AddImport(Import import) {
            imports.Add(import);
            return this;
        }

        public Root AddImports(params Import[] imports) {
            this.imports.AddRange(imports);
            return this;
        }

        public Root AddImports(IEnumerable<Import> imports) {
            this.imports.AddRange(imports);
            return this;
        }
    }
}
#endif