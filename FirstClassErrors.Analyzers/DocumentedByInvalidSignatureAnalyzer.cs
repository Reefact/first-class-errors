using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE007 — reports a <c>[DocumentedBy("X")]</c> whose target method exists but cannot be used as a documentation
///     factory: it must be static, parameterless and return <c>ErrorDocumentation</c>. A missing target is FCE006's
///     concern, not this one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentedByInvalidSignatureAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DocumentedByInvalidSignature);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.DocumentedByAttribute is null) { return; }

        context.RegisterSymbolAction(symbolContext => Analyze(symbolContext, symbols), SymbolKind.Method);
    }

    private static void Analyze(SymbolAnalysisContext context, KnownSymbols symbols) {
        IMethodSymbol method = (IMethodSymbol)context.Symbol;

        if (!SymbolFacts.TryGetDocumentedBy(method, symbols.DocumentedByAttribute!, out AttributeData? attribute, out string? targetName)) { return; }
        if (string.IsNullOrEmpty(targetName)) { return; }

        ImmutableArray<IMethodSymbol> candidates = method.ContainingType
                                                         .GetMembers(targetName!)
                                                         .OfType<IMethodSymbol>()
                                                         .ToImmutableArray();

        if (candidates.Length == 0) { return; }                                                   // not found → FCE006
        if (candidates.Any(candidate => IsValidDocumentationMethod(candidate, symbols.ErrorDocumentation))) { return; }

        Location location = attribute!.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                            ?? method.Locations.FirstOrDefault()
                            ?? Location.None;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DocumentedByInvalidSignature, location, targetName));
    }

    private static bool IsValidDocumentationMethod(IMethodSymbol candidate, INamedTypeSymbol? errorDocumentationType) {
        if (!candidate.IsStatic) { return false; }
        if (candidate.Parameters.Length != 0) { return false; }
        if (errorDocumentationType is null) { return true; }   // cannot verify the return type; avoid a false positive

        return SymbolFacts.IsOrInheritsFrom(candidate.ReturnType, errorDocumentationType);
    }

}
