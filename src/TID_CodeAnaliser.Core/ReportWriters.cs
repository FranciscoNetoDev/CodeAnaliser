using System.Text;
using System.Text.Json;

namespace TID_CodeAnaliser.Core;

public static class ReportWriters
{
    public static void WriteJson(ProjectAnalysisReport report, string outputPath)
    {
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
        File.WriteAllText(outputPath, json, Encoding.UTF8);
    }

    public static void WriteMarkdown(ProjectAnalysisReport report, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# TID_CodeAnaliser_CS - Report");
        sb.AppendLine();
        sb.AppendLine($"- Root: `{report.RootPath}`");
        sb.AppendLine($"- Generated (UTC): `{report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}`");
        sb.AppendLine($"- Files scanned: **{report.Summary.TotalFiles}**");
        sb.AppendLine($"- Syntax trees: **{report.Summary.TotalSyntaxTrees}**");
        sb.AppendLine($"- Findings: **{report.Summary.TotalFindings}**");
        sb.AppendLine($"- Overall score: **{report.Summary.OverallScore}/100**");
        sb.AppendLine($"- Critical: **{report.Summary.Critical}** | High: **{report.Summary.High}** | Medium: **{report.Summary.Medium}** | Low: **{report.Summary.Low}**");
        sb.AppendLine();

        sb.AppendLine("## Score por categoria");
        sb.AppendLine();
        foreach (var category in report.CategoryScores)
        {
            sb.AppendLine($"- **{category.Category}** — score `{category.Score}`, findings `{category.Findings}`");
        }
        sb.AppendLine();

        sb.AppendLine("## Top arquivos problemáticos");
        sb.AppendLine();
        foreach (var score in report.FileScores.Take(15))
        {
            sb.AppendLine($"- `{score.FilePath}` — score **{score.Score}**, findings **{score.Findings}**");
        }
        sb.AppendLine();

        sb.AppendLine("## Findings");
        sb.AppendLine();
        foreach (var finding in report.Findings)
        {
            sb.AppendLine($"### [{finding.RuleId}] {finding.Title}");
            sb.AppendLine($"- Categoria: **{finding.Category}**");
            sb.AppendLine($"- Arquivo: `{finding.FilePath}`");
            if (!string.IsNullOrWhiteSpace(finding.SymbolName))
            {
                sb.AppendLine($"- Símbolo: `{finding.SymbolName}`");
            }
            if (finding.StartLine is not null)
            {
                sb.AppendLine($"- Linha(s): `{finding.StartLine}` a `{finding.EndLine ?? finding.StartLine}`");
            }
            sb.AppendLine($"- Severidade: **{finding.Severity}**");
            sb.AppendLine($"- Descrição: {finding.Description}");
            sb.AppendLine($"- Recomendação: {finding.Recommendation}");
            if (!string.IsNullOrWhiteSpace(finding.Evidence))
            {
                sb.AppendLine($"- Evidência: `{finding.Evidence}`");
            }
            if (!string.IsNullOrWhiteSpace(finding.CodexPrompt))
            {
                sb.AppendLine("- Prompt Codex:");
                sb.AppendLine();
                sb.AppendLine("```text");
                sb.AppendLine(finding.CodexPrompt);
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    public static void WriteHtml(ProjectAnalysisReport report, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"pt-BR\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine("  <title>TID_CodeAnaliser_CS Report</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body{font-family:Segoe UI,Arial,sans-serif;margin:24px;background:#f7f8fa;color:#1b1f24}");
        sb.AppendLine("    .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px;margin:16px 0}");
        sb.AppendLine("    .card{background:#fff;border:1px solid #dfe3e8;border-radius:12px;padding:16px;box-shadow:0 2px 8px rgba(0,0,0,.04)}");
        sb.AppendLine("    table{width:100%;border-collapse:collapse;background:#fff;border-radius:12px;overflow:hidden}");
        sb.AppendLine("    th,td{padding:10px 12px;border-bottom:1px solid #eef1f4;text-align:left;vertical-align:top}");
        sb.AppendLine("    th{background:#f0f3f7}");
        sb.AppendLine("    .sev-Critical{color:#8b0000;font-weight:700}.sev-High{color:#a04a00;font-weight:700}.sev-Medium{color:#8b6a00;font-weight:700}.sev-Low{color:#2f6f2f;font-weight:700}");
        sb.AppendLine("    pre{white-space:pre-wrap;background:#10141a;color:#e6edf3;padding:12px;border-radius:8px;overflow:auto}");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <h1>TID_CodeAnaliser_CS</h1>");
        sb.AppendLine($"  <p><strong>Root:</strong> {EscapeHtml(report.RootPath)}<br/><strong>Generated (UTC):</strong> {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("  <div class=\"grid\">");
        sb.AppendLine($"    <div class=\"card\"><strong>Arquivos</strong><br/>{report.Summary.TotalFiles}</div>");
        sb.AppendLine($"    <div class=\"card\"><strong>Findings</strong><br/>{report.Summary.TotalFindings}</div>");
        sb.AppendLine($"    <div class=\"card\"><strong>Score Geral</strong><br/>{report.Summary.OverallScore}/100</div>");
        sb.AppendLine($"    <div class=\"card\"><strong>Critical</strong><br/>{report.Summary.Critical}</div>");
        sb.AppendLine($"    <div class=\"card\"><strong>High</strong><br/>{report.Summary.High}</div>");
        sb.AppendLine($"    <div class=\"card\"><strong>Medium</strong><br/>{report.Summary.Medium}</div>");
        sb.AppendLine($"    <div class=\"card\"><strong>Low</strong><br/>{report.Summary.Low}</div>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <h2>Score por categoria</h2>");
        sb.AppendLine("  <table><thead><tr><th>Categoria</th><th>Score</th><th>Findings</th></tr></thead><tbody>");
        foreach (var category in report.CategoryScores)
        {
            sb.AppendLine($"<tr><td>{EscapeHtml(category.Category)}</td><td>{category.Score}</td><td>{category.Findings}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("  <h2>Top arquivos problemáticos</h2>");
        sb.AppendLine("  <table><thead><tr><th>Arquivo</th><th>Score</th><th>Findings</th></tr></thead><tbody>");
        foreach (var file in report.FileScores.Take(20))
        {
            sb.AppendLine($"<tr><td>{EscapeHtml(file.FilePath)}</td><td>{file.Score}</td><td>{file.Findings}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("  <h2>Findings</h2>");
        foreach (var finding in report.Findings)
        {
            sb.AppendLine("  <div class=\"card\" style=\"margin-bottom:16px\">");
            sb.AppendLine($"    <h3>[{EscapeHtml(finding.RuleId)}] {EscapeHtml(finding.Title)}</h3>");
            sb.AppendLine($"    <p><strong>Categoria:</strong> {EscapeHtml(finding.Category)}<br/><strong>Arquivo:</strong> {EscapeHtml(finding.FilePath)}<br/><strong>Severidade:</strong> <span class=\"sev-{finding.Severity}\">{finding.Severity}</span></p>");
            if (!string.IsNullOrWhiteSpace(finding.SymbolName))
            {
                sb.AppendLine($"    <p><strong>Símbolo:</strong> {EscapeHtml(finding.SymbolName)}</p>");
            }
            if (finding.StartLine is not null)
            {
                sb.AppendLine($"    <p><strong>Linhas:</strong> {finding.StartLine} a {finding.EndLine ?? finding.StartLine}</p>");
            }
            sb.AppendLine($"    <p><strong>Descrição:</strong> {EscapeHtml(finding.Description)}</p>");
            sb.AppendLine($"    <p><strong>Recomendação:</strong> {EscapeHtml(finding.Recommendation)}</p>");
            if (!string.IsNullOrWhiteSpace(finding.Evidence))
            {
                sb.AppendLine($"    <p><strong>Evidência:</strong> {EscapeHtml(finding.Evidence)}</p>");
            }
            if (!string.IsNullOrWhiteSpace(finding.CodexPrompt))
            {
                sb.AppendLine($"    <pre>{EscapeHtml(finding.CodexPrompt)}</pre>");
            }
            sb.AppendLine("  </div>");
        }

        sb.AppendLine("</body></html>");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeHtml(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
