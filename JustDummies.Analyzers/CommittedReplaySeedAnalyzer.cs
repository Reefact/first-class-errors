using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD019 — reports a replay seed pinned in committed code. The seeded overloads exist to <b>replay</b> a run a
///     failure reported: correct while you are reproducing, wrong the moment it is committed, because the test then
///     draws the same values for ever and stops surfacing the coupling the library exists to reveal.
/// </summary>
/// <remarks>
///     Opt-in, and it must be. This repository's own maintainer guide instructs the opposite for a whole class of
///     tests — "Pin a seed for anything statistical" — so a rule enabled by default would fight documented practice.
///     It earns its keep as a pre-release sweep, not as a standing check.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommittedReplaySeedAnalyzer : DiagnosticAnalyzer {

    private const string SeedPropertyName = "Seed";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CommittedReplaySeed);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null) { return; }

        context.RegisterOperationAction(operationContext => AnalyzeInvocation(operationContext, symbols), OperationKind.Invocation);

        if (symbols.ReproducibleAttribute is not null) {
            context.RegisterSymbolAction(symbolContext => AnalyzeAttribute(symbolContext, symbols.ReproducibleAttribute), SymbolKind.Method, SymbolKind.NamedType);
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name is not ("Reproducibly" or "ReproduciblyAsync" or "UseSeed" or "WithSeed")) { return; }
        if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, symbols.Any)) { return; }

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.Parameter?.Name != "seed") { continue; }
            if (argument.ArgumentKind == ArgumentKind.DefaultValue) { continue; }
            if (!ConstantFacts.TryGetInt32(argument.Value, out int seed)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(Descriptors.CommittedReplaySeed, argument.Value.Syntax.GetLocation(), seed));

            return;
        }
    }

    private static void AnalyzeAttribute(SymbolAnalysisContext context, INamedTypeSymbol reproducibleAttribute) {
        foreach (AttributeData attribute in context.Symbol.GetAttributes()) {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, reproducibleAttribute)) { continue; }

            foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments) {
                if (named.Key != SeedPropertyName || named.Value.Value is not int seed) { continue; }

                Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                                 ?? context.Symbol.Locations[0];

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.CommittedReplaySeed, location, seed));

                return;
            }
        }
    }

}
