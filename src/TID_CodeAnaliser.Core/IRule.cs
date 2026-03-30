namespace TID_CodeAnaliser.Core;

public interface IRule
{
    string RuleId { get; }
    string Title { get; }
    string Category { get; }
    IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options);
}
