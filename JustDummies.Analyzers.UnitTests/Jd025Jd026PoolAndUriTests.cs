using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd025DuplicatePoolValueTests {

    [Theory]
    [InlineData("Any.OneOf(1, 2, 1)")]
    [InlineData("Any.OneOf(\"EUR\", \"USD\", \"EUR\")")]
    [InlineData("Any.Int32().OneOf(3, 3)")]
    [InlineData("Any.OneOf(true, false, true)")]
    public async Task Reports_a_value_listed_twice(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicatePoolValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD025");
    }

    [Theory]
    [InlineData("Any.OneOf(1, 2, 3)")]
    [InlineData("Any.OneOf(\"a\", \"A\")")]
    [InlineData("Any.Int32().OneOf(3, 4)")]
    public async Task Does_not_report_a_pool_of_distinct_values(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicatePoolValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_an_enum_member_listed_twice() {
        const string source = """
            using JustDummies;

            public enum Status { Active, Pending, Closed }

            public static class Sample {
                public static void M() {
                    _ = Any.OneOf(Status.Active, Status.Pending, Status.Active);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicatePoolValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD025");
    }

    [Fact]
    public async Task Stands_down_when_one_element_does_not_fold() {
        // The unfoldable element could itself be the duplicate of a later one, so a partial walk would claim a
        // completeness it does not have.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(int supplied) {
                    _ = Any.OneOf(1, supplied, 1);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicatePoolValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_pool_held_in_a_variable() {
        const string source = """
            using System.Collections.Generic;
            using JustDummies;

            public static class Sample {
                public static void M(IReadOnlyList<int> pool) {
                    _ = Any.ElementOf(pool);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicatePoolValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_negative_test() {
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Run(() => Any.OneOf(1, 1));
                }

                private static void Run(Func<object> body) { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DuplicatePoolValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}

public class Jd026EmptyRelativeUriTests {

    [Fact]
    public async Task Reports_a_relative_uri_that_can_only_be_empty() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Any.Uri().Relative().WithPathSegments(0);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyRelativeUriAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD026");
    }

    [Theory]
    [InlineData("Any.Uri().Relative().WithPathSegments(0).WithQuery()")]
    [InlineData("Any.Uri().Relative().WithPathSegments(0).WithFragment()")]
    [InlineData("Any.Uri().Relative().Rooted().WithPathSegments(0)")]
    [InlineData("Any.Uri().Relative().WithPathSegments(1)")]
    [InlineData("Any.Uri().Relative()")]
    [InlineData("Any.Uri().Web().WithPathSegments(0)")]
    [InlineData("Any.Uri().Ftp().WithPathSegments(0)")]
    public async Task Does_not_report_a_reference_that_can_render(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyRelativeUriAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_constant_segment_count() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(int segments) {
                    _ = Any.Uri().Relative().WithPathSegments(segments);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyRelativeUriAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_negative_test() {
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    Run(() => Any.Uri().Relative().WithPathSegments(0));
                }

                private static void Run(Func<object> body) { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EmptyRelativeUriAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
