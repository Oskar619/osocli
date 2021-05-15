
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osodots.Util;
using System.Linq;

namespace osodots.Model
{
    public class OrderedMetadata : BaseMetadata
    {
        public string UpdateBefore { get; set; }
        public string UpdateAfter { get; set; }
        public bool OrderFirst { get; set; }
        public bool OrderLast { get; set; }

        public short OrderId { get; set; }

        public override void LoadFrom(ClassDeclarationSyntax classSyntax, AttributeListSyntax attributes = null)
        {
            base.LoadFrom(classSyntax, attributes);
            var updateBefore = attributes?.Attributes.FirstOrDefault(a => a.Name.ToString().Equals(DotsConstants.UpdateBefore));
            var updateAfter = attributes?.Attributes.FirstOrDefault(a => a.Name.ToString().Equals(DotsConstants.UpdateAfter));

            if (updateBefore != null)
            {
                var argSyntax = updateBefore.DescendantNodes().OfType<AttributeArgumentSyntax>().SingleOrDefault();
                UpdateBefore = argSyntax.DescendantTokens().SingleOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken)).ValueText;
            }

            if (updateAfter != null)
            {
                var argSyntax = updateAfter.DescendantNodes().OfType<AttributeArgumentSyntax>().SingleOrDefault();
                var identifierToken = argSyntax.DescendantTokens().SingleOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken));
                UpdateBefore = identifierToken.ValueText;
            }
        }
    }
}
