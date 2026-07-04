using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce014ShortMessageSameAsDetailedMessageTests {

    [Fact]
    public async Task Reports_when_short_and_detailed_are_identical() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("Same message", "Same message");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ShortMessageSameAsDetailedMessageAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE014");
    }

    [Fact]
    public async Task Does_not_report_when_messages_differ() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("Short", "A longer detail");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ShortMessageSameAsDetailedMessageAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_only_short_message_is_given() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static DomainError Boom() =>
                    DomainError.Create(ErrorCode.Create("BOOM"), "diagnostic").WithPublicMessage("Short only");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ShortMessageSameAsDetailedMessageAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
