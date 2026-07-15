using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE018 — reports an <c>ErrorContextKey.Create&lt;T&gt;("name", ...)</c> whose value type <c>T</c> is a bulk
///     payload: a byte array, a <see cref="System.IO.Stream" /> (or subtype), or a <see cref="System.IO.FileInfo" />.
///     Error context is meant for small, loggable facts; a whole file or buffer bloats every log line and error-catalog
///     entry and often carries sensitive data with it. Opt-in and disabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OversizedErrorContextValueAnalyzer : DiagnosticAnalyzer {

    private const string StreamMetadataName   = "System.IO.Stream";
    private const string FileInfoMetadataName = "System.IO.FileInfo";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.OversizedErrorContextValue);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? keyType = context.Compilation.GetTypeByMetadataName(ErrorContextKeyFacts.ErrorContextKeyMetadataName);
        if (keyType is null) { return; }

        INamedTypeSymbol? streamType   = context.Compilation.GetTypeByMetadataName(StreamMetadataName);
        INamedTypeSymbol? fileInfoType = context.Compilation.GetTypeByMetadataName(FileInfoMetadataName);

        context.RegisterOperationAction(
            operationContext => Analyze(operationContext, keyType, streamType, fileInfoType),
            OperationKind.Invocation);
    }

    private static void Analyze(
        OperationAnalysisContext context,
        INamedTypeSymbol         keyType,
        INamedTypeSymbol?        streamType,
        INamedTypeSymbol?        fileInfoType) {

        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!ErrorContextKeyFacts.TryGetCreatedKey(invocation, keyType, out IOperation? nameArgument, out ITypeSymbol? valueType)) { return; }
        if (!IsBulkPayload(valueType!, streamType, fileInfoType)) { return; }

        string keyName = ErrorContextKeyFacts.TryGetLiteralName(nameArgument!, out string literal) ? literal : nameArgument!.Syntax.ToString();

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.OversizedErrorContextValue,
            invocation.Syntax.GetLocation(),
            keyName,
            valueType!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static bool IsBulkPayload(ITypeSymbol valueType, INamedTypeSymbol? streamType, INamedTypeSymbol? fileInfoType) {
        if (valueType is IArrayTypeSymbol array && array.ElementType.SpecialType == SpecialType.System_Byte) { return true; }
        if (streamType is not null && SymbolFacts.IsOrInheritsFrom(valueType, streamType)) { return true; }
        if (fileInfoType is not null && SymbolFacts.IsOrInheritsFrom(valueType, fileInfoType)) { return true; }

        return false;
    }

}
