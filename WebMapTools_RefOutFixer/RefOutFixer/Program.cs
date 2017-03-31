using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using Microsoft.CodeAnalysis.MSBuild;

namespace ASTTransformationTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            MainForm mf = new MainForm();
            mf.ShowDialog();


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
    }
}
