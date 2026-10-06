using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DoNotUseTheGreaterThanSign.Analyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LessThanOnlyCodeFixProvider)), Shared]
public sealed class LessThanOnlyCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use '<' instead of '>'";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(LessThanOnlyAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(async (context, document, _) => await FixAll(document, context.CancellationToken).ConfigureAwait(false));

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var comparison = root?.FindToken(context.Span.Start).Parent?.AncestorsAndSelf()
            .OfType<BinaryExpressionSyntax>()
            .FirstOrDefault(ComparisonRewriter.IsGreaterThan);
        if (comparison is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                ct => FixOne(context.Document, comparison.SpanStart, ct),
                equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> FixOne(Document document, int position, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return document;
        }

        var comparison = root.FindToken(position).Parent!.AncestorsAndSelf()
            .OfType<BinaryExpressionSyntax>()
            .First(ComparisonRewriter.IsGreaterThan);
        return document.WithSyntaxRoot(ComparisonRewriter.Rewrite(root, comparison, model));
    }

    // A chain rewrite can replace several diagnostics at once, so fix one at a time and re-analyze.
    private static async Task<Document> FixAll(Document document, CancellationToken ct)
    {
        while (true)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
            if (root is null || model is null)
            {
                return document;
            }

            var comparison = root.DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .FirstOrDefault(ComparisonRewriter.IsGreaterThan);
            if (comparison is null)
            {
                return document;
            }

            document = document.WithSyntaxRoot(ComparisonRewriter.Rewrite(root, comparison, model));
        }
    }
}
