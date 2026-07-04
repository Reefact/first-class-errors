using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce004InvalidErrorCodeFormatTests {

    [Fact]
    public async Task Reports_when_code_is_not_upper_snake_case() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("moneyTransferInvalid");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InvalidErrorCodeFormatAnalyzer(), source, "FCE004");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE004");
    }

    [Fact]
    public async Task Does_not_report_for_upper_snake_case() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("MONEY_TRANSFER_INVALID");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new InvalidErrorCodeFormatAnalyzer(), source, "FCE004");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
