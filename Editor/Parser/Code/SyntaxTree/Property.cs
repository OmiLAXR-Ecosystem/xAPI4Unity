using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A property in the syntax tree
    /// </summary>
    public class Property : TypedNode<Property> {
        
        public Property() { }

        public Property(string name, string type, string comment = default) : base(name, type, comment) {
        }
    }
}