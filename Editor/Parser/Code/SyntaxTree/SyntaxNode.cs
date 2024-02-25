#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree
{
    internal abstract class SyntaxNode<T> where T : SyntaxNode<T>
    {
        private readonly List<Modifier> modifiers = new List<Modifier>();

        /// <summary>
        /// Name of the property
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Access modifiers of the property
        /// </summary>
        public IReadOnlyList<Modifier> Modifiers => modifiers.AsReadOnly();
  
        /// <summary>
        /// Comment of the property
        /// </summary>
        public string Comment { get; private set; }

        public SyntaxNode() { }

        public SyntaxNode(string name, string comment = default) {
            Name = name;
            Comment = comment;
        }

        public T WithName(string name) {
            Name = name;
            return (T)this;
        }

        public T AddModifier(Modifier mod) {
            modifiers.Add(mod);
            return (T)this;
        }

        public T AddModifiers(params Modifier[] mods) {
            modifiers.AddRange(mods);
            return (T)this;
        }
        public T AddModifiers(IEnumerable<Modifier> mods) {
            modifiers.AddRange(mods);
            return (T)this;
        }
        public T WithComment(string comment) {
            Comment = comment.Replace(">", "&gt;").Replace("<", "&lt;");
            return (T)this;
        }
    }
}
#endif