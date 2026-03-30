using System.Text.Json;

namespace TID_CodeAnaliser.Core;

public sealed class AnalysisEngine
{
    private readonly IReadOnlyList<IRule> _rules;
    private readonly IAiSuggestionAgent? _aiSuggestionAgent;

    public AnalysisEngine(IEnumerable<IRule> rules, IAiSuggestionAgent? aiSuggestionAgent = null)
    {
        _rules = rules.ToList();
        _aiSuggestionAgent = aiSuggestionAgent;
    }

    public ProjectAnalysisReport Analyze(string rootPath, AnalysisOptions options)
    {
        var context = ProjectContextFactory.Create(rootPath, options);
        var findings = _rules
            .SelectMany(rule => rule.Evaluate(context, options))
            .OrderByDescending(GetWeight)
            .ThenBy(x => x.FilePath)
            .ThenBy(x => x.StartLine)
            .ToList();

        if (options.EnableAiSuggestions && _aiSuggestionAgent is not null)
        {
            findings = EnrichWithAiSuggestions(findings, context, options);
        }

        var fileScores = findings
            .GroupBy(x => x.FilePath)
            .Select(g => new FileScore
            {
                FilePath = g.Key,
                Findings = g.Count(),
                Score = g.Sum(GetWeight)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Findings)
            .ToList();

        var categoryScores = findings
            .GroupBy(x => x.Category)
            .Select(g => new CategoryScore
            {
                Category = g.Key,
                Findings = g.Count(),
                Score = g.Sum(GetWeight)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Findings)
            .ToList();

        var totalPenalty = findings.Sum(GetWeight);
        var overallScore = Math.Max(0, 100 - totalPenalty);

        return new ProjectAnalysisReport
        {
            RootPath = rootPath,
            Summary = new AnalysisSummary
            {
                TotalFiles = context.Files.Count,
                TotalSyntaxTrees = context.SyntaxTrees.Count,
                TotalFindings = findings.Count,
                Critical = findings.Count(x => x.Severity == FindingSeverity.Critical),
                High = findings.Count(x => x.Severity == FindingSeverity.High),
                Medium = findings.Count(x => x.Severity == FindingSeverity.Medium),
                Low = findings.Count(x => x.Severity == FindingSeverity.Low),
                OverallScore = overallScore
            },
            CategoryScores = categoryScores,
            FileScores = fileScores,
            Findings = findings
        };
    }

    private List<RuleFinding> EnrichWithAiSuggestions(List<RuleFinding> findings, ProjectContext context, AnalysisOptions options)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(3, options.AiTimeoutSeconds)));
        var maxSuggestions = Math.Max(1, options.AiMaxSuggestionsPerRun);
        var results = new List<RuleFinding>(findings.Count);

        for (var i = 0; i < findings.Count; i++)
        {
            var finding = findings[i];
            if (i >= maxSuggestions)
            {
                results.Add(finding);
                continue;
            }

            string? aiSuggestion;
            try
            {
                var codeContext = BuildCodeContextForFinding(context, finding);
                aiSuggestion = _aiSuggestionAgent!
                    .SuggestAsync(finding, codeContext, cts.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                aiSuggestion = null;
            }

            results.Add(CloneWithAiSuggestion(finding, aiSuggestion));
        }

        return results;
    }

    private static string? BuildCodeContextForFinding(ProjectContext context, RuleFinding finding)
    {
        var sourceFile = context.Files.FirstOrDefault(f => f.RelativePath.Equals(finding.FilePath, StringComparison.OrdinalIgnoreCase));
        if (sourceFile is null)
        {
            return null;
        }

        if (finding.StartLine is null || finding.EndLine is null)
        {
            return Helpers.TrimSnippet(sourceFile.Content, 1200);
        }

        var padding = 6;
        var start = Math.Max(1, finding.StartLine.Value - padding);
        var end = Math.Min(sourceFile.Lines.Length, finding.EndLine.Value + padding);
        if (start > end)
        {
            return null;
        }

        var sb = new System.Text.StringBuilder();
        for (var i = start; i <= end; i++)
        {
            sb.Append(i.ToString("0000")).Append(": ").AppendLine(sourceFile.Lines[i - 1]);
        }

        return sb.ToString();
    }

    private static RuleFinding CloneWithAiSuggestion(RuleFinding finding, string? aiSuggestion)
        => new()
        {
            RuleId = finding.RuleId,
            Title = finding.Title,
            Category = finding.Category,
            FilePath = finding.FilePath,
            SymbolName = finding.SymbolName,
            StartLine = finding.StartLine,
            EndLine = finding.EndLine,
            Severity = finding.Severity,
            Description = finding.Description,
            Recommendation = finding.Recommendation,
            Evidence = finding.Evidence,
            CodexPrompt = finding.CodexPrompt,
            AiSuggestion = aiSuggestion
        };

    public static AnalysisOptions LoadOptions(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new AnalysisOptions();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AnalysisOptions>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new AnalysisOptions();
    }

    public static int GetWeight(RuleFinding finding) => GetWeight(finding.Severity);

    public static int GetWeight(FindingSeverity severity) => severity switch
    {
        FindingSeverity.Critical => 12,
        FindingSeverity.High => 7,
        FindingSeverity.Medium => 4,
        _ => 1
    };
}
