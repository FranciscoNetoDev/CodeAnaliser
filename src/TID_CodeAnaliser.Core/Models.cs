using System.Text.Json.Serialization;

namespace TID_CodeAnaliser.Core;

public sealed class AnalysisOptions
{
    public int MaxClassNonEmptyLines { get; set; } = 300;
    public int MaxMethodNonEmptyLines { get; set; } = 40;
    public int MaxConstructorDependencies { get; set; } = 5;
    public int DuplicateMethodMinLines { get; set; } = 8;
    public int MaxControllerComplexityHints { get; set; } = 3;
    public string[] ExcludedDirectories { get; set; } = ["bin", "obj", ".git", ".vs", "packages", "node_modules", "Migrations"];
    public string[] IncludedExtensions { get; set; } = [".cs"];
    public string[] ApplicationPathHints { get; set; } = ["Application", "Handlers", "Services", "UseCases"];
    public string[] InfrastructurePathHints { get; set; } = ["Infrastructure", "Infra", "Repository", "Repositories", "Persistence", "Data"];
    public string[] DirectInfrastructureTypeTokens { get; set; } = ["SqlConnection", "FbConnection", "DbContext", "IDbConnection", "IDbTransaction", "Dapper", "DatabaseFacade"];
    public bool EnableAiSuggestions { get; set; } = false;
    public string AiProvider { get; set; } = "OpenAI";
    public string AiModel { get; set; } = "gpt-4.1-mini";
    public string AiEndpoint { get; set; } = "https://api.openai.com/v1/responses";
    public string AiApiKeyEnvVar { get; set; } = "OPENAI_API_KEY";
    public int AiSuggestionMaxTokens { get; set; } = 220;
    public int AiTimeoutSeconds { get; set; } = 20;
    public int AiMaxSuggestionsPerRun { get; set; } = 30;
}

public sealed class ProjectAnalysisReport
{
    public required string RootPath { get; init; }
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public required AnalysisSummary Summary { get; init; }
    public required IReadOnlyList<CategoryScore> CategoryScores { get; init; }
    public required IReadOnlyList<FileScore> FileScores { get; init; }
    public required IReadOnlyList<RuleFinding> Findings { get; init; }
}

public sealed class AnalysisSummary
{
    public int TotalFiles { get; init; }
    public int TotalSyntaxTrees { get; init; }
    public int TotalFindings { get; init; }
    public int Critical { get; init; }
    public int High { get; init; }
    public int Medium { get; init; }
    public int Low { get; init; }
    public int OverallScore { get; init; }
}

public sealed class CategoryScore
{
    public required string Category { get; init; }
    public int Score { get; init; }
    public int Findings { get; init; }
}

public sealed class FileScore
{
    public required string FilePath { get; init; }
    public int Score { get; init; }
    public int Findings { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FindingSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public sealed class RuleFinding
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
    public string? CodexPrompt { get; init; }
    public string? AiSuggestion { get; init; }
}

public sealed class SourceFile
{
    public required string FilePath { get; init; }
    public required string RelativePath { get; init; }
    public required string Content { get; init; }
    public required string[] Lines { get; init; }
}
