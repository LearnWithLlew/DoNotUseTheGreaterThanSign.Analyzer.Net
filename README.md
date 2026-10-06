# DoNotUseTheGreaterThanSign.Analyzer.Net

![icon](./icon.png)

[![build](https://github.com/LearnWithLlew/DoNotUseTheGreaterThanSign.Analyzer.Net/actions/workflows/build.yml/badge.svg)](https://github.com/LearnWithLlew/DoNotUseTheGreaterThanSign.Analyzer.Net/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/DoNotUseTheGreaterThanSign.Analyzer.Net.svg)](https://www.nuget.org/packages/DoNotUseTheGreaterThanSign.Analyzer.Net)

A Roslyn analyzer that flags `>` and `>=` (rule `LessThanOnly`, warning by default) and a code fix that rewrites them as `<` and `<=` by swapping the operands.

```csharp
a > b          // becomes  b < a
a >= b         // becomes  b <= a
5 > x && x > 2 // becomes  2 < x && x < 5
```

When an `&&` chain compares one variable against constants, the clauses are also ordered from the smallest constant to the largest. Other code is only flipped, never reordered.

Install: `dotnet add package DoNotUseTheGreaterThanSign.Analyzer.Net`
