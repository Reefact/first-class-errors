using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce010MultipleFactoriesShareDocumentationTests {

    [Fact]
    public async Task Reports_each_factory_sharing_a_documentation_method() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy(nameof(Doc))]
                internal static DomainError A() =>
                    DomainError.Create(ErrorCode.Create("A"), "diagnostic").WithPublicMessage("short");

                [DocumentedBy(nameof(Doc))]
                internal static DomainError B() =>
                    DomainError.Create(ErrorCode.Create("B"), "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new MultipleFactoriesShareDocumentationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(2);
        Check.That(diagnostics.All(d => d.Id == "FCE010")).IsTrue();
    }

    [Fact]
    public async Task Does_not_report_when_each_factory_has_its_own_documentation() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                [DocumentedBy(nameof(DocA))]
                internal static DomainError A() =>
                    DomainError.Create(ErrorCode.Create("A"), "diagnostic").WithPublicMessage("short");

                [DocumentedBy(nameof(DocB))]
                internal static DomainError B() =>
                    DomainError.Create(ErrorCode.Create("B"), "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation DocA() =>
                    DescribeError.WithTitle("a").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();

                private static ErrorDocumentation DocB() =>
                    DescribeError.WithTitle("b").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new MultipleFactoriesShareDocumentationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
