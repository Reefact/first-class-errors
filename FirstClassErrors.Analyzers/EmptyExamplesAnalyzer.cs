using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE012 — reports the terminal <c>WithExamples()</c> call of the documentation DSL when it is given no example
///     factory. The call cannot be skipped (it produces the <c>ErrorDocumentation</c>), but it can be called empty,
///     yielding documentation that shows no realistic message.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyExamplesAnalyzer : DiagnosticAnalyzer {

    private const string ErrorDocumentationMetadataName = "FirstClassErrors.ErrorDocumentation";
    private const string WithExamplesMethodName         = "WithExamples";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyExamples);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? errorDocumentationType = context.Compilation.GetTypeByMetadataName(ErrorDocumentationMetadataName);
        if (errorDocumentationType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, errorDocumentationType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol errorDocumentationType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != WithExamplesMethodName) { return; }
        if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, errorDocumentationType)) { return; }
        if (!IsEmptyParamArray(invocation)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyExamples, invocation.Syntax.GetLocation()));
    }

    private static bool IsEmptyParamArray(IInvocationOperation invocation) {
        if (invocation.Arguments.Length != 1) { return false; }

        IArgumentOperation argument = invocation.Arguments[0];
        if (argument.ArgumentKind != ArgumentKind.ParamArray) { return false; }
        if (argument.Value is not IArrayCreationOperation array) { return false; }

        if (array.Initializer is { } initializer) { return initializer.ElementValues.Length == 0; }

        return array.DimensionSizes.Length == 1 && array.DimensionSizes[0].ConstantValue is { HasValue: true, Value: 0 };
    }

}
