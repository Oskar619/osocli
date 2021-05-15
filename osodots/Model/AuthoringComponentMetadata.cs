using Microsoft.CodeAnalysis.CSharp.Syntax;
using osodots.Util;
using System.Linq;

namespace osodots.Model
{
    public class AuthoringComponentMetadata : BaseMetadata
    {
        public string[] Archetypes { get; set; }
        public string[] Components { get; set; }

        public override void LoadFrom(ClassDeclarationSyntax classSyntax, AttributeListSyntax attributes = null)
        {
            base.LoadFrom(classSyntax, attributes);
            // TODO: Search all components and archetypes used in the Convert method.
            var updateMethodNode = classSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>().SingleOrDefault(m => m.Identifier.ToString().Equals(DotsConstants.Convert));
        }
    }
}
