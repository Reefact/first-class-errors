using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd011GeneratorWhereValueExpectedTests {

    [Fact]
    public async Task Reports_a_generator_bound_to_an_object_parameter() {
        // The shape that matters: an assertion helper taking object inspects the recipe, not the value.
        const string source = """
            using JustDummies;

            public static class Assert {
                public static void NotNull(object value) { }
            }

            public static class Sample {
                public static void M() {
                    Assert.NotNull(Any.String().NonEmpty());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD011");
        Check.That(diagnostics[0].GetMessage()).Contains("Generate()");
    }

    [Fact]
    public async Task Reports_a_generator_in_an_object_array_row() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static object[] Row() {
                    return new object[] { Any.Int32().Positive(), 1 };
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD011");
    }

    [Fact]
    public async Task Reports_a_generator_assigned_to_an_object_local() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static object M() {
                    object boxed = Any.Int32().Positive();

                    return boxed;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD011");
    }

    [Fact]
    public async Task Reports_Equals_against_a_value() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static bool M(string expected) {
                    return Any.String().NonEmpty().Equals(expected);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD011");
    }

    [Fact]
    public async Task Does_not_report_ReferenceEquals_between_two_generators() {
        // How an immutability test proves a constraint returned a new generator. Generate() would destroy it.
        // This shape is live in JustDummies.PropertyTests/ScalarIntervalProperties.cs.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static bool M() {
                    AnyInt32 original = Any.Int32().Between(1, 10);
                    AnyInt32 narrowed = original.GreaterThanOrEqualTo(10);

                    return !ReferenceEquals(original, narrowed);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_Equals_between_two_generators() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static bool M() {
                    AnyInt32 first  = Any.Int32();
                    AnyInt32 second = Any.Int32();

                    return first.Equals(second);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_throws_assertion_binding_to_Func_of_object() {
        // Assert.Throws<T>(() => chain) binds to Func<object>, producing a real generator-to-object conversion at
        // 88+ existing sites in this repository.
        const string source = """
            using System;
            using JustDummies;

            public static class Assert {
                public static T Throws<T>(Func<object> code) where T : Exception => null!;
            }

            public static class Sample {
                public static void M() {
                    Assert.Throws<ArgumentException>(() => Any.String().WithLength(3).StartingWith("ORD-"));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_generated_value() {
        const string source = """
            using JustDummies;

            public static class Assert {
                public static void NotNull(object value) { }
            }

            public static class Sample {
                public static void M() {
                    Assert.NotNull(Any.String().NonEmpty().Generate());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorWhereValueExpectedAnalyzer(), source, "JD011");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Is_disabled_by_default() {
        // The severity choice is the finding, not an incidental: dogfooding produced no true positive and two false
        // ones, so the rule ships opt-in (ADR-0059's follow-up).
        DiagnosticDescriptor descriptor = new GeneratorWhereValueExpectedAnalyzer().SupportedDiagnostics[0];

        Check.That(descriptor.IsEnabledByDefault).IsFalse();
        Check.That(descriptor.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Warning);
    }

}
