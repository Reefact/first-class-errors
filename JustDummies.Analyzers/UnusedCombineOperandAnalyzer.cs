using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD027 — reports a <c>Combine</c> operand whose value never reaches the composed result, because the composer
///     lambda never reads the parameter it is bound to.
/// </summary>
/// <remarks>
///     The operand is still drawn: <c>Combine</c> generates every part before calling the composer, so the constraints
///     are built, the conflict checks run, and the value is dropped on the floor. Nothing fails — the composed value is
///     well-formed and simply does not carry the part the call site says it carries. Naming the parameter <c>_</c> is
///     the acknowledgement that switches the rule off, the same escape hatch C# already gives for a discard.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedCombineOperandAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UnusedCombineOperand);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name != "Combine") { return; }
        if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, symbols.Any)) { return; }
        if (invocation.Arguments.Length < 3) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        // The composer is the last argument, and it has to be a lambda written here: a method group's body is not
        // this compilation's to read, so an operand it ignores is not knowable.
        IArgumentOperation composerArgument = invocation.Arguments[invocation.Arguments.Length - 1];
        if (GeneratorFacts.Unwrap(composerArgument.Value) is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation composer }) { return; }

        int operandCount = invocation.Arguments.Length - 1;
        if (composer.Symbol.Parameters.Length != operandCount) { return; }
        if (ComposesNothing(composer)) { return; }

        HashSet<ISymbol?> read = ReadParameters(composer);

        for (int index = 0; index < operandCount; index++) {
            IParameterSymbol parameter = composer.Symbol.Parameters[index];

            // '_' is how C# spells "I know, and I mean it" — for a discard parameter and for one merely named like
            // one. Either way the author has said the draw is deliberate.
            if (parameter.Name is "_" or "") { continue; }
            if (read.Contains(parameter)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.UnusedCombineOperand, invocation.Arguments[index].Value.Syntax.GetLocation(), parameter.Name));
        }
    }

    // A composer whose whole body is a throw reads no parameter, and that is not an ignored operand: it composes
    // nothing at all, on purpose, which is how a test exercises the failure path Combine wraps. Found by dogfooding
    // this rule on the library's own suite, where the arity-8 case is written exactly that way.
    //
    // The spellings are one shape seen from several places in the tree. An expression-bodied '=> throw ...' is a
    // Return CARRYING the throw — and carrying it through a conversion to the composer's result type, so the throw is
    // not the returned operation but the operand under it. Both facts had to be measured; guessing either one wrong
    // let the very site that motivated this guard through.
    private static bool ComposesNothing(IAnonymousFunctionOperation composer) {
        foreach (IOperation statement in composer.Body.Operations) {
            if (statement is IThrowOperation or IExpressionStatementOperation { Operation: IThrowOperation }) { return true; }
            if (statement is IReturnOperation { ReturnedValue: { } returned } && GeneratorFacts.Unwrap(returned) is IThrowOperation) { return true; }
        }

        return false;
    }

    // Every parameter the composer's body reads, including through a nested lambda that captures it.
    private static HashSet<ISymbol?> ReadParameters(IAnonymousFunctionOperation composer) {
        HashSet<ISymbol?> read = new(SymbolEqualityComparer.Default);

        foreach (IOperation descendant in composer.Body.Descendants()) {
            if (descendant is IParameterReferenceOperation reference) { read.Add(reference.Parameter); }
        }

        return read;
    }

}
