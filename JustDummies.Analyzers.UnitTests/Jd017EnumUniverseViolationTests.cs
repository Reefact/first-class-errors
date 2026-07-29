using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd017EnumUniverseViolationTests {

    private const string Declarations = """
        using System;

        [Flags]
        public enum Perm { None = 0, Read = 1, Write = 2 }

        public enum Day { Mon, Tue }
        """;

    [Fact]
    public async Task Reports_a_flag_combination_without_AllowingCombinations() {
        // The natural thing to write on a [Flags] enum, and the generator refuses it.
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().OneOf(Perm.Read | Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD017");
        Check.That(diagnostics[0].GetMessage()).Contains("AllowingCombinations()");
    }

    [Fact]
    public async Task Reports_an_undeclared_numeric_value() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Day>().OneOf((Day)99);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("not a declared member of Day");
    }

    [Fact]
    public async Task Reports_an_exclusion_that_removes_every_member() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Day>().Except(Day.Mon, Day.Tue);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("no declared Day member remains");
    }

    [Fact]
    public async Task Does_not_report_a_combination_once_AllowingCombinations_is_declared() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().AllowingCombinations().OneOf(Perm.Read | Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_declared_members() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Perm>().OneOf(Perm.Read, Perm.Write);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_partial_exclusion() {
        string source = $$"""
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<Day>().Except(Day.Mon);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_unsubstituted_type_parameter() {
        // A generic helper gives no enum to reason about; the rule must bail rather than guess.
        string source = $$"""
            using System;
            using JustDummies;
            {{Declarations}}

            public static class Sample {
                public static IAny<T> AnyOf<T>() where T : struct, Enum => Any.Enum<T>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_conflict_asserting_negative_test() {
        string source = $$"""
            using System;
            using JustDummies;
            {{Declarations}}

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Any.Enum<Day>().Except(Day.Mon, Day.Tue));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
