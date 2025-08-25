namespace xAPI4Unity.Editor.Types
{
    public class ResolvedNullable : ResolvedType
    {
        public override bool IsNullable => true;
        public ResolvedNullable(ResolvedType type) : base(type.Type, type.Definition)
        {
            
        }

        public override string GetTypeSyntax() => base.GetTypeSyntax() + "?";
    }
}