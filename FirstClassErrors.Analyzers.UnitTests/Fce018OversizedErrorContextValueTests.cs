using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce018OversizedErrorContextValueTests {

    [Theory]
    [InlineData("byte[]")]
    [InlineData("System.IO.Stream")]
    [InlineData("System.IO.MemoryStream")]
    [InlineData("System.IO.FileInfo")]
    public async Task Reports_a_bulk_payload_value_type(string valueType) {
        string source = $$"""
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<{{valueType}}> K = ErrorContextKey.Create<{{valueType}}>("PAYLOAD");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new OversizedErrorContextValueAnalyzer(), source, "FCE018");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE018");
    }

    [Theory]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("System.Guid")]
    [InlineData("System.DateOnly")]
    [InlineData("int[]")]
    public async Task Does_not_report_a_small_value_type(string valueType) {
        string source = $$"""
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<{{valueType}}> K = ErrorContextKey.Create<{{valueType}}>("FIELD");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new OversizedErrorContextValueAnalyzer(), source, "FCE018");

        Check.That(diagnostics).IsEmpty();
    }

    [Fact]
    public async Task Names_the_offending_value_type_in_the_message() {
        const string source = """
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<byte[]> K = ErrorContextKey.Create<byte[]>("ATTACHMENT");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new OversizedErrorContextValueAnalyzer(), source, "FCE018");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("ATTACHMENT");
        Check.That(diagnostics[0].GetMessage()).Contains("byte[]");
    }

    [Fact]
    public async Task Is_disabled_by_default() {
        const string source = """
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<byte[]> K = ErrorContextKey.Create<byte[]>("PAYLOAD");
            }
            """;

        // No id passed to the harness, so the opt-in rule stays off.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new OversizedErrorContextValueAnalyzer(), source);

        Check.That(diagnostics).IsEmpty();
    }

}
