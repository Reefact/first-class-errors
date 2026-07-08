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
    public async Task Reports_when_target_method_is_only_declared_on_a_base_type() {
        // A documentation method is resolved on the containing type only; inherited members are not considered. This
        // mirrors the extraction-time reader, which resolves the target without BindingFlags.FlattenHierarchy, so a
        // factory declared on a base type is deliberately not supported and must surface as FCE006.
        const string source = """
            using FirstClassErrors;

            public abstract class BaseError {
                protected static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }

            [ProvidesErrorsFor("Sample")]
            public class SampleError : BaseError {
                [DocumentedBy(nameof(Doc))]
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DocumentedByTargetNotFoundAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE006");
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
