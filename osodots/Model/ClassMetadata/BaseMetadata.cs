using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace osodots.Model
{
    public class BaseMetadata
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public TextSpan Span { get; set; }

        public virtual void LoadFrom(ClassDeclarationSyntax classSyntax, AttributeListSyntax attributes = null)
        {
            Name = classSyntax.Identifier.ToString();
            Span = classSyntax.Span;
        }
    }
}
