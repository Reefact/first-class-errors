using System.Collections.Immutable;
using System.Reflection;

using FirstClassErrors;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FirstClassErrors.Analyzers.UnitTests;

/// <summary>
///     Minimal in-process harness: compiles a C# snippet against a reference set, runs a single analyzer over it, and
///     returns the analyzer diagnostics. Deliberately dependency-free (no Microsoft.CodeAnalysis.Testing) so it composes
///     cleanly with xUnit v3 and NFluent.
/// </summary>
/// <remarks>
///     <see cref="GetDiagnosticsAsync" /> compiles against the running runtime, the default for the analyzer suite.
///     <see cref="GetDiagnosticsAgainstNet472Async" /> compiles against the .NET Framework 4.7.2 reference assemblies
///     instead — the analyzed code's <i>target framework</i>, not the test's runtime, is what a framework-aware rule
///     reacts to — so a rule that resolves a counterpart from the compilation (FCE021) can be proven silent where that
///     counterpart does not exist for an older framework.
/// </remarks>
internal static class AnalyzerTestHarness {

    private const string Net472ReferenceAssembliesMetadataKey = "Net472ReferenceAssemblies";

    private static readonly ImmutableArray<MetadataReference> BaseReferences = BuildBaseReferences();

    public static Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        DiagnosticAnalyzer analyzer,
        string             source,
        params string[]    enabledDiagnosticIds) {

        return RunAsync(analyzer, source, BaseReferences, enabledDiagnosticIds);
    }

    public static Task<ImmutableArray<Diagnostic>> GetDiagnosticsAgainstNet472Async(
        DiagnosticAnalyzer analyzer,
        string             source,
        params string[]    enabledDiagnosticIds) {

        return RunAsync(analyzer, source, BuildNet472References(), enabledDiagnosticIds);
    }

    private static async Task<ImmutableArray<Diagnostic>> RunAsync(
        DiagnosticAnalyzer                 analyzer,
        string                             source,
        ImmutableArray<MetadataReference>  references,
        string[]                           enabledDiagnosticIds) {

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
            references: references,
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

        AddCore(references);

        return references.ToImmutableArray();
    }

    private static ImmutableArray<MetadataReference> BuildNet472References() {
        List<MetadataReference> references = new();

        // The .NET Framework 4.7.2 reference assemblies (including the netstandard facade, so the netstandard2.0 core
        // resolves). The directory is baked in at build time via the Net472ReferenceAssemblies assembly metadata.
        foreach (string dll in Directory.EnumerateFiles(Net472ReferenceDirectory(), "*.dll", SearchOption.AllDirectories)) {
            try {
                references.Add(MetadataReference.CreateFromFile(dll));
            } catch {
                // Skip anything Roslyn cannot read as a metadata reference.
            }
        }

        AddCore(references);

        return references.ToImmutableArray();
    }

    private static void AddCore(List<MetadataReference> references) {
        // The FirstClassErrors core, so Outcome / ErrorCode / DomainError resolve inside the snippet.
        references.Add(MetadataReference.CreateFromFile(typeof(ErrorCode).Assembly.Location));
    }

    private static string Net472ReferenceDirectory() {
        foreach (AssemblyMetadataAttribute attribute in typeof(AnalyzerTestHarness).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()) {
            if (attribute.Key == Net472ReferenceAssembliesMetadataKey && !string.IsNullOrWhiteSpace(attribute.Value)) {
                return attribute.Value!;
            }
        }

        throw new InvalidOperationException($"The '{Net472ReferenceAssembliesMetadataKey}' assembly metadata was not found; the net472 reference-assemblies package is not wired into the test project.");
    }

}
