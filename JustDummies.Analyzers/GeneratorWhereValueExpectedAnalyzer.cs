using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD011 — reports a generator reaching a position that accepts <c>object</c>. Generators are reference types, so
///     no conversion stands in the way and none was removed with the implicit ones: the recipe is stored, passed or
///     compared where the drawn value was meant. An assertion helper taking <c>object</c> then checks the recipe —
///     <c>Assert.NotNull(Any.String())</c> is green for ever and asserts nothing — and a theory row built as
///     <c>object[]</c> feeds the generator itself to the code under test.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratorWhereValueExpectedAnalyzer : DiagnosticAnalyzer {

    private const string EqualsMethodName          = "Equals";
    private const string ReferenceEqualsMethodName = "ReferenceEquals";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GeneratorWhereValueExpected);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.IAny is null) { return; }

        INamedTypeSymbol iAny = symbols.IAny;

        context.RegisterOperationAction(operationContext => AnalyzeConversion(operationContext, iAny), OperationKind.Conversion);
        context.RegisterOperationAction(operationContext => AnalyzeEquals(operationContext, iAny), OperationKind.Invocation);
    }

    private static void AnalyzeConversion(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        IConversionOperation conversion = (IConversionOperation)context.Operation;

        if (!conversion.IsImplicit) { return; }
        if (!IsObjectLike(conversion.Type)) { return; }

        IOperation operand = GeneratorFacts.Unwrap(conversion.Operand);
        if (!GeneratorFacts.IsGenerator(operand.Type, iAny)) { return; }

        // A test asserting that the chain throws writes it as the whole body of a lambda argument, which binds to
        // Func<object> rather than Action and so produces a real generator-to-object conversion. Reporting it would
        // fight every throws-assertion in the suite.
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(operand.Syntax)) { return; }

        // Comparing two recipes is a deliberate operation — it is how an immutability test proves a constraint
        // returned a new generator rather than mutating the receiver. Generate() there would destroy the very
        // property under test, so the identity comparisons belong to the Equals branch below, which reports only
        // the mixed comparison.
        if (IsOperandOfAGeneratorComparison(conversion, iAny)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GeneratorWhereValueExpected, operand.Syntax.GetLocation(), operand.Type!.Name));
    }

    // gen.Equals(value) resolves to object.Equals — reference equality against an unrelated object, false for ever.
    private static void AnalyzeEquals(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != EqualsMethodName) { return; }
        if (method.ContainingType?.SpecialType != SpecialType.System_Object) { return; }

        IOperation? receiver = invocation.Instance is null ? null : GeneratorFacts.Unwrap(invocation.Instance);
        if (receiver is null || invocation.Arguments.Length != 1) { return; }

        IOperation argument = GeneratorFacts.Unwrap(invocation.Arguments[0].Value);

        bool receiverIsGenerator = GeneratorFacts.IsGenerator(receiver.Type, iAny);
        bool argumentIsGenerator = GeneratorFacts.IsGenerator(argument.Type, iAny);

        // Comparing two generators is a deliberate identity check — this repository's own immutability tests do it.
        // Only the mixed comparison is the mistake, and only the generator side needs the Generate().
        if (receiverIsGenerator == argumentIsGenerator) { return; }

        IOperation offending = receiverIsGenerator ? receiver : argument;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GeneratorWhereValueExpected, offending.Syntax.GetLocation(), offending.Type!.Name));
    }

    // True when the conversion feeds object.Equals or object.ReferenceEquals and some other operand of that same call
    // is itself a generator — that is, when the call compares two recipes rather than a recipe against a value.
    private static bool IsOperandOfAGeneratorComparison(IConversionOperation conversion, INamedTypeSymbol iAny) {
        if (conversion.Parent is not IArgumentOperation { Parent: IInvocationOperation call }) { return false; }
        if (call.TargetMethod.Name is not (EqualsMethodName or ReferenceEqualsMethodName)) { return false; }
        if (call.TargetMethod.ContainingType?.SpecialType != SpecialType.System_Object) { return false; }

        if (call.Instance is not null && GeneratorFacts.IsGenerator(GeneratorFacts.Unwrap(call.Instance).Type, iAny)) { return true; }

        foreach (IArgumentOperation argument in call.Arguments) {
            if (ReferenceEquals(argument, conversion.Parent)) { continue; }
            if (GeneratorFacts.IsGenerator(GeneratorFacts.Unwrap(argument.Value).Type, iAny)) { return true; }
        }

        return false;
    }

    private static bool IsObjectLike(ITypeSymbol? type) {
        return type is not null && (type.SpecialType == SpecialType.System_Object || type.TypeKind == TypeKind.Dynamic);
    }

}
