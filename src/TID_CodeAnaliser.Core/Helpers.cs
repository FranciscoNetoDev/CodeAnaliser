using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TID_CodeAnaliser.Core;

internal static class Helpers
{
    public static int CountNonEmptyLines(IEnumerable<string> lines) => lines.Count(x => !string.IsNullOrWhiteSpace(x));

    public static string NormalizeCode(string text)
    {
        text = Regex.Replace(text, @"//.*", string.Empty);
        text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim().ToLowerInvariant();
    }

    public static (int startLine, int endLine) GetLineSpan(SyntaxNode node)
    {
        var span = node.SyntaxTree.GetLineSpan(node.Span);
        return (span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1);
    }

    public static string TrimSnippet(string value, int maxLength = 220)
    {
        value = value.Replace(Environment.NewLine, " ").Trim();
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    public static bool PathContainsAny(string path, IEnumerable<string> hints)
        => hints.Any(h => path.Contains(h, StringComparison.OrdinalIgnoreCase));

    public static int CountComplexityHints(BlockSyntax? body)
    {
        if (body is null)
        {
            return 0;
        }

        var count = 0;
        count += body.DescendantNodes().OfType<IfStatementSyntax>().Count();
        count += body.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
        count += body.DescendantNodes().OfType<ForStatementSyntax>().Count();
        count += body.DescendantNodes().OfType<WhileStatementSyntax>().Count();
        count += body.DescendantNodes().OfType<SwitchStatementSyntax>().Count();
        count += body.DescendantNodes().OfType<TryStatementSyntax>().Count();
        return count;
    }

    public static string BuildCodexPrompt(RuleFindingDraft draft)
    {
        var sb = new StringBuilder();
        sb.Append("Refatore o arquivo '").Append(draft.FilePath).Append("'");
        if (!string.IsNullOrWhiteSpace(draft.SymbolName))
        {
            sb.Append(", focando no símbolo '").Append(draft.SymbolName).Append("'");
        }
        sb.Append(". Corrija o problema '").Append(draft.Title).Append("'. ");
        sb.Append(draft.Recommendation).Append(' ');
        sb.Append("Preserve comportamento, contratos públicos, testes existentes e padrão arquitetural do projeto. Evite mudanças cosméticas sem necessidade.");
        return sb.ToString();
    }
}

internal sealed class RuleFindingDraft
{
    public required string RuleId { get; init; }
    public required string Title { get; init; }
    public required string Category { get; init; }
    public required string FilePath { get; init; }
    public string? SymbolName { get; init; }
    public int? StartLine { get; init; }
    public int? EndLine { get; init; }
    public required FindingSeverity Severity { get; init; }
    public required string Description { get; init; }
    public required string Recommendation { get; init; }
    public string? Evidence { get; init; }

    public RuleFinding ToFinding()
        => new()
        {
            RuleId = RuleId,
            Title = Title,
            Category = Category,
            FilePath = FilePath,
            SymbolName = SymbolName,
            StartLine = StartLine,
            EndLine = EndLine,
            Severity = Severity,
            Description = Description,
            Recommendation = Recommendation,
            Evidence = Evidence,
            CodexPrompt = Helpers.BuildCodexPrompt(this)
        };
}
