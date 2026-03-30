using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TID_CodeAnaliser.Core;

public static class ProjectContextFactory
{
    public static ProjectContext Create(string rootPath, AnalysisOptions options)
    {
        var files = SourceDiscovery.Discover(rootPath, options);
        var syntaxTrees = files
            .Select(file => CSharpSyntaxTree.ParseText(file.Content, path: file.FilePath))
            .ToList();

        var references = BuildMetadataReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: "TID_CodeAnaliser_Workspace",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var trees = new List<SyntaxTreeContext>();
        foreach (var syntaxTree in syntaxTrees)
        {
            var file = files.First(x => x.FilePath.Equals(syntaxTree.FilePath, StringComparison.OrdinalIgnoreCase));
            var root = (CSharpSyntaxNode)syntaxTree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);

            trees.Add(new SyntaxTreeContext
            {
                SourceFile = file,
                SyntaxTree = syntaxTree,
                Root = root,
                SemanticModel = semanticModel
            });
        }

        return new ProjectContext
        {
            RootPath = rootPath,
            Files = files,
            SyntaxTrees = trees
        };
    }

    private static IEnumerable<MetadataReference> BuildMetadataReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Task).Assembly,
            typeof(Console).Assembly,
            typeof(System.Data.IDbConnection).Assembly
        }
        .Distinct()
        .Select(a => MetadataReference.CreateFromFile(a.Location))
        .ToList();

        return assemblies;
    }
}
