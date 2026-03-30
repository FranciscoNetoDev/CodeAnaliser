using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TID_CodeAnaliser.Core;

public interface IAiSuggestionAgent
{
    Task<string?> SuggestAsync(RuleFinding finding, CancellationToken cancellationToken = default);
}

public sealed class OpenAiSuggestionAgent : IAiSuggestionAgent
{
    private readonly HttpClient _httpClient;
    private readonly AnalysisOptions _options;
    private readonly string _apiKey;

    public OpenAiSuggestionAgent(HttpClient httpClient, AnalysisOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _apiKey = Environment.GetEnvironmentVariable(options.AiApiKeyEnvVar) ?? string.Empty;
    }

    public Task<string?> SuggestAsync(RuleFinding finding, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return Task.FromResult<string?>(null);
        }

        return ExecuteRequestAsync(finding, cancellationToken);
    }

    private async Task<string?> ExecuteRequestAsync(RuleFinding finding, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.AiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var prompt = BuildPrompt(finding);
        var payload = new
        {
            model = _options.AiModel,
            input = prompt,
            max_output_tokens = _options.AiSuggestionMaxTokens
        };

        var json = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ExtractOutputText(body);
    }

    private static string BuildPrompt(RuleFinding finding)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é um revisor de código C# especialista em arquitetura.");
        sb.AppendLine("Retorne uma sugestão prática e curta (máximo 8 linhas) para corrigir o problema.");
        sb.AppendLine("Inclua 1 mini exemplo de código quando fizer sentido.");
        sb.AppendLine();
        sb.AppendLine($"RuleId: {finding.RuleId}");
        sb.AppendLine($"Título: {finding.Title}");
        sb.AppendLine($"Categoria: {finding.Category}");
        sb.AppendLine($"Arquivo: {finding.FilePath}");
        sb.AppendLine($"Símbolo: {finding.SymbolName}");
        sb.AppendLine($"Descrição: {finding.Description}");
        sb.AppendLine($"Recomendação base: {finding.Recommendation}");
        sb.AppendLine($"Evidência: {finding.Evidence}");
        return sb.ToString();
    }

    private static string? ExtractOutputText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("output_text", out var outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            return outputTextElement.GetString();
        }

        if (doc.RootElement.TryGetProperty("output", out var outputElement) &&
            outputElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputElement.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var contentElement) || contentElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var content in contentElement.EnumerateArray())
                {
                    if (!content.TryGetProperty("text", out var textElement) || textElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.Trim();
                    }
                }
            }
        }

        return null;
    }
}
