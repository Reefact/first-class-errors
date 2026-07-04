using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce007DocumentedByInvalidSignatureTests {

    [Fact]
    public async Task Reports_when_target_has_wrong_return_type() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy(nameof(Doc))]
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");

                private static string Doc() => "not a documentation";
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByInvalidSignatureAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE007");
    }

    [Fact]
    public async Task Reports_when_target_is_not_static() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public class SampleError {
                [DocumentedBy(nameof(Doc))]
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");

                private ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByInvalidSignatureAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE007");
    }

    [Fact]
    public async Task Does_not_report_for_valid_documentation_method() {
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

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByInvalidSignatureAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_target_is_missing() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy("Nope")]
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByInvalidSignatureAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
