using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE013 — reports an example passed to <c>WithExamples(...)</c> that does not invoke any factory of the type
///     declaring the documentation. Examples are meant to expose the documented error's real messages, so each should
///     build that error. Unrecognized example shapes are left alone to avoid false positives.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExampleDoesNotCallDocumentedFactoryAnalyzer : DiagnosticAnalyzer {

    private const string ErrorDocumentationMetadataName = "FirstClassErrors.ErrorDocumentation";
    private const string WithExamplesMethodName         = "WithExamples";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ExampleDoesNotCallDocumentedFactory);

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

        INamedTypeSymbol? documentingType = context.ContainingSymbol.ContainingType;
        if (documentingType is null) { return; }

        foreach (IOperation example in GetExampleOperations(invocation).Where(example => !ExampleInvokesMemberOf(example, documentingType))) {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ExampleDoesNotCallDocumentedFactory, example.Syntax.GetLocation(), documentingType.Name));
        }
    }

    private static IEnumerable<IOperation> GetExampleOperations(IInvocationOperation invocation) {
        if (invocation.Arguments.Length != 1) { return Enumerable.Empty<IOperation>(); }

        IArgumentOperation argument = invocation.Arguments[0];
        if (argument.ArgumentKind != ArgumentKind.ParamArray) { return Enumerable.Empty<IOperation>(); }
        if (argument.Value is not IArrayCreationOperation array || array.Initializer is null) { return Enumerable.Empty<IOperation>(); }

        return array.Initializer.ElementValues;
    }

    private static bool ExampleInvokesMemberOf(IOperation example, INamedTypeSymbol documentingType) {
        IOperation value = example;
        if (value is IConversionOperation conversion) { value = conversion.Operand; }
        if (value is IDelegateCreationOperation delegateCreation) { value = delegateCreation.Target; }

        if (value is IMethodReferenceOperation methodReference) {
            return SymbolEqualityComparer.Default.Equals(methodReference.Method.ContainingType, documentingType);
        }

        if (value is IAnonymousFunctionOperation lambda) {
            return OperationFacts.EnumerateOperations(lambda.Body)
                                 .OfType<IInvocationOperation>()
                                 .Any(call => SymbolEqualityComparer.Default.Equals(call.TargetMethod.ContainingType, documentingType));
        }

        // Unrecognized example shape → do not flag, to avoid false positives.
        return true;
    }

}
