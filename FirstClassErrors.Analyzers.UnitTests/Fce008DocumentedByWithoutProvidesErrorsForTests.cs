using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce008DocumentedByWithoutProvidesErrorsForTests {

    [Fact]
    public async Task Reports_when_documented_type_lacks_provides_errors_for() {
        const string source = """
            using FirstClassErrors;

            public static class SampleError {
                [DocumentedBy(nameof(Doc))]
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByWithoutProvidesErrorsForAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE008");
    }

    [Fact]
    public async Task Does_not_report_when_provides_errors_for_is_present() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy(nameof(Doc))]
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByWithoutProvidesErrorsForAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_type_has_no_documented_factory() {
        const string source = """
            using FirstClassErrors;

            public static class SampleError {
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByWithoutProvidesErrorsForAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
