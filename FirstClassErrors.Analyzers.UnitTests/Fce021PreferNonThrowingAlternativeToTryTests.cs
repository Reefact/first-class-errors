using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

public class Fce021PreferNonThrowingAlternativeToTryTests {

    #region Reports — a throwing call whose non-throwing counterpart resolves (regardless of origin)

    [Theory]
    [InlineData("int", "int.Parse(raw)")]
    [InlineData("double", "double.Parse(raw)")]
    [InlineData("System.Guid", "System.Guid.Parse(raw)")]
    [InlineData("System.DateTime", "System.DateTime.Parse(raw)")]
    [InlineData("System.TimeSpan", "System.TimeSpan.Parse(raw)")]
    [InlineData("System.Net.IPAddress", "System.Net.IPAddress.Parse(raw)")]
    public async Task Reports_a_BCL_parse_that_has_a_matching_TryParse(string resultType, string call) {
        string source = $$"""
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<{{resultType}}> M(string raw) {
                    return Outcome.Try<{{resultType}}, System.FormatException>(() => {{call}}, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE021");
    }

    [Theory]
    [InlineData("System.Guid", "new System.Guid(raw)")]                                     // ctor -> TryParse
    [InlineData("System.Version", "new System.Version(raw)")]                               // ctor -> TryParse
    [InlineData("System.Uri", "new System.Uri(raw, System.UriKind.Absolute)")]              // ctor -> TryCreate
    [InlineData("System.Net.Mail.MailAddress", "new System.Net.Mail.MailAddress(raw)")]     // ctor -> TryCreate
    public async Task Reports_a_constructor_that_has_a_matching_counterpart(string resultType, string call) {
        string source = $$"""
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<{{resultType}}> M(string raw) {
                    return Outcome.Try<{{resultType}}, System.FormatException>(() => {{call}}, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE021");
    }

    [Fact]
    public async Task Reports_a_block_bodied_lambda_that_returns_a_single_call() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(() => { return int.Parse(raw); }, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE021");
    }

    [Fact]
    public async Task Reports_a_generic_parse_by_constructing_the_counterpart() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<System.DayOfWeek> M(string raw) {
                    return Outcome.Try<System.DayOfWeek, System.FormatException>(() => System.Enum.Parse<System.DayOfWeek>(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE021");
    }

    [Fact]
    public async Task Reports_on_a_user_defined_method_with_a_matching_TryParse() {
        // The rule no longer cares where the type is declared: a consumer type with a shape-matching pair is flagged too.
        const string source = """
            using FirstClassErrors;

            public struct Temperature {
                public static Temperature Parse(string s) => default;
                public static bool TryParse(string s, out Temperature value) { value = default; return true; }
            }

            public static class Sample {
                public static Outcome<Temperature> M(string raw) {
                    return Outcome.Try<Temperature, System.FormatException>(() => Temperature.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE021");
    }

    [Fact]
    public async Task Reports_on_a_user_defined_constructor_with_a_matching_TryCreate() {
        // Fires even though this TryCreate may not be a behaviour-preserving inverse — that is why the rule is advisory
        // and suppressible; the developer confirms and silences it where it does not fit.
        const string source = """
            using FirstClassErrors;

            public sealed class Slug {
                public Slug(string value) { }
                public static bool TryCreate(string value, out Slug result) { result = new Slug(value); return true; }
            }

            public static class Sample {
                public static Outcome<Slug> M(string raw) {
                    return Outcome.Try<Slug, System.FormatException>(() => new Slug(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE021");
    }

    [Fact]
    public async Task Reports_by_default_without_an_explicit_opt_in() {
        // FCE021 is on by default (Warning); no .editorconfig opt-in is needed for it to fire.
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(() => int.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Fact]
    public async Task Reports_a_message_naming_both_the_call_and_its_counterpart() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(() => int.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("int.Parse").And.Contains("int.TryParse");
    }

    #endregion

    #region Does not report — no compatible counterpart resolves

    [Fact]
    public async Task Does_not_report_a_call_with_no_counterpart() {
        const string source = """
            using FirstClassErrors;

            public struct Gadget {
                public static Gadget Parse(string s) => default;
            }

            public static class Sample {
                public static Outcome<Gadget> M(string raw) {
                    return Outcome.Try<Gadget, System.FormatException>(() => Gadget.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_no_exact_arity_TryParse_exists() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(() => int.Parse(raw, System.Globalization.NumberStyles.Integer), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_the_counterpart_signature_does_not_match() {
        const string source = """
            using FirstClassErrors;

            public struct Foo {
                public static Foo Parse(string s) => default;
                public static bool TryParse(string s) => true; // no out result: not a drop-in
            }

            public static class Sample {
                public static Outcome<Foo> M(string raw) {
                    return Outcome.Try<Foo, System.FormatException>(() => Foo.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_when_the_counterpart_is_an_instance_method() {
        const string source = """
            using FirstClassErrors;

            public struct Thing {
                public static Thing Parse(string s) => default;
                public bool TryParse(string s, out Thing value) { value = default; return true; } // instance, not static
            }

            public static class Sample {
                public static Outcome<Thing> M(string raw) {
                    return Outcome.Try<Thing, System.FormatException>(() => Thing.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    #endregion

    #region Does not report — the wrapped operation is not a single, static, exact call

    [Fact]
    public async Task Does_not_report_an_instance_throwing_call() {
        // A static counterpart cannot carry the receiver, so an instance call is never a one-for-one rewrite.
        const string source = """
            using FirstClassErrors;

            public class Parser {
                public int Read(string s) => 0;
                public static bool TryRead(string s, out int value) { value = 0; return true; }
            }

            public static class Sample {
                public static Outcome<int> M(Parser parser, string raw) {
                    return Outcome.Try<int, System.FormatException>(() => parser.Read(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_construction_with_an_object_initializer() {
        const string source = """
            using FirstClassErrors;

            public class Widget {
                public int Tag;
                public Widget(string s) { }
                public static bool TryCreate(string s, out Widget value) { value = new Widget(s); return true; }
            }

            public static class Sample {
                public static Outcome<Widget> M(string raw) {
                    return Outcome.Try<Widget, System.FormatException>(() => new Widget(raw) { Tag = 1 }, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_lambda_with_more_than_one_statement() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(() => {
                        string trimmed = raw.Trim();
                        return int.Parse(trimmed);
                    }, _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_call_wrapped_in_a_cast() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(() => (int)long.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    #endregion

    #region Does not report — wrong overload or not Outcome.Try

    [Fact]
    public async Task Does_not_report_the_async_overload() {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using FirstClassErrors;

            public static class Sample {
                public static Task<Outcome<int>> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(ct => Task.FromResult(int.Parse(raw)), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_the_void_overload() {
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome M(string raw) {
                    return Outcome.Try<System.FormatException>(() => System.Console.WriteLine(int.Parse(raw)), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_Try_that_is_not_on_Outcome() {
        const string source = """
            using System;

            public static class Other {
                public static T Try<T, TException>(Func<T> operation, Func<TException, object> onError) where TException : Exception => operation();
            }

            public static class Sample {
                public static int M(string raw) {
                    return Other.Try<int, FormatException>(() => int.Parse(raw), _ => null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    #endregion

}
