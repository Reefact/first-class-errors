using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce006DocumentedByTargetNotFoundTests {

    [Fact]
    public async Task Reports_when_target_method_does_not_exist() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy("Nope")]
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByTargetNotFoundAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE006");
    }

    [Fact]
    public async Task Does_not_report_when_target_method_exists() {
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

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByTargetNotFoundAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_method_has_no_documented_by() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByTargetNotFoundAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
