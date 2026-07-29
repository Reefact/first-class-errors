using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd003AwaitableBodyPassedToReproduciblyTests {

    [Fact]
    public async Task Reports_an_expression_bodied_lambda_over_an_awaitable() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                private static Task SaveAsync() => Task.CompletedTask;

                public static void M() {
                    Any.Reproducibly(() => SaveAsync());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD003");
        Check.That(diagnostics[0].GetMessage()).Contains("ReproduciblyAsync");
    }

    [Fact]
    public async Task Reports_a_block_bodied_lambda_whose_single_statement_is_an_awaitable() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                private static Task SaveAsync() => Task.CompletedTask;

                public static void M() {
                    Any.Reproducibly(() => { SaveAsync(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD003");
    }

    [Fact]
    public async Task Reports_an_async_void_method_group() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                private static async void Body() { await Task.Yield(); }

                public static void M() {
                    Any.Reproducibly(Body);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD003");
    }

    [Fact]
    public async Task Reports_the_seeded_overload_too() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                private static Task SaveAsync() => Task.CompletedTask;

                public static void M() {
                    Any.Reproducibly(1234, () => SaveAsync());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD003");
    }

    [Fact]
    public async Task Does_not_report_a_synchronous_body() {
        const string source = """
            using JustDummies;

            public static class Sample {
                private static void Save() { }

                public static void M() {
                    Any.Reproducibly(() => Save());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_async_lambda_which_is_JD001s_case() {
        // JD001 owns the async-lambda shape. JD003 must not double-report it, or one mistake would raise two errors.
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static void M() {
                    Any.Reproducibly(async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_awaited_ReproduciblyAsync() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                private static Task SaveAsync() => Task.CompletedTask;

                public static async Task M() {
                    await Any.ReproduciblyAsync(() => SaveAsync());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_Reproducibly_that_is_not_Any() {
        const string source = """
            using System;
            using System.Threading.Tasks;

            public static class Other {
                public static void Reproducibly(Action body) { }
            }

            public static class Sample {
                private static Task SaveAsync() => Task.CompletedTask;

                public static void M() {
                    Other.Reproducibly(() => SaveAsync());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AwaitableBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
