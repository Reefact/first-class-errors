using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE020 — reports an <c>Outcome.Try&lt;..., TException&gt;</c> whose caught type is (or derives from) a protocol
///     failure such as <c>HttpRequestException</c>, <c>WebException</c>, <c>SocketException</c> or <c>DbException</c>.
///     These failures carry status or protocol data beyond the exception, so a dedicated adapter that inspects the result
///     keeps information <c>Try</c> would discard. Opt-in and disabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryCatchesRichProtocolExceptionAnalyzer : DiagnosticAnalyzer {

    private static readonly string[] ProtocolExceptionMetadataNames = {
        "System.Net.Http.HttpRequestException",
        "System.Net.WebException",
        "System.Net.Sockets.SocketException",
        "System.Data.Common.DbException",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.TryCatchesRichProtocolException);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? outcomeType = context.Compilation.GetTypeByMetadataName(OutcomeTryFacts.OutcomeMetadataName);
        if (outcomeType is null) { return; }

        ImmutableArray<INamedTypeSymbol> protocolTypes = ResolveProtocolTypes(context.Compilation);
        if (protocolTypes.IsEmpty) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, outcomeType, protocolTypes), OperationKind.Invocation);
    }

    private static ImmutableArray<INamedTypeSymbol> ResolveProtocolTypes(Compilation compilation) {
        ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (string metadataName in ProtocolExceptionMetadataNames) {
            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(metadataName);
            if (type is not null) { builder.Add(type); }
        }

        return builder.ToImmutable();
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol outcomeType, ImmutableArray<INamedTypeSymbol> protocolTypes) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!OutcomeTryFacts.TryGetCaughtExceptionType(invocation, outcomeType, out ITypeSymbol? caughtType)) { return; }

        foreach (INamedTypeSymbol protocolType in protocolTypes) {
            if (!SymbolFacts.IsOrInheritsFrom(caughtType!, protocolType)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.TryCatchesRichProtocolException,
                invocation.Syntax.GetLocation(),
                caughtType!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

            return;
        }
    }

}
