using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TID_CodeAnaliser.Core;

public sealed class ProjectContext
{
    public required string RootPath { get; init; }
    public required IReadOnlyList<SourceFile> Files { get; init; }
    public required IReadOnlyList<SyntaxTreeContext> SyntaxTrees { get; init; }
}

public sealed class SyntaxTreeContext
{
    public required SourceFile SourceFile { get; init; }
    public required SyntaxTree SyntaxTree { get; init; }
    public required CSharpSyntaxNode Root { get; init; }
    public required SemanticModel SemanticModel { get; init; }
}
