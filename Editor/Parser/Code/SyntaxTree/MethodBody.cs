using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A method body in the syntax tree
    /// </summary>
    public sealed class MethodBody {
        private List<Statement> statements = new List<Statement>();

        /// <summary>
        /// Statements in the method
        /// </summary>
        public IReadOnlyList<Statement> Statements => statements.AsReadOnly();

        public MethodBody() { }

        public MethodBody AddStatement(Statement statement) {
            statements.Add(statement);
            return this;
        }

        public MethodBody AddStatements(params Statement[] statements) {
            this.statements.AddRange(statements);
            return this;
        }

        public MethodBody AddStatements(IEnumerable<Statement> statements) {
            this.statements.AddRange(statements);
            return this;
        }
    }
}