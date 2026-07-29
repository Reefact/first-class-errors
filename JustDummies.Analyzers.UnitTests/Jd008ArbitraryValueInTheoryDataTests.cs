using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd008ArbitraryValueInTheoryDataTests {

    [Fact]
    public async Task Reports_a_draw_in_a_TheoryData_property() {
        const string source = """
            using JustDummies;
            using Xunit;

            public class Sample {
                public static TheoryData<string> Cases => new() { Any.String().NonEmpty().Generate() };

                [Theory]
                [MemberData(nameof(Cases))]
                public void T(string reference) { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ArbitraryValueInTheoryDataAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD008");
        Check.That(diagnostics[0].GetMessage()).Contains("discovery");
    }

    [Fact]
    public async Task Reports_a_draw_in_a_member_named_by_MemberData() {
        const string source = """
            using System.Collections.Generic;
            using JustDummies;
            using Xunit;

            public class Sample {
                public static IEnumerable<object[]> Cases() {
                    yield return new object[] { Any.Int32().Positive().Generate() };
                }

                [Theory]
                [MemberData(nameof(Cases))]
                public void T(int quantity) { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ArbitraryValueInTheoryDataAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD008");
    }

    [Fact]
    public async Task Reports_a_draw_in_a_ClassData_provider() {
        const string source = """
            using System.Collections;
            using System.Collections.Generic;
            using JustDummies;
            using Xunit;

            public class Cases : IEnumerable<object[]> {
                public IEnumerator<object[]> GetEnumerator() {
                    yield return new object[] { Any.Int32().Positive().Generate() };
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public class Sample {
                [Theory]
                [ClassData(typeof(Cases))]
                public void T(int quantity) { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ArbitraryValueInTheoryDataAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD008");
    }

    [Fact]
    public async Task Does_not_report_a_provider_that_yields_the_generator() {
        // The compliant shape: the provider hands over the recipe, the body draws inside the pinned scope.
        const string source = """
            using JustDummies;
            using Xunit;

            public class Sample {
                public static TheoryData<IAny<string>> Cases => new() { Any.String().NonEmpty() };

                [Theory]
                [MemberData(nameof(Cases))]
                public void T(IAny<string> reference) {
                    string value = reference.Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ArbitraryValueInTheoryDataAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_an_ordinary_test_body() {
        const string source = """
            using JustDummies;
            using Xunit;

            public class Sample {
                [Fact]
                public void T() {
                    string reference = Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ArbitraryValueInTheoryDataAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_ordinary_collection_of_object_arrays() {
        // A member returning object[] sequences is only a provider when xUnit is told so; without a [MemberData]
        // naming it and without the TheoryData shape, the rule must stay quiet here — this one IS named, so the
        // sibling test covers the positive. Here nothing references it.
        const string source = """
            using System.Collections.Generic;
            using JustDummies;

            public class Sample {
                public static List<string> Values() {
                    return new List<string> { Any.String().NonEmpty().Generate() };
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new ArbitraryValueInTheoryDataAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
