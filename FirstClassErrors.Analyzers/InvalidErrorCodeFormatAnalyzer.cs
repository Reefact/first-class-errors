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

    private static readonly Regex UpperSnakeCase = new("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$", RegexOptions.CultureInvariant);

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
