using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd009DrawInStaticInitializerTests {

    [Fact]
    public async Task Reports_a_draw_in_a_static_field_initializer() {
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly string Reference = Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD009");
        Check.That(diagnostics[0].GetMessage()).Contains("once for the whole suite");
    }

    [Fact]
    public async Task Reports_a_draw_in_a_static_constructor() {
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly string Reference;

                static Sample() {
                    Reference = Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD009");
    }

    [Fact]
    public async Task Does_not_report_a_static_field_holding_the_generator() {
        // The compliant shape: the recipe is shared, the draw happens per read. RandomSource resolves the source at
        // Generate() time, so a shared generator is safe — only a shared value is not.
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly IAny<string> Reference = Any.String().NonEmpty();

                public static string Next() => Reference.Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_an_instance_initializer() {
        const string source = """
            using JustDummies;

            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_an_ordinary_method() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string Next() => Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
