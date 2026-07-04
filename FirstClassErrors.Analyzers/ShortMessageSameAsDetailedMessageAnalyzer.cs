using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE014 — reports <c>WithPublicMessage(short, detailed)</c> where the two literal messages are identical. The
///     short message is a public summary and the detailed one an optional public detail; making them equal usually
///     signals a copy-paste.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ShortMessageSameAsDetailedMessageAnalyzer : DiagnosticAnalyzer {

    private const string PublicMessageStageMetadataName = "FirstClassErrors.PublicMessageStage`1";
    private const string WithPublicMessageMethodName    = "WithPublicMessage";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ShortMessageSameAsDetailedMessage);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? publicMessageStageType = context.Compilation.GetTypeByMetadataName(PublicMessageStageMetadataName);
        if (publicMessageStageType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, publicMessageStageType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol publicMessageStageType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != WithPublicMessageMethodName) { return; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, publicMessageStageType)) { return; }
        if (invocation.Arguments.Length != 2) { return; }

        if (invocation.Arguments[0].Value.ConstantValue.Value is not string shortMessage) { return; }
        if (invocation.Arguments[1].Value.ConstantValue.Value is not string detailedMessage) { return; }
        if (!string.Equals(shortMessage, detailedMessage, StringComparison.Ordinal)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.ShortMessageSameAsDetailedMessage, invocation.Syntax.GetLocation()));
    }

}
