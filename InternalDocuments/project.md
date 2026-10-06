# Project 

This is a .net analyzer that flags the use of the greater than sign (>) in code.
It should also have a auto fixer that replaces the greater than sign with a less than sign (<) and flips values so it's a pure refactoring.
this also includes <=
if multiple numbers are involved it should also order from smaller to larger.
So 
5 > x && x > 2 

should become 

2 < x && x < 5


# Decisions

1. Rule ID: `LessThanOnly` (message: `Use '<' instead of '>'`).
1. Default severity: warning. Users can raise it to error in `.editorconfig`.
1. Flagged operators: only the comparison operators `>` and `>=`. Ignored: `>>`, `>>=`, `=>`, and generic angle brackets.
1. Fix: `a > b` becomes `b < a`, and `a >= b` becomes `b <= a`.
1. Reordering: only for an `&&` chain where every clause compares the same variable against a constant (as in the example above). Clauses are ordered by constant, smallest first.
1. Anything else is only flipped and keeps its original order, because reordering can change short-circuit behavior and side effects.
1. Target framework: `netstandard2.0` (required for Roslyn analyzers).
1. Package ID: `DoNotUseTheGreaterThanSign.Analyzer.Net`.


# Project Structure

Scripts: build_and_test.sh
Unit tests: use xunit
CI: Github actions
    build and test (uses the script) on push
    publish, on github deploy, version from tag (example tag: v1.2.3)
Nuget: publishes the analyzer package to nuget.org on GitHub release, with the version taken from the tag (v1.2.3 becomes 1.2.3). Uses nuget.org trusted publishing (OIDC, no long-lived API key): the `publish.yml` job has `id-token: write`, and `NuGet/login@v1` exchanges the GitHub token for a short-lived key using the `NUGET_USER` repo secret (the nuget.org profile name, not the email). The trusted publishing policy on nuget.org must name `publish.yml` as the workflow file.
