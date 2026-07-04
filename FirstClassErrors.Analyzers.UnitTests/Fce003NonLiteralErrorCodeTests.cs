using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce003NonLiteralErrorCodeTests {

    [Fact]
    public async Task Reports_when_code_is_computed_at_runtime() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                private static string Build() => "DYNAMIC";
                public static readonly ErrorCode A = ErrorCode.Create(Build());
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new NonLiteralErrorCodeAnalyzer(), source, "FCE003");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE003");
    }

    [Fact]
    public async Task Does_not_report_for_a_literal_code() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("LITERAL_CODE");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new NonLiteralErrorCodeAnalyzer(), source, "FCE003");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_for_a_constant_reference() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                private const string Value = "CONST_CODE";
                public static readonly ErrorCode A = ErrorCode.Create(Value);
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new NonLiteralErrorCodeAnalyzer(), source, "FCE003");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
