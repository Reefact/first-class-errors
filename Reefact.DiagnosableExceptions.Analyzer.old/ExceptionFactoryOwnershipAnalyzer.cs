#region Usings declarations

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#endregion

namespace Reefact.DiagnosableExceptions.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class ExceptionFactoryOwnershipAnalyzer : DiagnosticAnalyzer {

    #region Statics members declarations

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invoke) { return; }

        IMethodSymbol method = invoke.TargetMethod;

        // Must be static
        if (!method.IsStatic) { return; }

        // Only public or internal
        if (!(method.DeclaredAccessibility == Accessibility.Public || method.DeclaredAccessibility == Accessibility.Internal)) { return; }
        // Must return an Exception
        if (!IsExceptionType(method.ReturnType)) { return; }

        // Containing type must have [ExceptionOf]
        INamedTypeSymbol? exceptionType = method.ContainingType;
        AttributeData? exceptionOfAttr = exceptionType
                                        .GetAttributes()
                                        .FirstOrDefault(a => a.AttributeClass?.Name == nameof(ErrorForAttribute));
        if (exceptionOfAttr is null) { return; }

        // Extract owner type
        ITypeSymbol? ownerType = exceptionOfAttr.ConstructorArguments[0].Value as ITypeSymbol;
        if (ownerType is null) { return; }

        // Caller type
        INamedTypeSymbol? callerType = invoke.SemanticModel?.GetEnclosingSymbol(invoke.Syntax.SpanStart)?.ContainingType;
        if (callerType is null) { return; }

        // If caller is not the owner => violation
        if (SymbolEqualityComparer.Default.Equals(callerType, ownerType)) { return; }

        Diagnostic diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.FactoryMustBeCalledByOwner,
            invoke.Syntax.GetLocation(),
            method.Name,
            ownerType.ToDisplayString(),
            callerType.ToDisplayString());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsExceptionType(ITypeSymbol type) {
        ITypeSymbol? typeSymbol = type;
        while (typeSymbol != null) {
            if (typeSymbol.ToString() == "System.Exception") { return true; } // typeof(Exception).Name ?

            typeSymbol = typeSymbol.BaseType;
        }

        return false;
    }

    #endregion

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.FactoryMustBeCalledByOwner);

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // IOperation = API cross-language, aucun besoin de syntax VB
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

}