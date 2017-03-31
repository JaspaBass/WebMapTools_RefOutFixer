using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ASTTransformationTest
{
    class DeclarationRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;
        public IDictionary<string, IDictionary<string, TypeInfo>> changingMethods;
        public List<string> MethodsToReplace;
        public int parcialReplaceCount = 0;

        public DeclarationRewriter(SemanticModel semanticModel)
        {
            this.SemanticModel = semanticModel;
            this.changingMethods = new Dictionary<string, IDictionary<string, TypeInfo>>();
        }
        
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            //Only work with methods in the list
            if (this.MethodsToReplace.Contains(node.Identifier.ToString()))
            {
                SyntaxNode newNode = null;
                //List of method parameter with no modifiers
                Dictionary<string, TypeInfo> parameters = new Dictionary<string, TypeInfo>();
                //Adding return value param
                parameters.Add("returnValue", SemanticModel.GetTypeInfo(node.ReturnType));
                //List of ParameterList
                ParameterListSyntax pls = SyntaxFactory.ParameterList().NormalizeWhitespace();
                //List of Parameter
                List<ParameterSyntax> ps = new System.Collections.Generic.List<ParameterSyntax>();
                bool needReplace = false;
                //Adding parameters without ref/out keyword
                foreach (ParameterSyntax parameter in node.ParameterList.Parameters)
                {
                    if (parameter.Modifiers.Count == 0)
                    {
                        ps.Add(parameter);

                        TypeInfo initializerInfo = SemanticModel.GetTypeInfo(parameter.Type);
                        parameters.Add("#" + parameter.Identifier.ToString(), initializerInfo);
                    }
                    foreach (SyntaxToken stl in parameter.Modifiers)
                    {
                        if (stl.IsKind(SyntaxKind.RefKeyword) || stl.IsKind(SyntaxKind.OutKeyword))
                        {
                            needReplace = true;
                            var updatedParameterNode = parameter.WithModifiers(new SyntaxTokenList());
                            ps.Add(updatedParameterNode);

                            TypeInfo initializerInfo = SemanticModel.GetTypeInfo(parameter.Type);
                            parameters.Add(parameter.Identifier.ToString(), initializerInfo);
                        }
                    }
                }
                //Filling ParameterList intances
                pls = needReplace ? pls.AddParameters(ps.ToArray()).NormalizeWhitespace() : pls;
                TypeInfo ti = new TypeInfo();
                string methodName = "";
                //Updating changinMethods Dictionary
                if (pls.Parameters.Count > 0 && (parameters.Count > 1 || parameters.Count == 1 && parameters.TryGetValue("returnValue", out ti) && ti.Type.ToString() != "void"))
                {
                    parcialReplaceCount++;
                    int numMethod = 0;
                    methodName = node.Identifier.ToString();
                    while(this.changingMethods.ContainsKey(methodName))
                    {
                        numMethod++;
                        methodName = node.Identifier.ToString() + numMethod;
                    }
                    this.changingMethods.Add(methodName, parameters);
                }
                //Feeding newNode
                if (pls.Parameters.Count > 0 && pls.Parameters.Count > 0)
                    newNode = parameters.Count > 0 ? node.ReplaceNode(node.ParameterList, pls.NormalizeWhitespace().WithLeadingTrivia(node.Body.GetLeadingTrivia()).WithTrailingTrivia(node.Body.GetTrailingTrivia())) : node;
                else
                    newNode = node;
                //Replacing return type
                var predefinedType = newNode.DescendantNodes().OfType<PredefinedTypeSyntax>().FirstOrDefault();
                //Replacing predefied type
                if (pls.Parameters.Count > 0 && (parameters.Count > 1 || parameters.Count == 1 && parameters.TryGetValue("returnValue", out ti) && ti.Type.ToString() != "void" && predefinedType != null))
                {
                    newNode = newNode.ReplaceNode(predefinedType, SyntaxFactory.ParseTypeName(" " + methodName + "Struct").NormalizeWhitespace()).NormalizeWhitespace();
                }
                //Replace body and returning the new node
                newNode = newNode.ReplaceNode(((MethodDeclarationSyntax)newNode).Body, node.Body);
                return newNode.WithLeadingTrivia(node.Body.GetLeadingTrivia()).WithTrailingTrivia(node.Body.GetTrailingTrivia());
            }
            else
                return node;
        }
    }
}
