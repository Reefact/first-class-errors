using System.Collections.Immutable;
using System.Linq;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce001DuplicateErrorCodeTests {

    [Fact]
    public async Task Reports_each_site_when_same_code_is_created_twice() {
        const string source = """
            using FirstClassErrors;

            public static class First {
                public static readonly ErrorCode A = ErrorCode.Create("DUP");
            }

            public static class Second {
                public static readonly ErrorCode B = ErrorCode.Create("DUP");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(2);
        Check.That(diagnostics.All(d => d.Id == "FCE001")).IsTrue();
    }

    [Fact]
    public async Task Does_not_report_for_distinct_codes() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("ONE");
                public static readonly ErrorCode B = ErrorCode.Create("TWO");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_for_a_single_use() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("ONLY");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Ignores_duplicate_empty_codes_which_belong_to_FCE002() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                public static readonly ErrorCode A = ErrorCode.Create("");
                public static readonly ErrorCode B = ErrorCode.Create("");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Ignores_non_literal_codes() {
        const string source = """
            using FirstClassErrors;

            public static class Codes {
                private static string Build() => "DYNAMIC";
                public static readonly ErrorCode A = ErrorCode.Create(Build());
                public static readonly ErrorCode B = ErrorCode.Create(Build());
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicateErrorCodeAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
