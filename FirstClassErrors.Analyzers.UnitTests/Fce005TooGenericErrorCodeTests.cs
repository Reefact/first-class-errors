using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce005TooGenericErrorCodeTests {

    [Fact]
    public async Task Reports_a_generic_code() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("INVALID");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TooGenericErrorCodeAnalyzer(), source, "FCE005");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE005");
    }

    [Fact]
    public async Task Does_not_report_a_specific_code() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("MONEY_TRANSFER_INVALID");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TooGenericErrorCodeAnalyzer(), source, "FCE005");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
