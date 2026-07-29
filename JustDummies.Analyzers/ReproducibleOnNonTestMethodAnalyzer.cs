using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers;

/// <summary>
///     JD010 — reports <c>[Reproducible]</c> on a method xUnit never treats as a test. The adapter's hooks are
///     collected from the test method, its class and the assembly only, so an attribute on a helper — or on a method
///     whose <c>[Fact]</c> was removed during a refactor — pins nothing and reports nothing. It is invisible when it
///     works (a passing test stays silent by design), so nothing else can tell the two apart.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReproducibleOnNonTestMethodAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ReproducibleOnNonTestMethod);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.ReproducibleAttribute is null || symbols.FactAttribute is null) { return; }

        context.RegisterSymbolAction(symbolContext => Analyze(symbolContext, symbols), SymbolKind.Method);
    }

    private static void Analyze(SymbolAnalysisContext context, KnownSymbols symbols) {
        IMethodSymbol method = (IMethodSymbol)context.Symbol;

        foreach (AttributeData attribute in method.GetAttributes()) {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.ReproducibleAttribute)) { continue; }
            if (XunitFacts.IsTestMethod(method, symbols.FactAttribute!)) { return; }

            Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                             ?? method.Locations[0];

            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ReproducibleOnNonTestMethod, location, method.Name));

            return;
        }
    }

}
