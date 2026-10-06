using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DoNotUseTheGreaterThanSign.Analyzer;

internal static class ComparisonRewriter
{
    public static bool IsGreaterThan(SyntaxNode node) =>
        node.IsKind(SyntaxKind.GreaterThanExpression) || node.IsKind(SyntaxKind.GreaterThanOrEqualExpression);

    /// <summary>
    /// Rewrites the comparison, or the whole '&amp;&amp;' chain it belongs to when the chain
    /// compares one variable against constants, and returns the new root.
    /// </summary>
    public static SyntaxNode Rewrite(SyntaxNode root, BinaryExpressionSyntax comparison, SemanticModel model)
    {
        var chain = OutermostAndChain(comparison);
        if (chain != comparison)
        {
            var clauses = Flatten(chain);
            if (TryOrder(clauses, model, out var ordered))
            {
                return root.ReplaceNode(chain, Join(ordered).WithTriviaFrom(chain));
            }
        }

        return root.ReplaceNode(comparison, Flip(comparison));
    }

    private static BinaryExpressionSyntax Flip(BinaryExpressionSyntax comparison)
    {
        var lessThan = comparison.IsKind(SyntaxKind.GreaterThanExpression)
            ? SyntaxKind.LessThanExpression
            : SyntaxKind.LessThanOrEqualExpression;
        var token = comparison.IsKind(SyntaxKind.GreaterThanExpression)
            ? SyntaxKind.LessThanToken
            : SyntaxKind.LessThanEqualsToken;

        return SyntaxFactory.BinaryExpression(
                lessThan,
                comparison.Right.WithoutTrivia(),
                SyntaxFactory.Token(token).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                comparison.Left.WithoutTrivia())
            .WithTriviaFrom(comparison);
    }

    private static ExpressionSyntax OutermostAndChain(ExpressionSyntax node)
    {
        while (node.Parent is BinaryExpressionSyntax parent && parent.IsKind(SyntaxKind.LogicalAndExpression))
        {
            node = parent;
        }

        return node;
    }

    private static List<ExpressionSyntax> Flatten(ExpressionSyntax chain)
    {
        var clauses = new List<ExpressionSyntax>();
        void Visit(ExpressionSyntax e)
        {
            if (e is BinaryExpressionSyntax b && b.IsKind(SyntaxKind.LogicalAndExpression))
            {
                Visit(b.Left);
                Visit(b.Right);
            }
            else
            {
                clauses.Add(e);
            }
        }

        Visit(chain);
        return clauses;
    }

    // Every clause must compare the same plain variable against a numeric constant,
    // otherwise reordering could change short-circuit behavior.
    private static bool TryOrder(List<ExpressionSyntax> clauses, SemanticModel model, out List<ExpressionSyntax> ordered)
    {
        ordered = new List<ExpressionSyntax>();
        ExpressionSyntax? variable = null;
        var keyed = new List<(double Key, ExpressionSyntax Clause)>();

        foreach (var clause in clauses)
        {
            if (clause is not BinaryExpressionSyntax b || !IsComparison(b))
            {
                return false;
            }

            var leftConstant = ToDouble(model, b.Left);
            var rightConstant = ToDouble(model, b.Right);
            if (leftConstant.HasValue == rightConstant.HasValue)
            {
                return false;
            }

            var operand = leftConstant.HasValue ? b.Right : b.Left;
            if (!IsPlainVariable(operand) || (variable != null && !SyntaxFactory.AreEquivalent(variable, operand)))
            {
                return false;
            }

            variable = operand;
            var next = IsGreaterThan(b) ? Flip(b) : b;
            keyed.Add((leftConstant ?? rightConstant!.Value, next));
        }

        ordered = keyed.OrderBy(k => k.Key).Select(k => k.Clause).ToList();
        return true;
    }

    private static bool IsComparison(BinaryExpressionSyntax b) =>
        b.IsKind(SyntaxKind.LessThanExpression)
        || b.IsKind(SyntaxKind.LessThanOrEqualExpression)
        || b.IsKind(SyntaxKind.GreaterThanExpression)
        || b.IsKind(SyntaxKind.GreaterThanOrEqualExpression);

    private static bool IsPlainVariable(ExpressionSyntax e) => e switch
    {
        IdentifierNameSyntax => true,
        MemberAccessExpressionSyntax m => m.IsKind(SyntaxKind.SimpleMemberAccessExpression) && IsPlainVariable(m.Expression),
        ThisExpressionSyntax => true,
        _ => false,
    };

    private static double? ToDouble(SemanticModel model, ExpressionSyntax e)
    {
        var constant = model.GetConstantValue(e);
        if (!constant.HasValue || constant.Value is null || constant.Value is bool || constant.Value is string)
        {
            return null;
        }

        try
        {
            return Convert.ToDouble(constant.Value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
        {
            return null;
        }
    }

    private static ExpressionSyntax Join(List<ExpressionSyntax> clauses)
    {
        var result = clauses[0].WithoutTrivia();
        foreach (var clause in clauses.Skip(1))
        {
            result = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                result,
                SyntaxFactory.Token(SyntaxKind.AmpersandAmpersandToken)
                    .WithLeadingTrivia(SyntaxFactory.Space)
                    .WithTrailingTrivia(SyntaxFactory.Space),
                clause.WithoutTrivia());
        }

        return result;
    }
}
