using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce002EmptyErrorCodeTests {

    [Fact]
    public async Task Reports_on_empty_string_literal() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static readonly ErrorCode Code = ErrorCode.Create("");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE002");
    }

    [Fact]
    public async Task Reports_on_whitespace_string_literal() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static readonly ErrorCode Code = ErrorCode.Create("   ");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE002");
    }

    [Fact]
    public async Task Does_not_report_on_valid_literal() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static readonly ErrorCode Code = ErrorCode.Create("VALID_CODE");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_on_non_literal_argument() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                private static string Build() => "DYNAMIC_CODE";
                public static readonly ErrorCode Code = ErrorCode.Create(Build());
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
