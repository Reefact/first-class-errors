using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd015StringConstraintsAdmitNoValueTests {

    [Fact]
    public async Task Reports_the_case_ADR_0035_names() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Numeric().StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD015");
        Check.That(diagnostics[0].GetMessage()).Contains("Numeric()");
    }

    [Fact]
    public async Task Does_not_report_the_sibling_ADR_0035_contrasts_it_with() {
        // Identical call site, identical static types — only the argument's value differs, and this one is legal.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().Numeric().StartingWith("123").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_letter_whose_case_the_chain_forbids() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().LowerCase().StartingWith("ABC").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("uppercase letter 'A'");
    }

    [Fact]
    public async Task Does_not_report_a_non_letter_under_a_casing_constraint() {
        // Verified against the library: casing constrains the case of a fragment's LETTERS and says nothing about its
        // other characters. This exact chain is asserted legal by JustDummies.UnitTests/AnyStringTests.cs.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().UpperCase().StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_length_budget_once_a_value_set_is_declared() {
        // With OneOf declared, the fragments are matched against the pooled values rather than laid out side by side,
        // so the budget no longer applies. Live in JustDummies.UnitTests/AnyStringValueSetTests.cs.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().OneOf("aba").WithMaxLength(3).Containing("ab").Containing("ba").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_the_constraint_in_either_declaration_order() {
        // StringSpec re-validates the whole spec on every mutation, so the order does not change the verdict.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().StartingWith("ORD-").Numeric().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Fact]
    public async Task Reports_fragments_that_cannot_fit_a_fixed_length() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithLength(3).StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("at least 4 characters");
    }

    [Fact]
    public async Task Reports_a_prefix_and_suffix_that_exceed_the_cap() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithMaxLength(6).StartingWith("SKU-").EndingWith("-EUR").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("WithMaxLength(6)");
    }

    [Fact]
    public async Task Does_not_report_a_budget_that_fits_exactly() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithLength(8).StartingWith("SKU-").EndingWith("-EUR").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_fragment_inside_an_explicit_pool() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    return Any.String().WithChars("ORD-0123456789").StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_constant_fragment() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M(string prefix) {
                    return Any.String().Numeric().StartingWith(prefix).Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_chain_split_across_statements() {
        // The chain must be one expression: following a generator through a local would need dataflow, and a rule
        // claiming a chain is unsatisfiable must see every constraint it carries.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    AnyString generator = Any.String().Numeric();

                    return generator.StartingWith("ORD-").Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_conflict_asserting_negative_test() {
        const string source = """
            using System;
            using JustDummies;

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Any.String().Numeric().StartingWith("ORD-"));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new StringConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
