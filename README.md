# TID_CodeAnaliser_CS

Versão 2 do motor de auditoria arquitetural para projetos C#.

## O que esta versão entrega

- análise baseada em **Roslyn** (`Microsoft.CodeAnalysis.CSharp`)
- leitura real de classes, métodos, construtores, controllers e namespaces
- score geral do projeto e score por categoria
- findings com severidade, evidência e recomendação
- sugestão objetiva de refatoração por problema encontrado
- **prompt automático para Codex** por finding
- integração opcional com agente de IA (OpenAI) para retornar sugestões adicionais por finding
- relatórios em `json`, `md` e `html`
- configuração externa via `tidcodeanaliser.json`

## Regras incluídas na versão 2

- `TID001` Classe muito grande
- `TID002` Método muito grande
- `TID003` Construtor com dependências demais
- `TID004` Mistura de leitura e escrita no mesmo método
- `TID005` Uso direto de infraestrutura em camada de aplicação/controller/service
- `TID006` Controller com regra de negócio
- `TID007` Duplicação aproximada de corpo de método
- `TID008` Chamada de banco dentro de loop
- `TID009` Uso de APIs síncronas em método assíncrono
- `TID010` Lançamento de `Exception` genérica

## Estrutura

```text
TID_CodeAnaliser_CS/
  src/
    TID_CodeAnaliser.Core/
    TID_CodeAnaliser.Cli/
  tidcodeanaliser.json
  TID_CodeAnaliser_CS.sln
```

## Como executar

```bash
dotnet restore

dotnet run --project ./src/TID_CodeAnaliser.Cli -- \
  --root "C:/caminho/do/projeto" \
  --output "C:/caminho/do/projeto/.tidcodeanaliser" \
  --config "C:/caminho/do/projeto/tidcodeanaliser.json"
```

Se `--output` não for informado, será usado:

```text
<root>/.tidcodeanaliser
```

Se `--config` não for informado, será usado:

```text
<root>/tidcodeanaliser.json
```

## Exemplo de uso simples

```bash
dotnet run --project ./src/TID_CodeAnaliser.Cli -- --root "D:/Projetos/PortalAtendimentoClinico"
```

## Observações

- esta implementação usa Roslyn para análise sintática e semântica leve do código-fonte
- para ativar sugestões por IA, configure no `tidcodeanaliser.json`:

```json
{
  "enableAiSuggestions": true,
  "aiProvider": "OpenAI",
  "aiModel": "gpt-4.1-mini",
  "aiEndpoint": "https://api.openai.com/v1/responses",
  "aiApiKeyEnvVar": "OPENAI_API_KEY",
  "aiSuggestionMaxTokens": 220,
  "aiTimeoutSeconds": 20,
  "aiMaxSuggestionsPerRun": 30
}
```

- e exporte a chave da API antes de rodar:

```bash
export OPENAI_API_KEY="sua-chave"
```

- o ambiente onde o arquivo foi gerado não possui SDK .NET instalado, então os arquivos foram preparados mas não compilados aqui
- para funcionamento completo, use .NET 8 SDK na sua máquina
