using System.Collections.Immutable;
using System.Linq;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce011DuplicateDocumentedCodeTests {

    [Fact]
    public async Task Reports_when_two_documented_factories_share_a_code_field() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy(nameof(DocA))]
                internal static DomainError A() =>
                    DomainError.Create(Code.Shared, "diagnostic").WithPublicMessage("short");

                [DocumentedBy(nameof(DocB))]
                internal static DomainError B() =>
                    DomainError.Create(Code.Shared, "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation DocA() =>
                    DescribeError.WithTitle("a").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();

                private static ErrorDocumentation DocB() =>
                    DescribeError.WithTitle("b").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();

                private static class Code {
                    public static readonly ErrorCode Shared = ErrorCode.Create("SHARED");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateDocumentedCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(2);
        Check.That(diagnostics.All(d => d.Id == "FCE011")).IsTrue();
    }

    [Fact]
    public async Task Does_not_report_when_documented_factories_use_distinct_code_fields() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy(nameof(DocA))]
                internal static DomainError A() =>
                    DomainError.Create(Code.A, "diagnostic").WithPublicMessage("short");

                [DocumentedBy(nameof(DocB))]
                internal static DomainError B() =>
                    DomainError.Create(Code.B, "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation DocA() =>
                    DescribeError.WithTitle("a").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();

                private static ErrorDocumentation DocB() =>
                    DescribeError.WithTitle("b").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();

                private static class Code {
                    public static readonly ErrorCode A = ErrorCode.Create("A");
                    public static readonly ErrorCode B = ErrorCode.Create("B");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateDocumentedCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_the_sharing_factories_are_undocumented() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                internal static DomainError A() =>
                    DomainError.Create(Code.Shared, "diagnostic").WithPublicMessage("short");

                internal static DomainError B() =>
                    DomainError.Create(Code.Shared, "diagnostic").WithPublicMessage("short");

                private static class Code {
                    public static readonly ErrorCode Shared = ErrorCode.Create("SHARED");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateDocumentedCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
