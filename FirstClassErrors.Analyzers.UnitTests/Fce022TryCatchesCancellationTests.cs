using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce022TryCatchesCancellationTests {

    [Fact]
    public async Task Reports_when_the_value_overload_catches_OperationCanceledException() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, OperationCanceledException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE022");
    }

    [Fact]
    public async Task Reports_when_the_caught_type_is_a_subtype_of_OperationCanceledException() {
        // TaskCanceledException : OperationCanceledException, so the same 'is not OperationCanceledException' filter
        // rules it out too — the catch is just as unreachable.
        const string source = """
            using System.Threading.Tasks;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, TaskCanceledException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE022");
    }

    [Fact]
    public async Task Reports_when_the_void_overload_catches_OperationCanceledException() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static void M() {
                    Outcome.Try<OperationCanceledException>(() => { }, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE022");
    }

    [Fact]
    public async Task Reports_when_the_async_overload_catches_OperationCanceledException() {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using FirstClassErrors;

            public static class Sample {
                public static Task<Outcome<int>> M() {
                    return Outcome.Try<int, OperationCanceledException>(ct => Task.FromResult(1), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE022");
    }

    [Fact]
    public async Task Names_the_caught_type_in_the_message() {
        const string source = """
            using System.Threading.Tasks;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, TaskCanceledException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("TaskCanceledException");
    }

    [Fact]
    public async Task Does_not_report_when_the_base_Exception_is_caught() {
        // Exception is a base of OperationCanceledException, not a subtype, so the filter can still engage for every
        // non-cancellation exception. FCE019 owns that case (too-broad); FCE022 stays silent.
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, Exception>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_an_unrelated_exception_is_caught() {
        const string source = """
            using System;
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M() {
                    return Outcome.Try<int, FormatException>(() => 1, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

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
                    Other.Try<OperationCanceledException>(() => { });
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new TryCatchesCancellationAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
