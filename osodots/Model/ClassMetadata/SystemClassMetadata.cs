
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osodots.Util;
using System.Collections.Generic;
using System.Linq;

namespace osodots.Model
{
    public class SystemClassMetadata : OrderedMetadata
    {
        public string Group { get; set; }
        public string[] ReadOnlyComponents { get; set; }
        public string[] ReadWriteComponents { get; set; }

        public override void LoadFrom(ClassDeclarationSyntax classSyntax, AttributeListSyntax attributes = null)
        {
            base.LoadFrom(classSyntax, attributes);
            var updateInGroup = attributes?.Attributes.FirstOrDefault(a => a.Name.ToString().Equals(DotsConstants.UpdateInGroup));
            if (updateInGroup != null)
            {
                var argSyntax = updateInGroup.DescendantNodes().OfType<AttributeArgumentSyntax>().SingleOrDefault();
                Group = argSyntax.DescendantTokens().SingleOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken)).ValueText;
            }

            var invocations = classSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            var entitiesForeachStatements = classSyntax.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression.ToString().StartsWith(DotsConstants.EntitiesForeach));

            var readOnly = new List<string>();
            var readWrite = new List<string>();

            foreach (var entityForeach in entitiesForeachStatements)
            {
                var parameters = entityForeach.DescendantNodes().OfType<ParameterListSyntax>().SingleOrDefault();
                if (parameters != null)
                {
                    foreach (var parameter in parameters.Parameters)
                    {
                        var paramTokens = parameter.DescendantTokens().ToArray();
                        if (paramTokens.Length == 3)
                        {
                            var inRefKw = paramTokens[0];
                            var component = paramTokens[1];
                            var componentName = component.ValueText;
                            if (inRefKw.IsKind(SyntaxKind.RefKeyword))
                            {
                                if (!readWrite.Contains(componentName))
                                {
                                    readWrite.Add(componentName);
                                }
                            }
                            else if (inRefKw.IsKind(SyntaxKind.InKeyword))
                            {
                                if (!readOnly.Contains(componentName))
                                {
                                    readOnly.Add(componentName);
                                }
                            }
                        }
                    }

                    foreach(var readWriteComp in readWrite)
                    {
                        if (readOnly.Contains(readWriteComp))
                        {
                            readOnly.Remove(readWriteComp);
                        }
                    }
                }
            }

            ReadOnlyComponents = readOnly.ToArray();
            ReadWriteComponents = readWrite.ToArray();
        }
    }
}
