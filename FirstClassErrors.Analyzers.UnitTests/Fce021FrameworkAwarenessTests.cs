using System.Collections.Immutable;

using FirstClassErrors.Analyzers;

using Microsoft.CodeAnalysis;

using NFluent;

namespace FirstClassErrors.Analyzers.UnitTests;

/// <summary>
///     Proves FCE021's framework-awareness end-to-end by compiling the analyzed snippet against the .NET Framework 4.7.2
///     reference assemblies (not the running runtime). A framework-aware rule reacts to the analyzed code's target
///     framework, so the same call can be flagged on a modern target and left alone on an older one.
/// </summary>
public class Fce021FrameworkAwarenessTests {

    [Fact]
    public async Task Reports_int_Parse_against_the_net472_reference_set_because_int_TryParse_exists_there() {
        // Control: int.TryParse ships on net472 too, so this still fires against the net472 reference set. It proves the
        // compilation resolves and the analyzer runs — which is what makes the silent result below meaningful rather
        // than a setup artefact.
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<int> M(string raw) {
                    return Outcome.Try<int, System.FormatException>(() => int.Parse(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAgainstNet472Async(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("FCE021");
    }

    [Fact]
    public async Task Does_not_report_new_MailAddress_against_the_net472_reference_set_where_TryCreate_does_not_exist() {
        // The marquee claim: MailAddress.TryCreate is .NET 5+, so against the net472 reference set no counterpart
        // resolves and the rule stays silent — even though the very same call fires on net10 (see the MailAddress case
        // in Fce021PreferNonThrowingAlternativeToTryTests.Reports_a_constructor_that_has_a_matching_counterpart).
        const string source = """
            using FirstClassErrors;

            public static class Sample {
                public static Outcome<System.Net.Mail.MailAddress> M(string raw) {
                    return Outcome.Try<System.Net.Mail.MailAddress, System.FormatException>(() => new System.Net.Mail.MailAddress(raw), _ => (Error)null);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAgainstNet472Async(new PreferNonThrowingAlternativeToTryAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
