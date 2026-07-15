using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce017SensitiveDataInErrorContextTests {

    [Theory]
    [InlineData("USER_PASSWORD")]
    [InlineData("Passphrase")]
    [InlineData("API_KEY")]
    [InlineData("apiKey")]
    [InlineData("AccessToken")]
    [InlineData("REFRESH_TOKEN")]
    [InlineData("ClientSecret")]
    [InlineData("CONNECTION_STRING")]
    [InlineData("PrivateKey")]
    [InlineData("CREDIT_CARD_NUMBER")]
    [InlineData("USER_PIN")]
    [InlineData("SSN")]
    [InlineData("cvv")]
    [InlineData("bearerToken")]
    public async Task Reports_a_sensitive_key_name(string keyName) {
        string source = $$"""
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<string> K = ErrorContextKey.Create<string>("{{keyName}}");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SensitiveDataInErrorContextAnalyzer(), source, "FCE017");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE017");
    }

    [Theory]
    [InlineData("TRANSACTION_DATE")]
    [InlineData("OrderId")]
    [InlineData("CustomerId")]
    [InlineData("ENDPOINT_URL")]
    [InlineData("ProvidedTemperature")]
    [InlineData("Provider")]
    public async Task Does_not_report_a_benign_key_name(string keyName) {
        string source = $$"""
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<string> K = ErrorContextKey.Create<string>("{{keyName}}");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SensitiveDataInErrorContextAnalyzer(), source, "FCE017");

        Check.That(diagnostics).IsEmpty();
    }

    [Fact]
    public async Task Reports_regardless_of_which_Create_overload_is_used() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<string> K =
                    ErrorContextKey.Create<string>("USER_PASSWORD", () => "The user's password.");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SensitiveDataInErrorContextAnalyzer(), source, "FCE017");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE017");
    }

    [Fact]
    public async Task Does_not_report_a_non_literal_name() {
        const string source = """
            using FirstClassErrors;

            public static class Keys {
                private const string Name = "USER_PASSWORD";
                public static readonly ErrorContextKey<string> K = ErrorContextKey.Create<string>(Compute());
                private static string Compute() => Name + "_HASH";
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SensitiveDataInErrorContextAnalyzer(), source, "FCE017");

        Check.That(diagnostics).IsEmpty();
    }

    [Fact]
    public async Task Is_disabled_by_default() {
        const string source = """
            using FirstClassErrors;

            public static class Keys {
                public static readonly ErrorContextKey<string> K = ErrorContextKey.Create<string>("USER_PASSWORD");
            }
            """;

        // No id passed to the harness, so the opt-in rule stays off.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new SensitiveDataInErrorContextAnalyzer(), source);

        Check.That(diagnostics).IsEmpty();
    }

}
