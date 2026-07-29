using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd006DiscardedGeneratorResultTests {

    [Fact]
    public async Task Reports_a_constraint_whose_result_is_dropped() {
        const string source = """
            using System.Collections.Generic;
            using JustDummies;

            public static class Sample {
                public static List<int> M() {
                    AnyList<int> numbers = Any.ListOf(Any.Int32());
                    numbers.NonEmpty();

                    return numbers.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD006");
        Check.That(diagnostics[0].GetMessage()).Contains("NonEmpty");
        Check.That(diagnostics[0].GetMessage()).Contains("immutable recipe");
    }

    [Fact]
    public async Task Reports_a_whole_chain_left_as_a_statement() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.String().NonEmpty();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD006");
    }

    [Fact]
    public async Task Does_not_report_an_explicit_discard() {
        // The rule exists because the mistake is silent — `numbers.NonEmpty();` reads as if it mutated the receiver.
        // An explicit discard cannot be misread that way, and it is how a test that only wants the construction to
        // throw spells its intent. JD002 and JD004 do report `_ =`, because discarding is never right there.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static bool M(string pattern) {
                    try {
                        _ = Any.StringMatching(pattern);

                        return false;
                    } catch (UnsupportedRegexException) {
                        return true;
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_constraint_assigned_back() {
        const string source = """
            using System.Collections.Generic;
            using JustDummies;

            public static class Sample {
                public static List<int> M() {
                    AnyList<int> numbers = Any.ListOf(Any.Int32());
                    numbers = numbers.NonEmpty();

                    return numbers.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_discarded_generated_value() {
        // Generate() returns the value, not a recipe; a dropped value is a different (and much weaker) smell.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_conflict_asserting_negative_test() {
        // The illegal chain is the whole body of a lambda argument — the shape every throws-assertion uses. This
        // repository's own suite writes hundreds of them.
        const string source = """
            using System;
            using JustDummies;

            public static class Assertions {
                public static void Throws(Action code) { }
            }

            public static class Sample {
                public static void M() {
                    Assertions.Throws(() => Any.String().WithLength(3).StartingWith("ORD-"));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_arrange_code_inside_a_block_bodied_lambda() {
        // The negative-test guard must stay narrow: inside Any.Reproducibly's body the dropped constraint is a real
        // defect, because the call is one statement of a block rather than the lambda's whole body.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.Reproducibly(() => {
                        AnyString reference = Any.String();
                        reference.NonEmpty();
                    });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD006");
    }

    [Fact]
    public async Task Does_not_report_when_JustDummies_is_absent_from_the_compilation() {
        const string source = """
            public static class Other {
                public static Other NonEmpty() => new();
            }

            public static class Sample {
                public static void M() {
                    Other.NonEmpty();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedGeneratorResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
