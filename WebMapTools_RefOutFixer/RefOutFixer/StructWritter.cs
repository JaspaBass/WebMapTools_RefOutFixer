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
    class StructWritter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;
        public IDictionary<string, IDictionary<string, TypeInfo>> changingMethods;

        public StructWritter(SemanticModel semanticModel)
        {
            this.SemanticModel = semanticModel;
            this.changingMethods = new Dictionary<string, IDictionary<string, TypeInfo>>();
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ClassDeclarationSyntax newNode = node;
            foreach (var changingMethod in this.changingMethods)
            {
                //Creating struct declaration syntax
                StructDeclarationSyntax newStruct = SyntaxFactory.StructDeclaration(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.TokenList(),
                        SyntaxFactory.Identifier(" " + changingMethod.Key + "Struct"), default(TypeParameterListSyntax), default(BaseListSyntax), default(SyntaxList<TypeParameterConstraintClauseSyntax>), default(SyntaxList<MemberDeclarationSyntax>))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword)).WithLeadingTrivia(SyntaxFactory.ElasticTab)
                .AddMembers()
                .NormalizeWhitespace().WithLeadingTrivia(node.GetLeadingTrivia()).WithLeadingTrivia(SyntaxFactory.ElasticTab)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.Tab).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Tab).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")))
                .WithTrailingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));
                foreach (var field in changingMethod.Value)
                {
                    //Declaring Field declaration syntax to add the variables to the new struct
                    FieldDeclarationSyntax fds = null;
                    if (!field.Value.Type.ToString().Equals("void") && !field.Key.StartsWith("#"))
                    {
                        //Creating the new variable/field of the struct
                        VariableDeclarationSyntax vds = SyntaxFactory.VariableDeclaration(
                            type: SyntaxFactory.IdentifierName(field.Value.Type.ToString()),
                            variables: SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(
                                new List<VariableDeclaratorSyntax> { VariableDeclarator(
                            identifier: Identifier(field.Key)) }))
                        .NormalizeWhitespace();
                        fds = SyntaxFactory.FieldDeclaration(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.TokenList(), vds).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword).NormalizeWhitespace()).NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));
                    }

                    //Adding parameter to the newStruct
                    newStruct = fds != null ? newStruct.AddMembers(fds.WithLeadingTrivia(SyntaxFactory.ElasticTab)) : newStruct;
                }
                //Adding new struct at the end of the class declaration
                newNode = newStruct != null ? newNode.AddMembers(newStruct) : null;

            }
            //Returning node
            if (newNode == null)
                return node;
            else
                return newNode.WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));
        }
    }
}
