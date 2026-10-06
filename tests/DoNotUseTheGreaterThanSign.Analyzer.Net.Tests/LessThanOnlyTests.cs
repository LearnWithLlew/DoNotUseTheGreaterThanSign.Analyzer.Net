using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DoNotUseTheGreaterThanSign.Analyzer.LessThanOnlyAnalyzer,
    DoNotUseTheGreaterThanSign.Analyzer.LessThanOnlyCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace DoNotUseTheGreaterThanSign.Analyzer.Tests;

public class LessThanOnlyTests
{
    private static string Wrap(string expression) => $@"
class C
{{
    bool M(int x, int y, int z) => {expression};
}}";

    // 'index' is where the operator starts inside the expression.
    private static DiagnosticResult Flag(string expression, int index, string token, string replacement)
    {
        var line = Wrap(expression).Split('\n')[3];
        var column = line.IndexOf("=> ") + 3 + index + 1;
        return Verify.Diagnostic("LessThanOnly").WithSpan(4, column, 4, column + token.Length).WithArguments(replacement, token);
    }

    [Fact]
    public async Task LessThanIsNotFlagged() =>
        await Verify.VerifyAnalyzerAsync(Wrap("x < y"));

    [Fact]
    public async Task ShiftAndLambdaAreNotFlagged() =>
        await Verify.VerifyAnalyzerAsync(@"
using System;
class C
{
    int M(int x) { Func<int, int> f = a => a >> 1; return x >> 2; }
}");

    [Fact]
    public async Task GreaterThanIsFlippedAndFlagged() =>
        await Verify.VerifyCodeFixAsync(Wrap("x > y"), Flag("x > y", 2, ">", "<"), Wrap("y < x"));

    [Fact]
    public async Task GreaterThanOrEqualIsFlippedAndFlagged() =>
        await Verify.VerifyCodeFixAsync(Wrap("x >= y"), Flag("x >= y", 2, ">=", "<="), Wrap("y <= x"));

    [Fact]
    public async Task ChainIsFlippedAndOrderedSmallestToLargest() =>
        await Verify.VerifyCodeFixAsync(
            Wrap("5 > x && x > 2"),
            new[] { Flag("5 > x && x > 2", 2, ">", "<"), Flag("5 > x && x > 2", 11, ">", "<") },
            Wrap("2 < x && x < 5"));

    [Fact]
    public async Task ChainWithDifferentVariablesIsOnlyFlipped() =>
        await Verify.VerifyCodeFixAsync(
            Wrap("y < 3 && x > 2"),
            Flag("y < 3 && x > 2", 11, ">", "<"),
            Wrap("y < 3 && 2 < x"));

    [Fact]
    public async Task ChainWithMethodCallIsNotReordered() =>
        await Verify.VerifyCodeFixAsync(
            @"
class C
{
    int F() => 1;
    bool M(int x) => x > 5 && F() > 2;
}",
            new[]
            {
                Verify.Diagnostic("LessThanOnly").WithSpan(5, 24, 5, 25).WithArguments("<", ">"),
                Verify.Diagnostic("LessThanOnly").WithSpan(5, 35, 5, 36).WithArguments("<", ">"),
            },
            @"
class C
{
    int F() => 1;
    bool M(int x) => 5 < x && 2 < F();
}");
}
