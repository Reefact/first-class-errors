using System.Collections.Immutable;

using FirstClassErrors;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FirstClassErrors.Analyzers.UnitTests;

/// <summary>
///     Minimal in-process harness: compiles a C# snippet against the running runtime plus the FirstClassErrors core,
///     runs a single analyzer over it, and returns the analyzer diagnostics. Deliberately dependency-free (no
///     Microsoft.CodeAnalysis.Testing) so it composes cleanly with xUnit v3 and NFluent.
/// </summary>
internal static class AnalyzerTestHarness {

    private static readonly ImmutableArray<MetadataReference> BaseReferences = BuildBaseReferences();

    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        DiagnosticAnalyzer  analyzer,
        string              source,
        params string[]     enabledDiagnosticIds) {

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);
        if (enabledDiagnosticIds.Length > 0) {
            // Force otherwise opt-in (isEnabledByDefault: false) rules on for the test, as an .editorconfig would.
            ImmutableDictionary<string, ReportDiagnostic>.Builder specific = ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>();
            foreach (string id in enabledDiagnosticIds) { specific[id] = ReportDiagnostic.Info; }
            options = options.WithSpecificDiagnosticOptions(specific.ToImmutable());
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "FirstClassErrors.Analyzers.TestSnippet",
            syntaxTrees: new[] { syntaxTree },
            references: BaseReferences,
            options: options);

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static ImmutableArray<MetadataReference> BuildBaseReferences() {
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

        // The FirstClassErrors core, so ErrorCode / DomainError / DescribeError resolve inside the snippet.
        references.Add(MetadataReference.CreateFromFile(typeof(ErrorCode).Assembly.Location));

        return references.ToImmutableArray();
    }

}
