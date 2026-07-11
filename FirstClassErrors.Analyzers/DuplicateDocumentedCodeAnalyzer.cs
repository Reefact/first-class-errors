using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE011 — reports when more than one documented factory produces the same error code by referencing the same
///     <c>ErrorCode</c> field. Documentation extraction groups by code and keeps a single entry, so the others collapse
///     silently. This complements FCE001, which only sees duplicate <c>ErrorCode.Create</c> literals.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateDocumentedCodeAnalyzer : DiagnosticAnalyzer {

    private const string CreateMethodName = "Create";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DuplicateDocumentedCode);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Error is null || symbols.DocumentedByAttribute is null) { return; }

        ConcurrentDictionary<ISymbol, ConcurrentBag<Location>> usagesByCodeField = new(SymbolEqualityComparer.Default);

        context.RegisterOperationBlockAction(blockContext => Collect(blockContext, symbols, usagesByCodeField));
        context.RegisterCompilationEndAction(endContext => Report(endContext, usagesByCodeField));
    }

    private static void Collect(
        OperationBlockAnalysisContext                          context,
        KnownSymbols                                           symbols,
        ConcurrentDictionary<ISymbol, ConcurrentBag<Location>> usagesByCodeField) {

        if (context.OwningSymbol is not IMethodSymbol method) { return; }
        if (!SymbolFacts.HasAttribute(method, symbols.DocumentedByAttribute!)) { return; }

        // The outermost error-factory Create identifies the produced code.
        IInvocationOperation? create = FindErrorFactoryCreate(context.OperationBlocks, symbols);
        if (create is null) { return; }

        if (TryGetCodeField(create.Arguments[0].Value, out ISymbol? codeField)) {
            usagesByCodeField.GetOrAdd(codeField!, _ => new ConcurrentBag<Location>())
                             .Add(method.Locations.FirstOrDefault() ?? Location.None);
        }
    }

    /// <summary>Finds the first <c>Error.Create(...)</c> invocation carrying at least one argument, across the blocks.</summary>
    private static IInvocationOperation? FindErrorFactoryCreate(ImmutableArray<IOperation> blocks, KnownSymbols symbols) {
        foreach (IOperation block in blocks) {
            foreach (IOperation operation in OperationFacts.EnumerateOperations(block)) {
                IInvocationOperation? create = AsErrorFactoryCreate(operation, symbols);
                if (create is not null) { return create; }
            }
        }

        return null;
    }

    /// <summary>Returns <paramref name="operation" /> as an <c>Error.Create(...)</c> invocation with arguments, or <c>null</c>.</summary>
    private static IInvocationOperation? AsErrorFactoryCreate(IOperation operation, KnownSymbols symbols) {
        if (operation is not IInvocationOperation invocation) { return null; }
        if (invocation.TargetMethod.Name != CreateMethodName) { return null; }
        if (!SymbolFacts.IsOrInheritsFrom(invocation.TargetMethod.ContainingType, symbols.Error!)) { return null; }

        return invocation.Arguments.Length == 0 ? null : invocation;
    }

    private static bool TryGetCodeField(IOperation codeArgument, out ISymbol? codeField) {
        IOperation value = codeArgument is IConversionOperation conversion ? conversion.Operand : codeArgument;

        if (value is IFieldReferenceOperation fieldReference) {
            codeField = fieldReference.Field;

            return true;
        }

        codeField = null;

        return false;
    }

    private static void Report(
        CompilationAnalysisContext                             context,
        ConcurrentDictionary<ISymbol, ConcurrentBag<Location>> usagesByCodeField) {

        foreach (KeyValuePair<ISymbol, ConcurrentBag<Location>> entry in usagesByCodeField) {
            Location[] locations = entry.Value.ToArray();
            if (locations.Length < 2) { continue; }

            foreach (Location location in locations) {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.DuplicateDocumentedCode, location, entry.Key.Name));
            }
        }
    }

}
