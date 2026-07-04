using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce013ExampleDoesNotCallDocumentedFactoryTests {

    [Fact]
    public async Task Reports_when_example_does_not_call_a_factory_of_the_documenting_type() {
        const string source = """
            using FirstClassErrors;

            public static class Other {
                public static DomainError Build() =>
                    DomainError.Create(ErrorCode.Create("OTHER"), "diagnostic").WithPublicMessage("short");
            }

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic()
                                 .WithExamples<DomainError>(() => Other.Build());
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ExampleDoesNotCallDocumentedFactoryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE013");
    }

    [Fact]
    public async Task Does_not_report_when_example_calls_a_factory_of_the_documenting_type() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                internal static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("short");

                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic()
                                 .WithExamples<DomainError>(() => Boom());
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ExampleDoesNotCallDocumentedFactoryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_there_are_no_examples() {
        const string source = """
            using FirstClassErrors;

            [ProvidesErrorsFor("Sample")]
            public static class SampleError {
                private static ErrorDocumentation Doc() =>
                    DescribeError.WithTitle("t").WithDescription("d").WithoutRule().WithoutDiagnostic().WithExamples<DomainError>();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ExampleDoesNotCallDocumentedFactoryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
