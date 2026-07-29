using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd005GeneratorRenderedAsTextTests {

    [Fact]
    public async Task Reports_a_generator_in_an_interpolation_hole() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return $"order {Any.String().NonEmpty()}";
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD005");
        Check.That(diagnostics[0].GetMessage()).Contains("Generate()");
        Check.That(diagnostics[0].GetMessage()).Contains("AnyString");
    }

    [Fact]
    public async Task Reports_a_generator_concatenated_to_a_string() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return "order " + Any.String().NonEmpty();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD005");
    }

    [Fact]
    public async Task Reports_an_explicit_ToString_on_a_generator() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.Int32().Positive().ToString();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD005");
        Check.That(diagnostics[0].GetMessage()).Contains("AnyInt32");
    }

    [Fact]
    public async Task Reports_a_generator_held_behind_the_IAny_interface() {
        // The rule keys on IAny<T>, not on a list of concrete builders, so As(...) and Combine(...) results are covered.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    IAny<string> generator = Any.String().NonEmpty();

                    return $"{generator}";
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD005");
    }

    [Fact]
    public async Task Does_not_report_a_generated_value() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return $"order {Any.String().NonEmpty().Generate()}"
                         + Any.Int32().Positive().Generate().ToString();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_ordinary_value() {
        const string source = """
            using System;

            public static class Sample {
                public static string M() {
                    int value = 42;

                    return $"n {value}" + value + value.ToString() + DateTime.UtcNow.ToString();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_ToString_overload_that_takes_a_format() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.Int32().Positive().Generate().ToString("D");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_numeric_addition() {
        // The concatenation branch must key on the string result type, not on the operator alone.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static int M() {
                    return Any.Int32().Positive().Generate() + 1;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorRenderedAsTextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
