using System.Text.Json;

namespace TID_CodeAnaliser.Core;

public sealed class AnalysisEngine
{
    private readonly IReadOnlyList<IRule> _rules;

    public AnalysisEngine(IEnumerable<IRule> rules)
    {
        _rules = rules.ToList();
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
