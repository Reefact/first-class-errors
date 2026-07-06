using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce016UnusedToExceptionResultTests {

    [Fact]
    public async Task Reports_when_result_is_discarded() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static void M(DomainError error) {
                    error.ToException();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedToExceptionResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE016");
    }

    [Fact]
    public async Task Reports_when_result_is_assigned_to_a_discard() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static void M(DomainError error) {
                    _ = error.ToException();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedToExceptionResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE016");
    }

    [Fact]
    public async Task Does_not_report_when_thrown() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static void M(DomainError error) {
                    throw error.ToException();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedToExceptionResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_result_is_captured() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static void M(DomainError error) {
                    var exception = error.ToException();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UnusedToExceptionResultAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
