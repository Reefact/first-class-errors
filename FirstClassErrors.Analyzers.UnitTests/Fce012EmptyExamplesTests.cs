using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce012EmptyExamplesTests {

    [Fact]
    public async Task Reports_when_with_examples_has_no_factory() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyExamplesAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE012");
    }

    [Fact]
    public async Task Does_not_report_when_an_example_is_provided() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>(() => Boom());
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyExamplesAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
