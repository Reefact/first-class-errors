using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce015DocumentationTitleTooGenericTests {

    [Fact]
    public async Task Reports_a_generic_title() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("Invalid value").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentationTitleTooGenericAnalyzer(), source, "FCE015");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE015");
    }

    [Fact]
    public async Task Does_not_report_a_specific_title() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("Temperature below absolute zero").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentationTitleTooGenericAnalyzer(), source, "FCE015");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
