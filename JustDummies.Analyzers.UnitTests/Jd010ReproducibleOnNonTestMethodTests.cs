using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd010ReproducibleOnNonTestMethodTests {

    [Fact]
    public async Task Reports_the_attribute_on_a_helper_method() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;

            public class Sample {
                [Reproducible]
                private string Arrange() => Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ReproducibleOnNonTestMethodAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD010");
        Check.That(diagnostics[0].GetMessage()).Contains("Arrange");
    }

    [Fact]
    public async Task Does_not_report_the_attribute_on_a_Fact() {
        const string source = """
            using JustDummies;
            using JustDummies.Xunit;
            using Xunit;

            public class Sample {
                [Fact]
                [Reproducible]
                public void T() {
                    string reference = Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ReproducibleOnNonTestMethodAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_the_attribute_on_a_Theory() {
        const string source = """
            using JustDummies.Xunit;
            using Xunit;

            public class Sample {
                [Theory]
                [InlineData(1)]
                [Reproducible]
                public void T(int value) { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ReproducibleOnNonTestMethodAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_the_attribute_on_a_class() {
        // The class and assembly levels are exactly where xUnit does collect it.
        const string source = """
            using JustDummies.Xunit;
            using Xunit;

            [Reproducible]
            public class Sample {
                [Fact]
                public void T() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ReproducibleOnNonTestMethodAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_method_without_the_attribute() {
        const string source = """
            using JustDummies;

            public class Sample {
                private string Arrange() => Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ReproducibleOnNonTestMethodAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
