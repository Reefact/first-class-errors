using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce020TryCatchesRichProtocolExceptionTests {

    [Fact]
    public async Task Reports_when_Try_catches_HttpRequestException() {
        const string source = """
            using System.Net.Http;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, HttpRequestException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesRichProtocolExceptionAnalyzer(), source, "FCE020");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE020");
    }

    [Fact]
    public async Task Reports_when_Try_catches_a_subtype_of_a_protocol_exception() {
        const string source = """
            using System.Net.Http;
            using FirstClassErrors;

            public class TransportException : HttpRequestException { }

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, TransportException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesRichProtocolExceptionAnalyzer(), source, "FCE020");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE020");
    }

    [Fact]
    public async Task Reports_when_Try_catches_SocketException() {
        const string source = """
            using System.Net.Sockets;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, SocketException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesRichProtocolExceptionAnalyzer(), source, "FCE020");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE020");
    }

    [Fact]
    public async Task Does_not_report_a_domain_exception() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, FormatException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesRichProtocolExceptionAnalyzer(), source, "FCE020");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_broad_Exception_which_is_FCE019s_concern() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, Exception>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesRichProtocolExceptionAnalyzer(), source, "FCE020");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
