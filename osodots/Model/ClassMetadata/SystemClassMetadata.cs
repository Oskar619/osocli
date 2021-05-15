
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
        public short GroupOrderId { get; set; }
        public string[] ReadOnlyComponents { get; set; }
        public string[] ReadWriteComponents { get; set; }
        public override void LoadFrom(ClassDeclarationSyntax classSyntax, AttributeListSyntax attributes = null)
        {
            base.LoadFrom(classSyntax, attributes);
            var updateInGroup = attributes?.Attributes.FirstOrDefault(a => a.Name.ToString().Equals(DotsConstants.UpdateInGroup));
            if (updateInGroup != null)
            {
                var argSyntaxList = updateInGroup.DescendantNodes().OfType<AttributeArgumentSyntax>();
                foreach(var argSyntax in argSyntaxList)
                {
                    var descendants = argSyntax.DescendantTokens().Where(d => !d.IsKind(SyntaxKind.OpenParenToken)
                    && !d.IsKind(SyntaxKind.CloseParenToken)
                    && !d.IsKind(SyntaxKind.TypeOfKeyword)
                    && !d.IsKind(SyntaxKind.EqualsToken)).ToArray();
                    if(descendants.Length > 2 || descendants.Length < 1)
                    {
                        continue;
                    }

                    var typeId = descendants[0].ValueText;
                    if(typeId == DotsConstants.OrderLast)
                    {
                        if(descendants.Length < 2)
                        {
                            continue;
                        }
                        OrderLast = descendants[1].IsKind(SyntaxKind.TrueKeyword);
                    }
                    else if(typeId == DotsConstants.OrderFirst)
                    {
                        if (descendants.Length < 2)
                        {
                            continue;
                        }
                        OrderFirst = descendants[1].IsKind(SyntaxKind.TrueKeyword);
                    }
                    else
                    {
                        // If the syntax id is neither Order first or Order last, then it means it's a reference to the system for the group's id.
                        Group = typeId;
                    }
                }
            }

            var invocations = classSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            var entitiesForeachStatements = classSyntax.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression.ToString().StartsWith(DotsConstants.EntitiesForeach));

            var readOnly = new List<string>();
            var readWrite = new List<string>();

            foreach (var entityForeach in entitiesForeachStatements)
            {
                // TODO: we could also explore more about what's happening inside the 
                var parameterList = entityForeach.DescendantNodes().OfType<ParameterListSyntax>().Where(p => p.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression)).ToList();
                foreach(var parameters in parameterList)
                {
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

                        foreach (var readWriteComp in readWrite)
                        {
                            if (readOnly.Contains(readWriteComp))
                            {
                                readOnly.Remove(readWriteComp);
                            }
                        }
                    }
                }

                // var parameters = entityForeach.DescendantNodes().OfType<ParameterListSyntax>().SingleOrDefault();
            }

            ReadOnlyComponents = readOnly.ToArray();
            ReadWriteComponents = readWrite.ToArray();
        }
    }
}
