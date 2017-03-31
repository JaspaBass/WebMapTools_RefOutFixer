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
    class ReturnStatementRewritter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;
        public IDictionary<string, IDictionary<string, TypeInfo>> changingMethods;

        public ReturnStatementRewritter(SemanticModel semanticModel)
        {
            this.SemanticModel = semanticModel;
            this.changingMethods = new Dictionary<string, IDictionary<string, TypeInfo>>();
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            //Creating the working node
            SyntaxNode newNode = node;
            //Retrieving return statements syntax
            var returnStat = node.DescendantNodes()
                                     .OfType<ReturnStatementSyntax>();
            IDictionary<string, TypeInfo> params2 = new Dictionary<string, TypeInfo>();

            //Trying to get the values from the changing methods dictionary for the current method
            IDictionary<string, TypeInfo> parametersDicc = new Dictionary<string, TypeInfo>();
            bool ready = false;
            int numParam = 0;
            string nameMethod = node.Identifier.ToFullString();
            while (!ready)
            {
                if (!this.changingMethods.TryGetValue(nameMethod, out parametersDicc))
                    break;
                if (parametersDicc.Count - 1 == node.ParameterList.Parameters.Count)
                {
                    bool internalReady = true;
                    for (int i = 0; i < node.ParameterList.Parameters.Count; i++)
                    {
                        if (!parametersDicc.Values.ElementAt(i + 1).Type.ToString().Substring(parametersDicc.Values.ElementAt(i + 1).Type.ToString().LastIndexOf(".") + 1).Equals(node.ParameterList.Parameters.ElementAt(i).Type.ToString()))
                        {
                            internalReady = false;
                            break;
                        }
                    }
                    if (internalReady) break;

                }
                numParam++;
                nameMethod = node.Identifier.ToString() + numParam;
            }
            if (this.changingMethods.TryGetValue(nameMethod, out params2))
            {
                if (returnStat.Count<object>() > 0)
                {
                    //Retrieving current return statements
                    var currentReturnStat = newNode.DescendantNodes()
                                         .OfType<ReturnStatementSyntax>();
                    bool onWork = false;
                    int indexCount = 0;
                    //Iterating in current return statements of the current method/node
                    while (currentReturnStat.Count<object>() > indexCount)
                    {
                        //If return statement has arguments
                        if (currentReturnStat.ElementAt(indexCount).ChildNodes().Count<SyntaxNode>() > 0)
                        {
                            if (!onWork)
                            {
                                //Building the new return statement
                                string returnExpression = @"new " + nameMethod + "Struct" + @"() { ";
                                foreach (var param in params2)
                                {
                                    if (param.Key.Equals("returnValue"))
                                    {
                                        returnExpression = returnExpression + " returnValue= " + currentReturnStat.ElementAt(indexCount).ChildNodes().ElementAt(0).ToString() + ", ";
                                    }
                                    else if(!param.Key.StartsWith("#"))
                                        returnExpression = returnExpression + param.Key + " = " + param.Key + ", ";
                                }

                                returnExpression = returnExpression + @" } ";

                                //Create a expression with the new return statement
                                SyntaxNode sn = SyntaxFactory.ParseExpression(returnExpression);

                                //Replace the current return statement
                                newNode = newNode.ReplaceNode(currentReturnStat.ElementAt(indexCount).ChildNodes().ElementAt(0), sn.WithLeadingTrivia(currentReturnStat.ElementAt(indexCount).ChildNodes().ElementAt(0).GetLeadingTrivia()).WithTrailingTrivia(currentReturnStat.ElementAt(indexCount).ChildNodes().ElementAt(0).GetTrailingTrivia()));
                                //Update current return statements array
                                currentReturnStat = newNode.DescendantNodes()
                                                     .OfType<ReturnStatementSyntax>();
                            }
                            indexCount++;
                        }
                        else
                        {
                            //If return statement does not has arguments (return;)
                            //Building the new return statement
                            string returnExpression = @"new " + nameMethod + "Struct" + @"() { ";
                            foreach (var param in params2)
                            {
                                if (!param.Value.Type.ToString().Equals("void") && !param.Key.StartsWith("#"))
                                    returnExpression = returnExpression + param.Key + " = " + param.Key + ", ";
                            }

                            returnExpression = returnExpression + @" } ";
                            
                            if (indexCount == 0)
                            {
                                //Adding returning statement
                                var block = newNode.DescendantNodes().OfType<BlockSyntax>().First();
                                
                                //Adding return statement at the end of the method
                                var newStats = block.AddStatements(SyntaxFactory.ReturnStatement(ParseExpression(returnExpression).WithoutTrailingTrivia())).NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));

                                //Replacing the node
                                newNode = newNode.ReplaceNode(block, newStats);

                                //Updting newNode
                                currentReturnStat = newNode.DescendantNodes()
                                                     .OfType<ReturnStatementSyntax>();

                                onWork = true;
                            }
                            //Replacing the occurrence of return;
                            newNode = newNode.ReplaceNode(currentReturnStat.ElementAt(indexCount), SyntaxFactory.ReturnStatement(ParseExpression(returnExpression)).NormalizeWhitespace().WithLeadingTrivia(currentReturnStat.ElementAt(indexCount).GetLeadingTrivia()).WithTrailingTrivia(currentReturnStat.ElementAt(indexCount).GetTrailingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));
                            //Updting newNode
                            currentReturnStat = newNode.DescendantNodes()
                                                 .OfType<ReturnStatementSyntax>();
                            indexCount++;
                        }
                    }
                }
                else
                {
                    //If the method does not has returning statements
                    //Building new return expression
                    string returnExpression = @"new " + nameMethod + "Struct" + @"() { ";
                    foreach (var param in params2)
                    {
                        if(!param.Value.Type.ToString().Equals("void") && !param.Key.StartsWith("#"))
                            returnExpression = returnExpression + param.Key + " = " + param.Key + ", ";
                    }
                    returnExpression = returnExpression + @" } ";

                    //Retrieving first block node
                    var block = newNode.DescendantNodes().OfType<BlockSyntax>().First();
                    //Creating new statements
                    var newStats = block.AddStatements(SyntaxFactory.ReturnStatement(ParseExpression(node.GetLeadingTrivia() + returnExpression))).NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));

                    //Replacing the block node
                    newNode = newNode.ReplaceNode(block, newStats.WithTriviaFrom(node));
                }
            }

            //Returning the worked node
            return newNode;
        }
    }
}
