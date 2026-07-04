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

        foreach (IOperation block in context.OperationBlocks) {
            foreach (IOperation operation in EnumerateOperations(block)) {
                if (operation is not IInvocationOperation invocation) { continue; }
                if (invocation.TargetMethod.Name != CreateMethodName) { continue; }
                if (!SymbolFacts.IsOrInheritsFrom(invocation.TargetMethod.ContainingType, symbols.Error!)) { continue; }
                if (invocation.Arguments.Length == 0) { continue; }

                if (TryGetCodeField(invocation.Arguments[0].Value, out ISymbol? codeField)) {
                    usagesByCodeField.GetOrAdd(codeField!, _ => new ConcurrentBag<Location>())
                                     .Add(method.Locations.FirstOrDefault() ?? Location.None);
                }

                return; // the outermost error-factory Create identifies the produced code
            }
        }
    }

    // Pre-order (ancestor-before-descendant) walk over the operation tree; the outermost Error.Create is reached
    // before any inner-error Create it may wrap.
    private static IEnumerable<IOperation> EnumerateOperations(IOperation root) {
        Stack<IOperation> pending = new();
        pending.Push(root);

        while (pending.Count > 0) {
            IOperation current = pending.Pop();
            yield return current;

            foreach (IOperation child in current.ChildOperations) {
                pending.Push(child);
            }
        }
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
