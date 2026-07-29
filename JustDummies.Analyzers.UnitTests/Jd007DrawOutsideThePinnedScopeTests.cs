using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd007DrawOutsideThePinnedScopeTests {

    [Fact]
    public async Task Reports_a_draw_in_the_constructor_of_a_reproducible_class() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                private readonly string _reference;

                public Sample() {
                    _reference = Any.String().NonEmpty().Generate();
                }

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
        Check.That(diagnostics[0].GetMessage()).Contains("constructor");
    }

    [Fact]
    public async Task Reports_a_draw_in_a_field_initializer() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
        Check.That(diagnostics[0].GetMessage()).Contains("field initializer");
    }

    [Fact]
    public async Task Reports_a_draw_in_InitializeAsync() {
        const string source = """
            using System.Threading.Tasks;
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample : IAsyncLifetime {
                private string _reference = "";

                public ValueTask InitializeAsync() {
                    _reference = Any.String().NonEmpty().Generate();

                    return default;
                }

                public ValueTask DisposeAsync() => default;

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
        Check.That(diagnostics[0].GetMessage()).Contains("InitializeAsync");
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_the_test_body() {
        // The body IS inside the pinned scope — verified against xunit.v3 by probe before this rule was written.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                [Fact]
                public void T() {
                    string reference = Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_class_without_the_attribute() {
        const string source = """
            using JustDummies;
            using Xunit;

            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_from_an_isolated_context() {
        // Any.WithSeed is isolated by design: the ambient scope does not govern it, so the diagnostic would be wrong.
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                private readonly string _reference;

                public Sample() {
                    AnyContext context = Any.WithSeed(1234);
                    _reference = context.String().NonEmpty().Generate();
                }

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_under_an_assembly_level_attribute() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            [assembly: Reproducible]

            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();

                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawOutsideThePinnedScopeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD007");
    }

}
