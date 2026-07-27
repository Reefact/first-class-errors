using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd001AsyncBodyPassedToReproduciblyTests {

    [Fact]
    public async Task Reports_an_async_lambda_passed_to_Reproducibly() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static void M() {
                    Any.Reproducibly(async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AsyncBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD001");
    }

    [Fact]
    public async Task Reports_an_async_lambda_passed_to_the_seeded_Reproducibly() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static void M() {
                    Any.Reproducibly(42, async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AsyncBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD001");
    }

    [Fact]
    public async Task Does_not_report_a_synchronous_lambda() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.Reproducibly(() => { var _ = Any.Int32().Generate(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AsyncBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_async_lambda_passed_to_ReproduciblyAsync() {
        const string source = """
            using JustDummies;
            using System.Threading.Tasks;

            public static class Sample {
                public static async Task M() {
                    await Any.ReproduciblyAsync(async () => { await Task.Yield(); });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new AsyncBodyPassedToReproduciblyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
