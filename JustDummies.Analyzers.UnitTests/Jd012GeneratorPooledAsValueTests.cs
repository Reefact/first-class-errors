using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd012GeneratorPooledAsValueTests {

    [Fact]
    public async Task Reports_a_pool_of_generators() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IAny<AnyInt32> pool = Any.OneOf(Any.Int32().Positive(), Any.Int32().Negative());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD012");
        Check.That(diagnostics[0].GetMessage()).Contains("Generate()");
    }

    [Fact]
    public async Task Reports_a_pool_of_generators_on_a_seeded_context() {
        // OneOf and ElementOf are mirrored on AnyContext; a rule keyed on Any alone would miss half the surface.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    AnyContext context = Any.WithSeed(1234);
                    IAny<AnyInt32> pool = context.OneOf(context.Int32(), context.Int32());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD012");
    }

    [Fact]
    public async Task Does_not_report_a_pool_of_values() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IAny<int> pool = Any.OneOf(1, 2, 3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_pool_of_generated_values() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IAny<int> pool = Any.OneOf(Any.Int32().Positive().Generate(), Any.Int32().Negative().Generate());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_another_types_OneOf() {
        const string source = """
            using JustDummies;

            public static class Other {
                public static T OneOf<T>(params T[] values) => values[0];
            }

            public static class Sample {
                public static void M() {
                    AnyInt32 chosen = Other.OneOf(Any.Int32(), Any.Int32());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
