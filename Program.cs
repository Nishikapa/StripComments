using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System.IO;
using System.Linq;

namespace StripComments
{
    public class Rewriter : CSharpSyntaxRewriter
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia) =>
            (
                SyntaxKind.SingleLineCommentTrivia == trivia.Kind() ||
                SyntaxKind.MultiLineCommentTrivia == trivia.Kind()
            ) ? default(SyntaxTrivia) : base.VisitTrivia(trivia);
    }

    static class Program
    {
        static void Main(string[] args)
        {
            var workspace = MSBuildWorkspace.Create();

            workspace.WorkspaceFailed += (sender, _args) =>
                System.Diagnostics.Debug.WriteLine(_args.Diagnostic.Message);

            foreach (var (NewPath, Contents) in
                from p in workspace.OpenSolutionAsync(args[0]).Result.Projects
                from s in p.GetCompilationAsync().Result.SyntaxTrees
                let DirectoryName = Path.GetDirectoryName(s.FilePath)
                let FileNameWithoutExtension = Path.GetFileNameWithoutExtension(s.FilePath)
                let Extension = Path.GetExtension(s.FilePath)
                let NewPath = Path.Combine(DirectoryName, $"_{FileNameWithoutExtension}{Extension}")
                let Contents = new Rewriter().Visit(s.GetRoot()).ToFullString()
                select (NewPath, Contents))
            {
                File.WriteAllText(NewPath, Contents);
            }
        }
    }
}
