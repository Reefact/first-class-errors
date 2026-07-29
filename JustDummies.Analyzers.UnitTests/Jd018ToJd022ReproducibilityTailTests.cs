using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd018NestedReproducibilityScopeTests {

    [Fact]
    public async Task Reports_a_runner_nested_in_another_runner() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.Reproducibly(() => {
                        Any.Reproducibly(() => { });
                    });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new NestedReproducibilityScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD018");
        Check.That(diagnostics[0].GetMessage()).Contains("another Any.Reproducibly scope");
    }

    [Fact]
    public async Task Reports_a_runner_inside_a_reproducible_test() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            public class Sample {
                [Fact, Reproducible]
                public void T() {
                    Any.Reproducibly(() => { });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new NestedReproducibilityScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("[Reproducible] test");
    }

    [Fact]
    public async Task Does_not_report_the_seeded_overload() {
        // Pinning a chosen seed inside is deliberate; only the seedless form silently overrides the outer instruction.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            public class Sample {
                [Fact, Reproducible]
                public void T() {
                    Any.Reproducibly(1234, () => { });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new NestedReproducibilityScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_lone_runner() {
        const string source = """
            using JustDummies;
            using Xunit;

            public class Sample {
                [Fact]
                public void T() {
                    Any.Reproducibly(() => { });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new NestedReproducibilityScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}

public class Jd019CommittedReplaySeedTests {

    [Theory]
    [InlineData("Any.Reproducibly(1234, () => { });")]
    [InlineData("Any.ReproduciblyAsync(1234, () => System.Threading.Tasks.Task.CompletedTask);")]
    [InlineData("_ = Any.WithSeed(1234);")]
    public async Task Reports_a_pinned_constant_seed(string statement) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    {{statement}}
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CommittedReplaySeedAnalyzer(), source, "JD019");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD019");
        Check.That(diagnostics[0].GetMessage()).Contains("1234");
    }

    [Fact]
    public async Task Reports_a_pinned_seed_on_the_attribute() {
        const string source = """
            using JustDummies.Xunit;
            using Xunit;

            public class Sample {
                [Fact, Reproducible(Seed = 1234)]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CommittedReplaySeedAnalyzer(), source, "JD019");

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Fact]
    public async Task Does_not_report_the_seedless_form() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Any.Reproducibly(() => { });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CommittedReplaySeedAnalyzer(), source, "JD019");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_computed_seed() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(int seed) {
                    Any.Reproducibly(seed, () => { });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new CommittedReplaySeedAnalyzer(), source, "JD019");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Is_disabled_by_default() {
        // The maintainer guide instructs pinning a seed for anything statistical, so a default-on rule would fight
        // documented practice.
        DiagnosticDescriptor descriptor = new CommittedReplaySeedAnalyzer().SupportedDiagnostics[0];

        Check.That(descriptor.IsEnabledByDefault).IsFalse();
    }

}

public class Jd020SharedStaticAnyContextTests {

    [Fact]
    public async Task Reports_a_static_context_field() {
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly AnyContext Context = Any.WithSeed(1234);
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SharedStaticAnyContextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD020");
        Check.That(diagnostics[0].GetMessage()).Contains("Context");
    }

    [Fact]
    public async Task Does_not_report_an_instance_context() {
        const string source = """
            using JustDummies;

            public class Sample {
                private readonly AnyContext _context = Any.WithSeed(1234);
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SharedStaticAnyContextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_static_generator() {
        // A shared generator is safe: the random source is resolved at Generate(), not at construction.
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly IAny<string> Reference = Any.String().NonEmpty();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SharedStaticAnyContextAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}

public class Jd021BlankReplaySnippetTests {

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public async Task Reports_a_blank_snippet(string literal) {
        string source = $$"""
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    using IDisposable scope = Any.UseSeed(1234, {{literal}});
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new BlankReplaySnippetAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD021");
    }

    [Fact]
    public async Task Does_not_report_a_real_snippet() {
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    using IDisposable scope = Any.UseSeed(1234, "[Reproducible(Seed = 1234)]");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new BlankReplaySnippetAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_the_overload_without_a_snippet() {
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    using IDisposable scope = Any.UseSeed(1234);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new BlankReplaySnippetAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_guard_asserting_negative_test() {
        // JustDummies.PropertyTests asserts exactly this rejection.
        const string source = """
            using System;
            using JustDummies;

            public static class Expect {
                public static bool Throws<T>(Func<object> code) where T : Exception => true;
            }

            public static class Sample {
                public static void M(int seed) {
                    Expect.Throws<ArgumentException>(() => Any.UseSeed(seed, " "));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new BlankReplaySnippetAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}

public class Jd022ParallelDrawWithoutPerItemSeedTests {

    [Fact]
    public async Task Reports_a_draw_in_a_parallel_body_with_no_scope() {
        const string source = """
            using System.Threading.Tasks;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Parallel.For(0, 64, index => {
                        string reference = Any.String().NonEmpty().Generate();
                    });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ParallelDrawWithoutPerItemSeedAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD022");
    }

    [Fact]
    public async Task Does_not_report_a_body_that_opens_its_own_scope() {
        // The documented shape: a scope inside the loop body gives each unit of work its own sequence.
        const string source = """
            using System.Threading.Tasks;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    const int runSeed = 20240501;

                    Parallel.For(0, 64, index => {
                        using (Any.UseSeed(unchecked(runSeed * 397 ^ index))) {
                            string reference = Any.String().NonEmpty().Generate();
                        }
                    });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ParallelDrawWithoutPerItemSeedAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_sequential_draw() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    string reference = Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ParallelDrawWithoutPerItemSeedAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
