using TID_CodeAnaliser.Core;

var arguments = CliArguments.Parse(args);
var rootPath = arguments.TryGetValue("root", out var root) ? root : Directory.GetCurrentDirectory();
var outputDirectory = arguments.TryGetValue("output", out var output)
    ? output
    : Path.Combine(rootPath, ".tidcodeanaliser");
var configPath = arguments.TryGetValue("config", out var config)
    ? config
    : Path.Combine(rootPath, "tidcodeanaliser.json");

Directory.CreateDirectory(outputDirectory);

var options = AnalysisEngine.LoadOptions(configPath);
var engine = new AnalysisEngine(new IRule[]
{
    new LargeClassRule(),
    new LongMethodRule(),
    new TooManyDependenciesRule(),
    new CqrsMixingRule(),
    new DirectInfrastructureUsageRule(),
    new ControllerBusinessLogicRule(),
    new DuplicateMethodBodyRule(),
    new DbCallInsideLoopRule(),
    new SyncOverAsyncRule(),
    new GenericExceptionRule()
});

Console.WriteLine("[TID_CodeAnaliser_CS] Iniciando análise...");
Console.WriteLine($"[TID_CodeAnaliser_CS] Root   : {rootPath}");
Console.WriteLine($"[TID_CodeAnaliser_CS] Output : {outputDirectory}");
Console.WriteLine($"[TID_CodeAnaliser_CS] Config : {configPath}");

var report = engine.Analyze(rootPath, options);
var jsonPath = Path.Combine(outputDirectory, "report.json");
var mdPath = Path.Combine(outputDirectory, "report.md");
var htmlPath = Path.Combine(outputDirectory, "report.html");

ReportWriters.WriteJson(report, jsonPath);
ReportWriters.WriteMarkdown(report, mdPath);
ReportWriters.WriteHtml(report, htmlPath);

Console.WriteLine();
Console.WriteLine($"Arquivos analisados : {report.Summary.TotalFiles}");
Console.WriteLine($"Syntax trees        : {report.Summary.TotalSyntaxTrees}");
Console.WriteLine($"Findings            : {report.Summary.TotalFindings}");
Console.WriteLine($"Score geral         : {report.Summary.OverallScore}/100");
Console.WriteLine($"Critical            : {report.Summary.Critical}");
Console.WriteLine($"High                : {report.Summary.High}");
Console.WriteLine($"Medium              : {report.Summary.Medium}");
Console.WriteLine($"Low                 : {report.Summary.Low}");
Console.WriteLine();
Console.WriteLine($"JSON     : {jsonPath}");
Console.WriteLine($"Markdown : {mdPath}");
Console.WriteLine($"HTML     : {htmlPath}");

internal static class CliArguments
{
    public static Dictionary<string, string> Parse(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = current[2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";

            dict[key] = value;
        }

        return dict;
    }
}
