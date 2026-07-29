using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd013HeldCollectionPassedToOneOfTests {

    [Fact]
    public async Task Reports_a_held_list_passed_to_OneOf() {
        const string source = """
            using System.Collections.Generic;
            using JustDummies;

            public static class Sample {
                public static void M(List<string> references) {
                    IAny<List<string>> pool = Any.OneOf(references);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new HeldCollectionPassedToOneOfAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD013");
        Check.That(diagnostics[0].GetMessage()).Contains("ElementOf");
    }

    [Fact]
    public async Task Does_not_report_an_explicit_type_argument() {
        // Any.OneOf<List<string>>(references) states the intent: a pool whose single element is that collection.
        const string source = """
            using System.Collections.Generic;
            using JustDummies;

            public static class Sample {
                public static void M(List<string> references) {
                    IAny<List<string>> pool = Any.OneOf<List<string>>(references);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new HeldCollectionPassedToOneOfAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_single_string() {
        // A string is IEnumerable<char>; a one-string pool is ordinary and must not be flagged.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IAny<string> pool = Any.OneOf("EUR");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new HeldCollectionPassedToOneOfAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_several_values() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IAny<string> pool = Any.OneOf("EUR", "USD");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new HeldCollectionPassedToOneOfAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_ElementOf() {
        const string source = """
            using System.Collections.Generic;
            using JustDummies;

            public static class Sample {
                public static void M(List<string> references) {
                    IAny<string> pool = Any.ElementOf(references);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new HeldCollectionPassedToOneOfAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_array_which_binds_to_the_element_type() {
        // An array satisfies params directly, so T is inferred as the element type — the call is already correct.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(string[] references) {
                    IAny<string> pool = Any.OneOf(references);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new HeldCollectionPassedToOneOfAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
