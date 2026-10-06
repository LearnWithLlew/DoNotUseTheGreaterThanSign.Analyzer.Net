using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DoNotUseTheGreaterThanSign.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LessThanOnlyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LessThanOnly";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Use '<' instead of '>'",
        messageFormat: "Use '{0}' instead of '{1}'",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Comparisons should be written with '<' or '<=' only. Swap the operands to get the same result.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanOrEqualExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var binary = (BinaryExpressionSyntax)context.Node;
        var found = binary.OperatorToken.Text;
        var replacement = binary.IsKind(SyntaxKind.GreaterThanExpression) ? "<" : "<=";
        context.ReportDiagnostic(Diagnostic.Create(Rule, binary.OperatorToken.GetLocation(), replacement, found));
    }
}
