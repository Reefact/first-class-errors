using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE001 — reports the same error code created by more than one <c>ErrorCode.Create("X")</c> literal in the
///     compilation. <c>ErrorCode</c> is compared by value, so a duplicated code yields equal instances that silently
///     collapse two distinct errors into one identity (documentation extraction and dictionary lookups keep a single
///     entry). Detection is per-compilation (cross-assembly duplicates are not seen here — they surface only as a
///     documentation-pipeline warning) and limited to literal codes — a non-literal code is FCE003's concern.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateErrorCodeAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DuplicateErrorCode);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? errorCodeType = context.Compilation.GetTypeByMetadataName(ErrorCodeFacts.ErrorCodeMetadataName);
        if (errorCodeType is null) { return; }

        // Per-compilation state; ErrorCode registers with ordinal comparison, so duplicates are case-sensitive.
        ConcurrentDictionary<string, ConcurrentBag<Location>> occurrences = new(StringComparer.Ordinal);

        context.RegisterOperationAction(operationContext => Collect(operationContext, errorCodeType, occurrences), OperationKind.Invocation);
        context.RegisterCompilationEndAction(endContext => Report(endContext, occurrences));
    }

    private static void Collect(
        OperationAnalysisContext                              context,
        INamedTypeSymbol                                      errorCodeType,
        ConcurrentDictionary<string, ConcurrentBag<Location>> occurrences) {

        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        IOperation? argument = ErrorCodeFacts.GetCreateArgument(invocation, errorCodeType);
        if (argument is null) { return; }

        // Non-literal codes are FCE003's concern, empty ones FCE002's; only real codes can collide.
        if (!ErrorCodeFacts.TryGetNonEmptyLiteralCode(argument, out string code)) { return; }

        occurrences.GetOrAdd(code, _ => new ConcurrentBag<Location>()).Add(argument.Syntax.GetLocation());
    }

    private static void Report(
        CompilationAnalysisContext                            context,
        ConcurrentDictionary<string, ConcurrentBag<Location>> occurrences) {

        foreach (KeyValuePair<string, ConcurrentBag<Location>> entry in occurrences) {
            Location[] locations = entry.Value.ToArray();
            if (locations.Length < 2) { continue; }

            foreach (Location location in locations) {
                IEnumerable<Location> others = locations.Where(other => !other.Equals(location));
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.DuplicateErrorCode, location, others, entry.Key));
            }
        }
    }

}
