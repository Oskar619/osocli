
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osodots.Util;
using System.Linq;

namespace osodots.Model
{
    public class ComponentMetaData : BaseMetadata
    {
        public bool IsBuffer { get; set; }

        public override void LoadFrom(ClassDeclarationSyntax classSyntax, AttributeListSyntax attributes = null)
        {
            base.LoadFrom(classSyntax, attributes);
            var types = classSyntax.BaseList?.Types;
            if (types != null)
            {
                IsBuffer = types.Value.Any(t => t.Type.ToString().Equals(DotsConstants.IBufferElementData));
            }
        }
    }
}
