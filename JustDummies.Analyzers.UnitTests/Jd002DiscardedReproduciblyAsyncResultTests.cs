using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd002DiscardedReproduciblyAsyncResultTests {

    [Fact]
    public async Task Reports_when_the_task_is_discarded_as_a_statement() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static void M() {
                    Any.ReproduciblyAsync(async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD002");
        Check.That(diagnostics[0].GetMessage()).Contains("ReproduciblyAsync");
    }

    [Fact]
    public async Task Reports_when_the_task_is_assigned_to_a_discard() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static void M() {
                    _ = Any.ReproduciblyAsync(async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD002");
    }

    [Fact]
    public async Task Does_not_report_a_ReproduciblyAsync_that_is_not_Any() {
        // A same-named method on another type must not trip the rule — the analyzer keys on JustDummies.Any.
        const string source = """
            using System;
            using System.Threading.Tasks;

            public static class Other {
                public static Task ReproduciblyAsync(Func<Task> body) => Task.CompletedTask;
            }

            public static class Sample {
                public static void M() {
                    Other.ReproduciblyAsync(async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_the_task_is_awaited() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static async Task M() {
                    await Any.ReproduciblyAsync(async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_the_task_is_captured() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static Task M() {
                    Task task = Any.ReproduciblyAsync(async () => { await Task.Yield(); });

                    return task;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
