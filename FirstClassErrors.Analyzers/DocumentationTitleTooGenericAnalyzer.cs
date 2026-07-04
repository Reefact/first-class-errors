using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE015 — reports a <c>WithTitle("...")</c> whose literal title is one of a small denylist of empty phrases
///     (Error, Invalid value, Failure…) that describe nothing. Opt-in: disabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentationTitleTooGenericAnalyzer : DiagnosticAnalyzer {

    private const string ErrorDescriptionStageMetadataName = "FirstClassErrors.IErrorDescriptionStage";
    private const string WithTitleMethodName               = "WithTitle";

    private static readonly ImmutableHashSet<string> GenericTitles = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "Error", "Invalid", "Invalid value", "Failure", "Failed", "Unknown", "Unknown error", "Bad", "Problem", "Something went wrong");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DocumentationTitleTooGeneric);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? descriptionStageType = context.Compilation.GetTypeByMetadataName(ErrorDescriptionStageMetadataName);
        if (descriptionStageType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, descriptionStageType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol descriptionStageType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != WithTitleMethodName) { return; }
        if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, descriptionStageType)) { return; }
        if (invocation.Arguments.Length != 1) { return; }

        if (invocation.Arguments[0].Value.ConstantValue.Value is not string title) { return; }
        if (!GenericTitles.Contains(title.Trim())) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DocumentationTitleTooGeneric, invocation.Arguments[0].Value.Syntax.GetLocation(), title));
    }

}
