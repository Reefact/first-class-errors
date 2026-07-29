using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd004DiscardedSeedingResultTests {

    [Fact]
    public async Task Reports_a_discarded_UseSeed_statement() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.UseSeed(1234);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD004");
        Check.That(diagnostics[0].GetMessage()).Contains("UseSeed");
        Check.That(diagnostics[0].GetMessage()).Contains("stays pinned");
    }

    [Fact]
    public async Task Reports_a_UseSeed_assigned_to_a_discard() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.UseSeed(1234, "[Reproducible(Seed = 1234)]");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD004");
    }

    [Fact]
    public async Task Reports_a_discarded_WithSeed_with_its_own_consequence() {
        // WithSeed and UseSeed differ by one word and do opposite things, so the message must name which one is wrong.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.WithSeed(1234);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD004");
        Check.That(diagnostics[0].GetMessage()).Contains("WithSeed");
        Check.That(diagnostics[0].GetMessage()).Contains("pins nothing");
    }

    [Fact]
    public async Task Does_not_report_a_UseSeed_held_by_a_using_statement() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    using (Any.UseSeed(1234)) {
                        Any.String().Generate();
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_UseSeed_held_by_a_using_declaration() {
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    using IDisposable scope = Any.UseSeed(1234);

                    Any.String().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_captured_WithSeed_context() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string M() {
                    AnyContext context = Any.WithSeed(1234);

                    return context.String().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_guard_asserting_negative_test() {
        // A test asserting that UseSeed rejects its snippet never opens a scope, so there is nothing to leak. This
        // shape is live in JustDummies.PropertyTests/SeedDeterminismProperties.cs.
        const string source = """
            using System;
            using JustDummies;

            public static class Expect {
                public static bool Throws<T>(Action code) where T : Exception => true;
            }

            public static class Sample {
                public static void M(int seed) {
                    Expect.Throws<ArgumentNullException>(() => Any.UseSeed(seed, null!));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_another_types_UseSeed() {
        const string source = """
            using System;

            public static class Other {
                public static IDisposable UseSeed(int seed) => null!;
            }

            public static class Sample {
                public static void M() {
                    Other.UseSeed(1234);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_another_Any_member_whose_result_is_discarded() {
        // Only the two seeding entry points carry this hazard; a discarded generator is JD006's subject, not JD004's.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.String();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DiscardedSeedingResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
