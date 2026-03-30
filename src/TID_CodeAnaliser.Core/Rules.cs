using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TID_CodeAnaliser.Core;

public sealed class LargeClassRule : IRule
{
    public string RuleId => "TID001";
    public string Title => "Classe muito grande";
    public string Category => "Design";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees)
        {
            var types = tree.Root.DescendantNodes().OfType<TypeDeclarationSyntax>();
            foreach (var type in types)
            {
                var text = type.GetText().ToString();
                var nonEmpty = Helpers.CountNonEmptyLines(text.Split(Environment.NewLine));
                if (nonEmpty <= options.MaxClassNonEmptyLines)
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(type);
                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    SymbolName = type.Identifier.Text,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = nonEmpty > options.MaxClassNonEmptyLines * 2 ? FindingSeverity.Critical : FindingSeverity.High,
                    Description = $"A classe '{type.Identifier.Text}' possui {nonEmpty} linhas úteis, acima do limite configurado de {options.MaxClassNonEmptyLines}.",
                    Recommendation = "Quebre a classe por responsabilidade. Separe handlers, validators, serviços de domínio, orquestração e acesso a dados.",
                    Evidence = Helpers.TrimSnippet(type.Identifier.Text)
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class LongMethodRule : IRule
{
    public string RuleId => "TID002";
    public string Title => "Método muito grande";
    public string Category => "Design";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees)
        {
            var methods = tree.Root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                var bodyText = method.Body?.ToFullString() ?? method.ExpressionBody?.ToFullString();
                if (string.IsNullOrWhiteSpace(bodyText))
                {
                    continue;
                }

                var nonEmpty = Helpers.CountNonEmptyLines(bodyText.Split(Environment.NewLine));
                if (nonEmpty <= options.MaxMethodNonEmptyLines)
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(method);
                var symbolName = method switch
                {
                    MethodDeclarationSyntax m => m.Identifier.Text,
                    ConstructorDeclarationSyntax c => c.Identifier.Text,
                    _ => method.Kind().ToString()
                };

                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    SymbolName = symbolName,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = nonEmpty > options.MaxMethodNonEmptyLines * 2 ? FindingSeverity.High : FindingSeverity.Medium,
                    Description = $"O método '{symbolName}' possui {nonEmpty} linhas úteis no corpo.",
                    Recommendation = "Extraia passos menores com nomes de intenção. Separe validação, persistência, mapeamento, integração e retorno.",
                    Evidence = Helpers.TrimSnippet(method.ToString())
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class TooManyDependenciesRule : IRule
{
    public string RuleId => "TID003";
    public string Title => "Construtor com dependências demais";
    public string Category => "Architecture";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees)
        {
            foreach (var ctor in tree.Root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                var dependencyCount = ctor.ParameterList.Parameters.Count;
                if (dependencyCount <= options.MaxConstructorDependencies)
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(ctor);
                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    SymbolName = ctor.Identifier.Text,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = dependencyCount >= options.MaxConstructorDependencies + 3 ? FindingSeverity.High : FindingSeverity.Medium,
                    Description = $"O construtor de '{ctor.Identifier.Text}' recebe {dependencyCount} dependências.",
                    Recommendation = "Reavalie responsabilidades. Extraia orquestradores menores, segregue portas e evite classes com dependência excessiva.",
                    Evidence = Helpers.TrimSnippet(ctor.ParameterList.ToString())
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class CqrsMixingRule : IRule
{
    public string RuleId => "TID004";
    public string Title => "Mistura de leitura e escrita";
    public string Category => "CQRS";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();
        var readTokens = new[] { "Get", "Find", "List", "Select", "Query", "Obter", "Buscar" };
        var writeTokens = new[] { "Save", "Insert", "Update", "Delete", "Create", "Add", "Commit" };

        foreach (var tree in context.SyntaxTrees)
        {
            foreach (var method in tree.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var methodName = method.Identifier.Text;
                var body = (method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? string.Empty);
                if (string.IsNullOrWhiteSpace(body))
                    continue;

                var nameHasRead = readTokens.Any(t => methodName.Contains(t, StringComparison.OrdinalIgnoreCase));
                var nameHasWrite = writeTokens.Any(t => methodName.Contains(t, StringComparison.OrdinalIgnoreCase));

                var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
                var hasReadCall = invocations.Any(inv =>
                    inv.Expression is MemberAccessExpressionSyntax ma &&
                    readTokens.Any(t => ma.Name.Identifier.Text.Contains(t, StringComparison.OrdinalIgnoreCase))
                );
                var hasWriteCall = invocations.Any(inv =>
                    inv.Expression is MemberAccessExpressionSyntax ma &&
                    writeTokens.Any(t => ma.Name.Identifier.Text.Contains(t, StringComparison.OrdinalIgnoreCase))
                );

                if ((nameHasRead && nameHasWrite) || (hasReadCall && hasWriteCall))
                {
                    var span = Helpers.GetLineSpan(method);
                    findings.Add(new RuleFindingDraft
                    {
                        RuleId = RuleId,
                        Title = Title,
                        Category = Category,
                        FilePath = tree.SourceFile.RelativePath,
                        SymbolName = method.Identifier.Text,
                        StartLine = span.startLine,
                        EndLine = span.endLine,
                        Severity = FindingSeverity.High,
                        Description = $"O método '{method.Identifier.Text}' mistura operações de leitura e escrita, o que fere o princípio CQRS.",
                        Recommendation = "Separe em Command/CommandHandler e Query/QueryHandler. Deixe a orquestração explícita e com responsabilidade única.",
                        Evidence = "Foram detectadas chamadas de leitura e escrita no mesmo método."
                    }.ToFinding());
                }
            }
        }

        return findings;
    }
}

public sealed class DirectInfrastructureUsageRule : IRule
{
    public string RuleId => "TID005";
    public string Title => "Uso direto de infraestrutura em camada indevida";
    public string Category => "Architecture";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees)
        {
            var path = tree.SourceFile.RelativePath;
            var isApplicationLike = Helpers.PathContainsAny(path, options.ApplicationPathHints) || path.Contains("Controller", StringComparison.OrdinalIgnoreCase);
            var isInfrastructureLike = Helpers.PathContainsAny(path, options.InfrastructurePathHints);
            if (!isApplicationLike || isInfrastructureLike)
            {
                continue;
            }

            var typeUsages = tree.Root.DescendantNodes().OfType<IdentifierNameSyntax>();
            var offendingNode = typeUsages.FirstOrDefault(id => options.DirectInfrastructureTypeTokens.Contains(id.Identifier.Text, StringComparer.OrdinalIgnoreCase));
            if (offendingNode is null)
            {
                continue;
            }

            var span = Helpers.GetLineSpan(offendingNode);
            findings.Add(new RuleFindingDraft
            {
                RuleId = RuleId,
                Title = Title,
                Category = Category,
                FilePath = path,
                SymbolName = offendingNode.Identifier.Text,
                StartLine = span.startLine,
                EndLine = span.endLine,
                Severity = FindingSeverity.High,
                Description = $"Foi detectado uso direto de '{offendingNode.Identifier.Text}' em uma área que parece Application/Service/Controller.",
                Recommendation = "Introduza abstração de repositório, query service, command gateway ou porta de persistência e mantenha detalhes em Infrastructure.",
                Evidence = offendingNode.Identifier.Text
            }.ToFinding());
        }

        return findings;
    }
}

public sealed class ControllerBusinessLogicRule : IRule
{
    public string RuleId => "TID006";
    public string Title => "Controller com regra de negócio";
    public string Category => "Web";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees.Where(t => t.SourceFile.RelativePath.Contains("Controller", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var method in tree.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var hints = Helpers.CountComplexityHints(method.Body);
                var hasDirectInfra = method.DescendantNodes().OfType<IdentifierNameSyntax>().Any(x => options.DirectInfrastructureTypeTokens.Contains(x.Identifier.Text, StringComparer.OrdinalIgnoreCase));
                if (hints < options.MaxControllerComplexityHints && !hasDirectInfra)
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(method);
                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    SymbolName = method.Identifier.Text,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = hasDirectInfra ? FindingSeverity.High : FindingSeverity.Medium,
                    Description = $"O método '{method.Identifier.Text}' no controller parece concentrar lógica além da coordenação HTTP.",
                    Recommendation = "Mantenha o controller fino. Extraia regras para handler, application service, domain service ou pipeline específico.",
                    Evidence = $"Hints de complexidade: {hints}. Infra direta: {hasDirectInfra}."
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class DuplicateMethodBodyRule : IRule
{
    public string RuleId => "TID007";
    public string Title => "Duplicação aproximada de corpo de método";
    public string Category => "Maintainability";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();
        var map = new Dictionary<string, (SyntaxTreeContext Tree, MethodDeclarationSyntax Method, int Lines)>();
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tree in context.SyntaxTrees)
        {
            foreach (var method in tree.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var body = method.Body?.ToString();
                if (string.IsNullOrWhiteSpace(body))
                {
                    continue;
                }

                var lineCount = Helpers.CountNonEmptyLines(body.Split(Environment.NewLine));
                if (lineCount < options.DuplicateMethodMinLines)
                {
                    continue;
                }

                var normalized = Helpers.NormalizeCode(body);
                if (!map.TryGetValue(normalized, out var first))
                {
                    map[normalized] = (tree, method, lineCount);
                    continue;
                }

                var key = string.Join("|", new[]
                {
                    first.Tree.SourceFile.RelativePath,
                    first.Method.Identifier.Text,
                    tree.SourceFile.RelativePath,
                    method.Identifier.Text
                }.OrderBy(x => x));

                if (!emitted.Add(key))
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(method);
                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    SymbolName = method.Identifier.Text,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = FindingSeverity.Medium,
                    Description = $"O corpo de '{method.Identifier.Text}' parece duplicado em relação a '{first.Method.Identifier.Text}'.",
                    Recommendation = "Extraia o comportamento comum para método compartilhado, serviço, strategy, specification ou componente reutilizável.",
                    Evidence = $"Duplicado com {first.Tree.SourceFile.RelativePath}:{first.Method.Identifier.Text}"
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class DbCallInsideLoopRule : IRule
{
    public string RuleId => "TID008";
    public string Title => "Chamada de banco dentro de loop";
    public string Category => "Performance";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();
        var suspiciousTokens = new[] { "Query", "Execute", "SaveChanges", "OpenAsync", "BeginTransaction", "Commit", "Rollback" };

        foreach (var tree in context.SyntaxTrees)
        {
            var loops = tree.Root.DescendantNodes().Where(n => n is ForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax);
            foreach (var loop in loops)
            {
                var identifiers = loop.DescendantNodes().OfType<IdentifierNameSyntax>().Select(x => x.Identifier.Text).ToList();
                if (!identifiers.Any(id => suspiciousTokens.Any(t => id.Contains(t, StringComparison.OrdinalIgnoreCase))))
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(loop);
                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = FindingSeverity.High,
                    Description = "Foi detectado indício de acesso a banco ou persistência dentro de um loop.",
                    Recommendation = "Avalie batching, pré-carga, bulk operations, composição em memória ou reescrita da consulta para evitar N+1 e excesso de roundtrips.",
                    Evidence = Helpers.TrimSnippet(loop.ToString())
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class SyncOverAsyncRule : IRule
{
    public string RuleId => "TID009";
    public string Title => "Uso de APIs síncronas em fluxo assíncrono";
    public string Category => "Performance";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees)
        {
            foreach (var method in tree.Root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Modifiers.Any(SyntaxKind.AsyncKeyword)))
            {
                var body = method.Body?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(body))
                {
                    continue;
                }

                var hasBlocking = body.Contains(".Result", StringComparison.Ordinal) || body.Contains(".Wait(", StringComparison.Ordinal) || body.Contains("Task.Run(", StringComparison.Ordinal);
                if (!hasBlocking)
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(method);
                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    SymbolName = method.Identifier.Text,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = FindingSeverity.Medium,
                    Description = $"O método assíncrono '{method.Identifier.Text}' usa padrão bloqueante como '.Result' ou '.Wait()'.",
                    Recommendation = "Propague async/await até a borda do fluxo e elimine esperas bloqueantes para reduzir risco de deadlock e contenção.",
                    Evidence = Helpers.TrimSnippet(method.ToString())
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class GenericExceptionRule : IRule
{
    public string RuleId => "TID010";
    public string Title => "Lançamento de Exception genérica";
    public string Category => "Robustness";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees)
        {
            foreach (var throwNode in tree.Root.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                if (throwNode.Expression is not ObjectCreationExpressionSyntax objectCreation)
                {
                    continue;
                }

                var typeName = objectCreation.Type.ToString();
                if (!typeName.Equals("Exception", StringComparison.Ordinal))
                {
                    continue;
                }

                var span = Helpers.GetLineSpan(throwNode);
                findings.Add(new RuleFindingDraft
                {
                    RuleId = RuleId,
                    Title = Title,
                    Category = Category,
                    FilePath = tree.SourceFile.RelativePath,
                    StartLine = span.startLine,
                    EndLine = span.endLine,
                    Severity = FindingSeverity.Low,
                    Description = "Foi encontrado lançamento de Exception genérica.",
                    Recommendation = "Crie exceções específicas de domínio, aplicação ou infraestrutura, preservando intenção semântica e tratamento mais preciso.",
                    Evidence = Helpers.TrimSnippet(throwNode.ToString())
                }.ToFinding());
            }
        }

        return findings;
    }
}

public sealed class SingleResponsibilityRule : IRule
{
    public string RuleId => "TID011";
    public string Title => "Violação do Princípio da Responsabilidade Única (SRP)";
    public string Category => "Design";

    public IReadOnlyList<RuleFinding> Evaluate(ProjectContext context, AnalysisOptions options)
    {
        var findings = new List<RuleFinding>();

        foreach (var tree in context.SyntaxTrees)
        {
            foreach (var type in tree.Root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var publicMethods = type.Members.OfType<MethodDeclarationSyntax>().Count(m => m.Modifiers.Any(SyntaxKind.PublicKeyword));
                var privateFields = type.Members.OfType<FieldDeclarationSyntax>().Count(f => f.Modifiers.Any(SyntaxKind.PrivateKeyword));
                var properties = type.Members.OfType<PropertyDeclarationSyntax>().Count();

                if (publicMethods > 8 || privateFields > 8 || (publicMethods + properties) > 12)
                {
                    var span = Helpers.GetLineSpan(type);
                    findings.Add(new RuleFindingDraft
                    {
                        RuleId = RuleId,
                        Title = Title,
                        Category = Category,
                        FilePath = tree.SourceFile.RelativePath,
                        SymbolName = type.Identifier.Text,
                        StartLine = span.startLine,
                        EndLine = span.endLine,
                        Severity = FindingSeverity.Medium,
                        Description = $"A classe '{type.Identifier.Text}' possui muitos métodos públicos ({publicMethods}) e/ou campos privados ({privateFields}), sugerindo múltiplas responsabilidades.",
                        Recommendation = "Quebre a classe em componentes menores, cada um com uma responsabilidade clara e única.",
                        Evidence = $"Métodos públicos: {publicMethods}, Campos privados: {privateFields}, Propriedades: {properties}"
                    }.ToFinding());
                }
            }
        }

        return findings;
    }
}
