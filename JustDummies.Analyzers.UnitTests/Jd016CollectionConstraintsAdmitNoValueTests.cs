using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd016CollectionConstraintsAdmitNoValueTests {

    private const string EnumDeclarations = """
        public enum Day { Mon, Tue }
        """;

    [Theory]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(0).NonEmpty()")]
    [InlineData("Any.ListOf(Any.Int32()).NonEmpty().WithCount(0)")]
    [InlineData("Any.ListOf(Any.Int32()).Empty().NonEmpty()")]
    [InlineData("Any.ListOf(Any.Int32()).WithMinCount(5).WithMaxCount(2)")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(2).WithMinCount(5)")]
    public async Task Reports_counts_that_cannot_all_hold(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD016");
    }

    [Fact]
    public async Task Reports_more_contained_elements_than_the_cap_allows() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.ListOf(Any.Int32()).WithMaxCount(2).Containing(1).Containing(2).Containing(3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("cannot fit");
    }

    [Fact]
    public async Task Reports_a_set_asking_for_more_than_boolean_can_give() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.SetOf(Any.Boolean()).WithCount(5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("only 2");
    }

    [Fact]
    public async Task Reports_a_distinct_list_exceeding_the_enum_member_count() {
        string source = $$"""
            using JustDummies;

            {{EnumDeclarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.ListOf(Any.Enum<Day>()).Distinct().WithCount(10);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("only 2");
    }

    [Fact]
    public async Task Reports_a_distinct_list_exceeding_an_explicit_pool() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.SetOf(Any.OneOf("a", "b")).WithCount(3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Theory]
    [InlineData("Any.ListOf(Any.Int32()).WithCountBetween(2, 10)")]
    [InlineData("Any.ListOf(Any.Int32()).NonEmpty().WithMaxCount(5)")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(3).Containing(1).Containing(2)")]
    [InlineData("Any.SetOf(Any.Boolean()).WithCount(2)")]
    [InlineData("Any.SetOf(Any.Int32()).WithCount(500)")]
    [InlineData("Any.ListOf(Any.Int32()).WithCount(500)")]
    public async Task Does_not_report_a_satisfiable_chain(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_distinct_list_over_a_small_domain() {
        // Without Distinct, repeats are fine: ten booleans in a list is ordinary.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.ListOf(Any.Boolean()).WithCount(10);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

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
                    Check2.ThatCode(() => Any.SetOf(Any.Boolean()).WithCount(5));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_widened_flag_enum_domain() {
        // AllowingCombinations widens the universe to the OR-closure of the declared members — eight values for four
        // flags — so counting declared members would condemn a legal chain. Live in
        // JustDummies.UnitTests/AnyEnumCombinationTests.cs, which asserts WithCount(8) succeeds.
        const string source = """
            using System;
            using JustDummies;

            [Flags]
            public enum Perm { None = 0, Read = 1, Write = 2, Execute = 4 }

            public static class Sample {
                public static void M() {
                    _ = Any.SetOf(Any.Enum<Perm>().AllowingCombinations()).WithCount(8);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Still_reports_a_flag_enum_without_the_widening() {
        const string source = """
            using System;
            using JustDummies;

            [Flags]
            public enum Perm { None = 0, Read = 1, Write = 2, Execute = 4 }

            public static class Sample {
                public static void M() {
                    _ = Any.SetOf(Any.Enum<Perm>()).WithCount(5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("only 4");
    }

    [Fact]
    public async Task Does_not_report_an_element_generator_whose_domain_is_unprovable() {
        // An unprovable domain must never be treated as a small one.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(IAny<int> elements) {
                    _ = Any.SetOf(elements).WithCount(1000);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CollectionConstraintsAdmitNoValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
