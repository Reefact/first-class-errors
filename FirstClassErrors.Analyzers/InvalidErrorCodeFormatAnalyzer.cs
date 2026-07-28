using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE004 — reports a literal <c>ErrorCode.Create("...")</c> whose code does not follow the UPPER_SNAKE_CASE
///     convention (e.g. <c>MONEY_TRANSFER_INVALID</c>). Convention check, opt-in: disabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidErrorCodeFormatAnalyzer : DiagnosticAnalyzer {

    // The timeout is a ceiling, not a repair: '_' is excluded from '[A-Z0-9]', so the mandatory separator pins the
    // start of every '(_[A-Z0-9]+)' iteration to a position the input alone determines. The decomposition is unique,
    // giving back characters inside one iteration can never open a different one, and the match stays linear — the
    // catastrophic backtracking this rule guards against is unreachable here, and the code is a compile-time constant
    // the developer wrote (ErrorCodeFacts.TryGetNonEmptyLiteralCode), never attacker-supplied input. The ceiling is
    // there so that no later edit of the pattern, and no regex engine in a host compiler we do not control, can turn
    // a per-invocation convention check into a hung build. One second is orders of magnitude above the microseconds a
    // real error code costs, so it can only fire on a genuine pathology; letting the resulting RegexMatchTimeoutException
    // escape is deliberate — Roslyn wraps every analyzer action and surfaces it as AD0001 instead of crashing the
    // compiler, so a catch here would only add unreachable, untestable code.
    private static readonly Regex UpperSnakeCase = new("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.InvalidErrorCodeFormat);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? errorCodeType = context.Compilation.GetTypeByMetadataName(ErrorCodeFacts.ErrorCodeMetadataName);
        if (errorCodeType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, errorCodeType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol errorCodeType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        IOperation? argument = ErrorCodeFacts.GetCreateArgument(invocation, errorCodeType);
        if (argument is null) { return; }

        // Non-literal codes are FCE003's concern, empty ones FCE002's.
        if (!ErrorCodeFacts.TryGetNonEmptyLiteralCode(argument, out string code)) { return; }
        if (UpperSnakeCase.IsMatch(code)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.InvalidErrorCodeFormat, argument.Syntax.GetLocation(), code));
    }

}
