using System.Collections.Immutable;

using JustDummies;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     Minimal in-process harness: compiles a C# snippet against the running runtime plus the JustDummies core, runs a
///     single analyzer over it, and returns the analyzer diagnostics. Deliberately dependency-free (no
///     Microsoft.CodeAnalysis.Testing) so it composes cleanly with xUnit v3 and NFluent — the same shape as
///     FirstClassErrors' harness.
/// </summary>
internal static class AnalyzerTestHarness {

    private static readonly ImmutableArray<MetadataReference> References = BuildReferences();

    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(DiagnosticAnalyzer analyzer, string source, params string[] enabledDiagnosticIds) {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);
        if (enabledDiagnosticIds.Length > 0) {
            // Force otherwise opt-in (isEnabledByDefault: false) rules on for the test, as an .editorconfig would.
            ImmutableDictionary<string, ReportDiagnostic>.Builder specific = ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>();
            foreach (string id in enabledDiagnosticIds) { specific[id] = ReportDiagnostic.Warn; }
            options = options.WithSpecificDiagnosticOptions(specific.ToImmutable());
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "JustDummies.Analyzers.TestSnippet",
            syntaxTrees: new[] { syntaxTree },
            references: References,
            options: options);

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static ImmutableArray<MetadataReference> BuildReferences() {
        List<MetadataReference> references = new();

        // Reference the running runtime's assemblies so snippets resolve System types without pinning a ref pack.
        string trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator)) {
            if (string.IsNullOrEmpty(path)) { continue; }
            try {
                references.Add(MetadataReference.CreateFromFile(path));
            } catch {
                // Skip any native or otherwise unloadable entry in the TPA list.
            }
        }

        // The JustDummies core, so Any.Reproducibly / Any.ReproduciblyAsync resolve inside the snippet.
        references.Add(MetadataReference.CreateFromFile(typeof(Any).Assembly.Location));

        // The xUnit adapter and xUnit itself, so [Reproducible], [Fact], [Theory] and TheoryData resolve in the
        // snippets the lifecycle rules are tested against.
        references.Add(MetadataReference.CreateFromFile(typeof(JustDummies.Xunit.ReproducibleAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(FactAttribute).Assembly.Location));

        return references.ToImmutableArray();
    }

}
