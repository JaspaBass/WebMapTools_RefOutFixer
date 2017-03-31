using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using Microsoft.CodeAnalysis.MSBuild;

namespace ASTTransformationTest
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            //Initializing Builed Workspace and solution.
            var msworkspace = MSBuildWorkspace.Create();

            var solution = msworkspace.OpenSolutionAsync(textBox1.Text).Result;
            //Variable in which we are going to store the solution with the new transformations.
            var newSolution = solution;
            //dictionary to store old nodes and transformed nodes
            Dictionary<SyntaxTree, Dictionary<SyntaxNode, SyntaxNode>> nodeToChanges = new Dictionary<SyntaxTree, Dictionary<SyntaxNode, SyntaxNode>>();

            int totalReplcaes = 0;

            int totalFiles = 0;

            int indexProj = 0;
            //Iterating in the solution projects
            foreach (var project in solution.Projects)
            {
                //var solutionChnages = solution.GetChanges(solution);
                if (project.AssemblyName == textBox2.Text)
                {
                    progressBar1.Step = 1;
                    progressBar1.Maximum = project.Documents.Count();
                    int indexDoc = 0;
                    //Iterating in the project documents
                    foreach (var document in project.Documents)
                    {
                        List<string> methodsToReplace = new List<string>();
                        //Reading a file with the methods that need to be transformed
                        foreach (var line in File.ReadLines(textBox3.Text))
                        {
                            if (line.Split('\t')[0].Equals(document.FilePath.Substring(document.FilePath.LastIndexOf("\\") + 1)))
                                methodsToReplace.Add(line.Split('\t')[2]);
                        }

                        //Retrieving the semantic model in order to use it to obtain symbols, references, types and another compile time matter.
                        var model = newSolution.Projects.ElementAt(indexProj).Documents.ElementAt(indexDoc).GetSemanticModelAsync().Result;

                        //Retrieving the root syntax node ot the actual document/class
                        var documentNode = newSolution.Projects.ElementAt(indexProj).Documents.ElementAt(indexDoc).GetSyntaxRootAsync().Result;

                        //Index to iterate in the methods declaration nodes
                        int indexMDNodes = 0;

                        //Clear array
                        nodeToChanges.Clear();


                        //Retrieving current syntax tree.
                        SyntaxTree sourceTree = newSolution.Projects.ElementAt(indexProj).Documents.ElementAt(indexDoc).GetSyntaxTreeAsync().Result;

                        //Updating the model
                        model = newSolution.Projects.ElementAt(indexProj).Documents.ElementAt(indexDoc).GetSemanticModelAsync().Result;

                        //Part II - Replacing method declaration
                        //Creating new DeclarationRewriter instances
                        var rewriter = new DeclarationRewriter(model);

                        rewriter.MethodsToReplace = methodsToReplace;

                        //Visit nodes in order to execute override methods of DeclarationRewriter class
                        var newSource = rewriter.Visit(sourceTree.GetRoot());

                        totalReplcaes += rewriter.parcialReplaceCount;
                        if (rewriter.parcialReplaceCount > 0)
                            totalFiles++;

                        //Part III - Adding the new Structs
                        //Creating new StructWritter instances
                        var structW = new StructWritter(model);

                        structW.changingMethods = rewriter.changingMethods;

                        //Visit nodes in order to execute override methods of StructWritter class
                        var newSource2 = structW.Visit(newSource.SyntaxTree.GetRoot());

                        //Part IV - Replacing returning statements
                        //Creating new ReturnStatementRewritter instances
                        var returnStatW = new ReturnStatementRewritter(model);

                        returnStatW.changingMethods = structW.changingMethods;

                        //Visit nodes in order to execute override methods of ReturnStatementRewritter class
                        var newSource3 = returnStatW.Visit(newSource2.SyntaxTree.GetRoot());

                        //Part V - Replacing calls to methods to be transformed from the current classes
                        //Retrieving Invocation Expression syntax nodes
                        var nodes = newSource3.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(n => n.Expression.ChildNodes().Count<object>() > 0 && returnStatW.changingMethods.ContainsKey(n.Expression.ChildNodes().ElementAt(n.Expression.ChildNodes().Count<object>() - 1).ToString())
                                || n.Expression.ChildNodes().Count<object>() == 0 && returnStatW.changingMethods.ContainsKey(n.Expression.ToString()));

                        var newSource4 = newSource3;

                        indexMDNodes = 0;


                        foreach (InvocationExpressionSyntax node in nodes)
                        {

                            InvocationExpressionSyntax newInvoNode = node;

                            if (node.Expression.ChildNodes().Count<object>() > 1)
                            {
                            }
                            else
                            {
                                //List to stored the Statement Syntax nodes of the new transformation
                                List<StatementSyntax> ssList = new System.Collections.Generic.List<StatementSyntax>();
                                //Retrieving arguments from invocation expression
                                var invoArgs = nodes.ElementAt(indexMDNodes).ArgumentList.Arguments;
                                int index = 0;
                                //Iterating the arguments
                                foreach (var argum in invoArgs)
                                {
                                    //Removing ref keyword
                                    var newArgum = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(argum.ToString().Replace("ref ", "").Replace("out ", "")));
                                    //Replacing node
                                    newInvoNode = newInvoNode.ReplaceNode(newInvoNode.ArgumentList.Arguments[index], newArgum).WithLeadingTrivia(newInvoNode.ArgumentList.Arguments[index].GetLeadingTrivia()).WithTrailingTrivia(newInvoNode.ArgumentList.Arguments[index].GetTrailingTrivia());
                                    //Update the invocation arguments
                                    invoArgs = newInvoNode.ArgumentList.Arguments;
                                    index++;
                                }
                                //Add invoke stament node
                                ssList.Add(SyntaxFactory.ParseStatement("var returningMetodValue" + indexMDNodes.ToString() + " = " + newInvoNode.ToFullString() + ";").NormalizeWhitespace().WithLeadingTrivia(newInvoNode.GetLeadingTrivia()).WithTrailingTrivia(newInvoNode.GetTrailingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));

                                //Returning value assigment node
                                SyntaxNode rvNode = SyntaxFactory.ParseExpression("returningMetodValue" + indexMDNodes + "." + "returnValue").SyntaxTree.GetRoot().NormalizeWhitespace().WithLeadingTrivia(newInvoNode.GetLeadingTrivia()).WithTrailingTrivia(newInvoNode.GetTrailingTrivia());

                                //Retrieving full line syntax node
                                SyntaxNode fullLineSN = nodes.ElementAt(indexMDNodes);
                                //Flag for If/While/For statements
                                bool isIfStat = false;
                                bool isMultiStat = false;
                                bool isSwitchStat = false;
                                while (!fullLineSN.GetLastToken().IsKind(SyntaxKind.SemicolonToken))
                                {
                                    if (fullLineSN.IsKind(SyntaxKind.IfStatement) || fullLineSN.IsKind(SyntaxKind.WhileStatement) || fullLineSN.IsKind(SyntaxKind.ForStatement))
                                    {
                                        isIfStat = true;
                                        break;
                                    }
                                    else if(fullLineSN.IsKind(SyntaxKind.SwitchStatement))
                                    {
                                        isSwitchStat = true;
                                        break;
                                    }
                                    fullLineSN = fullLineSN.Parent;
                                }

                                //Dictionary ref parameters
                                IDictionary<string, TypeInfo> parametersDicc = new Dictionary<string, TypeInfo>();
                                //Trying to get dictionary for current invocation identifier
                                bool ready = false;
                                int numParam = 0;
                                string nameMethod = nodes.ElementAt(indexMDNodes).Expression.ToString();
                                while (!ready)
                                {
                                    if (!returnStatW.changingMethods.TryGetValue(nameMethod, out parametersDicc))
                                        break;
                                    if(parametersDicc.Count - 1 == invoArgs.Count)
                                        break;
                                    numParam++;
                                    nameMethod = nodes.ElementAt(indexMDNodes).Expression.ToString() + numParam;
                                }
                                //Check for returning types different to "void"
                                if (parametersDicc != null && parametersDicc.Keys.ElementAt(0).Equals("returnValue") && !parametersDicc.Values.ElementAt(0).Type.ToString().Equals("void"))
                                {
                                    if (isIfStat)
                                    {
                                        //Creating and add if expresion
                                        string expIf = fullLineSN.ReplaceNode(nodes.ElementAt(indexMDNodes), rvNode).ToString();
                                        ssList.Add(SyntaxFactory.ParseStatement(expIf.Substring(0, expIf.IndexOf('{') + 1)).NormalizeWhitespace().WithLeadingTrivia(fullLineSN.GetLeadingTrivia()).WithTrailingTrivia(fullLineSN.GetTrailingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));
                                    }
                                    else
                                    {
                                        //add other expression
                                        if(fullLineSN.DescendantNodes().ElementAt(0).IsKind(SyntaxKind.SimpleAssignmentExpression) || fullLineSN.DescendantNodes().ElementAt(0).IsKind(SyntaxKind.VariableDeclaration))
                                            ssList.Add(SyntaxFactory.ParseStatement(fullLineSN.ReplaceNode(nodes.ElementAt(indexMDNodes), rvNode).NormalizeWhitespace().WithLeadingTrivia(fullLineSN.GetLeadingTrivia()).WithTrailingTrivia(fullLineSN.GetTrailingTrivia()).ToString()));
                                    }
                                }
                                //Updating invoke arguments
                                invoArgs = nodes.ElementAt(indexMDNodes).ArgumentList.Arguments;
                                index = 1;
                                //Iterating in invoke arguments in order to add the new expressions
                                foreach (var argum in invoArgs)
                                {
                                    if (argum.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) || argum.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                                    {
                                        //Building the expression to add
                                        var newArgum = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(argum.ToString().Replace("ref ", "").Replace("out ", "")));
                                        string finalExpression = newArgum.ToFullString() + " = " + "returningMetodValue" + indexMDNodes + "." + parametersDicc.Keys.ElementAt(index) + ";";
                                        ssList.Add(SyntaxFactory.ParseStatement(finalExpression).WithLeadingTrivia(fullLineSN.GetLeadingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));
                                    }
                                    index++;
                                }

                                //If is if statement, add the rest of the if block
                                if (isIfStat)
                                    ssList.Add(((BlockSyntax)fullLineSN.ChildNodes().ElementAt(1)).WithOpenBraceToken(SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken)));

                                if (isSwitchStat)
                                {
                                    ssList.Add(SyntaxFactory.ParseStatement(fullLineSN.ReplaceNode(nodes.ElementAt(indexMDNodes), rvNode).NormalizeWhitespace().WithLeadingTrivia(nodes.ElementAt(indexMDNodes).GetLeadingTrivia()).WithTrailingTrivia(nodes.ElementAt(indexMDNodes).GetTrailingTrivia()).ToString()));
                                }

                                //Create a block with the statements of the new node
                                var block = SyntaxFactory.Block(ssList);

                                block = block.WithOpenBraceToken(SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken))
                                        .WithCloseBraceToken(SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));

                                //Replacing node
                                if (isIfStat)
                                {
                                    newSource4 = newSource4.ReplaceNode(fullLineSN, block.NormalizeWhitespace().WithLeadingTrivia(fullLineSN.GetLeadingTrivia()).WithTrailingTrivia(fullLineSN.GetTrailingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));
                                }
                                else
                                    newSource4 = newSource4.ReplaceNode(fullLineSN, block.NormalizeWhitespace().WithLeadingTrivia(fullLineSN.GetLeadingTrivia()).WithTrailingTrivia(fullLineSN.GetTrailingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));

                                //Updating nodes
                                nodes = newSource4.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(n => n.Expression.ChildNodes().Count<object>() > 0 && returnStatW.changingMethods.ContainsKey(n.Expression.ChildNodes().ElementAt(n.Expression.ChildNodes().Count<object>() - 1).ToString())
                                || n.Expression.ChildNodes().Count<object>() == 0 && returnStatW.changingMethods.ContainsKey(n.Expression.ToString()));
                            }
                            indexMDNodes++;
                        }
                        //Part I - Replacing calls to methods to be transformed from other classes that are not the current class
                        //Iterating in the methods declaration nodes of the actual document/class in analysis
                        foreach (MethodDeclarationSyntax node in documentNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                        {
                            //Check if current method needs to be transformed
                            if (methodsToReplace.Contains(node.Identifier.ToString()))
                            {
                                //Retrieving symbols for current method
                                var mdSymbols = model.GetDeclaredSymbol(node);
                                //Retrieving references to the current method
                                var mdReferences = Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindReferencesAsync(mdSymbols, newSolution).Result;
                                //Iterating in method references
                                foreach (var references in mdReferences.ElementAt(0).Locations)
                                {
                                    //Check for other documents/classes
                                    if (references.Location.SourceTree.FilePath != document.FilePath)
                                    {
                                        //Retrieving Syntax Tree of the file of the current references
                                        var currentSourceNode = references.Location.SourceTree.GetRoot();
                                        //Search for refereces node in the syntax tree
                                        var currentNode = currentSourceNode.FindNode(references.Location.SourceSpan);

                                        //Up in the tree to find the Invocation Expression Syntax Node
                                        var invoNode = ((InvocationExpressionSyntax)currentNode.Parent.Parent);
                                        //Create a copy to work in it
                                        InvocationExpressionSyntax newInvoNode = invoNode;

                                        //List to stored the Statement Syntax nodes of the new transformation
                                        List<StatementSyntax> ssList = new System.Collections.Generic.List<StatementSyntax>();
                                        //Arguments of the method invocation
                                        var invoArgs = invoNode.ArgumentList.Arguments;
                                        int argsIndex = 0;
                                        //Iterating the invocation arguments in order to remove ref keyword
                                        foreach (var argum in invoArgs)
                                        {
                                            //Removing ref keyword
                                            var newArgum = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(argum.ToString().Replace("ref ", "").Replace("out ", "")));
                                            //Replacing node
                                            newInvoNode = newInvoNode.ReplaceNode(newInvoNode.ArgumentList.Arguments[argsIndex], newArgum).WithLeadingTrivia(newInvoNode.GetLeadingTrivia()).WithTrailingTrivia(newInvoNode.GetTrailingTrivia());
                                            //Update the invocation arguments
                                            invoArgs = newInvoNode.ArgumentList.Arguments;
                                            argsIndex++;
                                        }
                                        //Add invoke stament node
                                        ssList.Add(SyntaxFactory.ParseStatement("var returningMetodValue" + indexMDNodes.ToString() + " = " + newInvoNode.ToFullString() + ";").NormalizeWhitespace().WithLeadingTrivia(newInvoNode.GetLeadingTrivia()).WithTrailingTrivia(newInvoNode.GetTrailingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));

                                        //Returning value assigment node
                                        SyntaxNode rvNode = SyntaxFactory.ParseExpression("returningMetodValue" + indexMDNodes + "." + "returnValue").SyntaxTree.GetRoot().NormalizeWhitespace().WithLeadingTrivia(newInvoNode.GetLeadingTrivia()).WithTrailingTrivia(newInvoNode.GetTrailingTrivia());

                                        //Retrieving full line syntax node
                                        SyntaxNode fullLineSN = invoNode;
                                        //Flag for If/While/For statements
                                        bool isIfStat = false;
                                        bool isSwitchStat = false;
                                        while (!fullLineSN.GetLastToken().IsKind(SyntaxKind.SemicolonToken))
                                        {
                                            if (fullLineSN.IsKind(SyntaxKind.IfStatement) || fullLineSN.IsKind(SyntaxKind.WhileStatement) || fullLineSN.IsKind(SyntaxKind.ForStatement))
                                            {
                                                isIfStat = true;
                                                break;
                                            }
                                            else if (fullLineSN.IsKind(SyntaxKind.SwitchStatement))
                                            {
                                                isSwitchStat = true;
                                                break;
                                            }
                                            fullLineSN = fullLineSN.Parent;
                                        }

                                        //Dictionary ref parameters
                                        IDictionary<string, TypeInfo> parametersDicc = new Dictionary<string, TypeInfo>();
                                        foreach (ParameterSyntax parameter in node.ParameterList.Parameters)
                                        {
                                            foreach (SyntaxToken stl in parameter.Modifiers)
                                            {
                                                if (stl.IsKind(SyntaxKind.RefKeyword) || stl.IsKind(SyntaxKind.OutKeyword))
                                                {
                                                    //Adding identifies and type info
                                                    TypeInfo initializerInfo = model.GetTypeInfo(parameter.Type);
                                                    parametersDicc.Add(parameter.Identifier.ToString(), initializerInfo);
                                                }
                                            }
                                        }
                                        //Check for returning types different to "void"
                                        if (!model.GetTypeInfo(node.ReturnType).Type.ToString().Equals("void"))
                                        {
                                            if (isIfStat)
                                            {
                                                //Creating and add if expresion
                                                string expIf = fullLineSN.ReplaceNode(invoNode, rvNode).ToString();
                                                ssList.Add(SyntaxFactory.ParseStatement(expIf.Substring(0, expIf.IndexOf('{') + 1)).NormalizeWhitespace().WithLeadingTrivia(invoNode.GetLeadingTrivia()).WithTrailingTrivia(invoNode.GetTrailingTrivia()));
                                            }
                                            else
                                            {
                                                //add other expression
                                                if (fullLineSN.DescendantNodes().ElementAt(0).IsKind(SyntaxKind.SimpleAssignmentExpression) || fullLineSN.DescendantNodes().ElementAt(0).IsKind(SyntaxKind.VariableDeclaration))
                                                    ssList.Add(SyntaxFactory.ParseStatement(fullLineSN.ReplaceNode(invoNode, rvNode).NormalizeWhitespace().WithLeadingTrivia(invoNode.GetLeadingTrivia()).WithTrailingTrivia(invoNode.GetTrailingTrivia()).ToString()));
                                            }
                                        }
                                        //Updating invoke arguments
                                        invoArgs = invoNode.ArgumentList.Arguments;
                                        argsIndex = 0;
                                        //Iterating in invoke arguments in order to add the new expressions
                                        foreach (var argum in invoArgs)
                                        {
                                            if (argum.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) || argum.IsKind(SyntaxKind.OutKeyword))
                                            {
                                                //Building the expression to add
                                                var newArgum = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(argum.ToString().Replace("ref ", "").Replace("out ", "")));
                                                string finalExpression = newArgum.ToFullString() + " = " + "returningMetodValue" + indexMDNodes + "." + parametersDicc.Keys.ElementAt(argsIndex) + ";";
                                                ssList.Add(SyntaxFactory.ParseStatement(finalExpression).WithLeadingTrivia(fullLineSN.GetLeadingTrivia()).WithTrailingTrivia(SyntaxFactory.Whitespace("\n")));
                                                argsIndex++;
                                            }
                                        }

                                        //If is if statement, add the rest of the if block
                                        if (isIfStat)
                                            ssList.Add(((BlockSyntax)fullLineSN.ChildNodes().ElementAt(1)).WithOpenBraceToken(SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken)));

                                        if(isSwitchStat)
                                        {
                                            ssList.Add(SyntaxFactory.ParseStatement(fullLineSN.ReplaceNode(invoNode, rvNode).NormalizeWhitespace().WithLeadingTrivia(invoNode.GetLeadingTrivia()).WithTrailingTrivia(invoNode.GetTrailingTrivia()).ToString()));
                                        }

                                        //Create a block with the statements of the new node
                                        var block = SyntaxFactory.Block(ssList);

                                        block = block.WithOpenBraceToken(SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken))
                                                .WithCloseBraceToken(SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));

                                        //Adding old and new nodes to nodeToChanges array, latter we are going to replace file by file
                                        Dictionary<SyntaxNode, SyntaxNode> ds;
                                        if (nodeToChanges.TryGetValue(references.Location.SourceTree, out ds))
                                        {
                                            nodeToChanges[references.Location.SourceTree].Add(fullLineSN, block);
                                        }
                                        else
                                        {
                                            var nodesOfFile = new Dictionary<SyntaxNode, SyntaxNode>();
                                            nodesOfFile.Add(fullLineSN, block);
                                            nodeToChanges.Add(references.Location.SourceTree, nodesOfFile);
                                        }
                                    }
                                    indexMDNodes++;


                                }
                            }
                        }
                        //Replacing new nodes in respective file in the new solution
                        foreach (var fileToReplace in nodeToChanges)
                        {
                            var newFileST = fileToReplace.Key.GetRoot().ReplaceNodes(fileToReplace.Value.Keys, (OldNode, newNode) =>
                            {
                                return fileToReplace.Value[OldNode];
                            });
                            newSolution = newSolution.WithDocumentSyntaxRoot(project.GetDocumentId(fileToReplace.Key.GetRoot().SyntaxTree), newFileST);
                        }
                        //Updating solution
                        if (newSource4 != sourceTree.GetRoot())
                        {
                            newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, newSource4);
                        }

                        indexDoc++;
                        progressBar1.Increment(1);
                        solution = newSolution;
                    }
                }
                indexProj++;
            }

            //Applying changes in workspace
            bool result = msworkspace.TryApplyChanges(newSolution);
            MessageBox.Show("Process Finish Sucesfully. " + totalReplcaes + " Replaces in " + totalFiles + " Files." );
            button1.Enabled = true;
        }

        private static Compilation CreateTestCompilation(string path)
        {
            var programPath = path;
            var programText = File.ReadAllText(programPath);
            var programTree =
                           CSharpSyntaxTree.ParseText(programText)
                                           .WithFilePath(programPath);


            SyntaxTree[] sourceTrees = { programTree };

            MetadataReference mscorlib =
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference codeAnalysis =
                    MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location);
            MetadataReference csharpCodeAnalysis =
                    MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location);

            MetadataReference[] references = { mscorlib, codeAnalysis, csharpCodeAnalysis };

            return CSharpCompilation.Create("TransformationCS",
                                            sourceTrees,
                                            references,
                                            new CSharpCompilationOptions(
                                                    OutputKind.ConsoleApplication));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog1.ShowDialog();
            textBox1.Text = openFileDialog1.FileName;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog2.ShowDialog();
            textBox3.Text = openFileDialog2.FileName;
        }
    }
}
