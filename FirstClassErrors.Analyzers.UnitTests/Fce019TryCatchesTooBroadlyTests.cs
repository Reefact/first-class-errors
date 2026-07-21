using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce019TryCatchesTooBroadlyTests {

    [Fact]
    public async Task Reports_when_the_value_overload_catches_Exception() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, Exception>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesTooBroadlyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE019");
    }

    [Fact]
    public async Task Reports_when_the_void_overload_catches_Exception() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static void M() {
                    Outcome.Try<Exception>(() => { }, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesTooBroadlyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE019");
    }

    [Fact]
    public async Task Reports_when_the_async_overload_catches_Exception() {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using FirstClassErrors;

            public static class Sample {
                public static Task<Outcome<int>> M() {
                    return Outcome.Try<int, Exception>(ct => Task.FromResult(1), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesTooBroadlyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE019");
    }

    [Fact]
    public async Task Does_not_report_when_a_specific_exception_is_caught() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, FormatException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesTooBroadlyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_Try_that_is_not_on_Outcome() {
        const string source = """
            using System;

            public static class Other {
                public static void Try<TException>(Action action) where TException : Exception { }
            }

            public static class Sample {
                public static void M() {
                    Other.Try<Exception>(() => { });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesTooBroadlyAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
