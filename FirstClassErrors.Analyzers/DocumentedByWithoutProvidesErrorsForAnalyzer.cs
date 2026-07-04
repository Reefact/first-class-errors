using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE008 — reports a type that declares <c>[DocumentedBy]</c> factories but is missing <c>[ProvidesErrorsFor]</c>.
///     Documentation extraction only scans types carrying <c>[ProvidesErrorsFor]</c>, so every documented error on such
///     a type is silently ignored. Reported once per type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentedByWithoutProvidesErrorsForAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DocumentedByWithoutProvidesErrorsFor);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.DocumentedByAttribute is null || symbols.ProvidesErrorsForAttribute is null) { return; }

        context.RegisterSymbolAction(symbolContext => Analyze(symbolContext, symbols), SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context, KnownSymbols symbols) {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Class) { return; }
        if (SymbolFacts.HasAttribute(type, symbols.ProvidesErrorsForAttribute!)) { return; }

        bool hasDocumentedFactory = type.GetMembers()
                                        .OfType<IMethodSymbol>()
                                        .Any(method => SymbolFacts.HasAttribute(method, symbols.DocumentedByAttribute!));
        if (!hasDocumentedFactory) { return; }

        Location location = type.Locations.FirstOrDefault() ?? Location.None;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DocumentedByWithoutProvidesErrorsFor, location, type.Name));
    }

}
