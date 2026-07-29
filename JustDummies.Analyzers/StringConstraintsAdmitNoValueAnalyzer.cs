using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD015 — reports an <c>AnyString</c> chain whose constant constraints admit no value: an anchored fragment
///     holding a character the declared character family forbids, or fragments that cannot fit the declared length.
/// </summary>
/// <remarks>
///     This is the case ADR-0035 names by hand as the one an analyzer should carry and the type system cannot:
///     <c>Numeric().StartingWith("ORD-")</c> conflicts while <c>Numeric().StartingWith("123")</c> does not, from
///     identical call sites and identical static types. Only the argument's value tells them apart, which is exactly
///     what makes it value-dependent — and what puts it on the analyzer's side of the ADR's line.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringConstraintsAdmitNoValueAnalyzer : DiagnosticAnalyzer {

    private const string UpperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerLetters = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits       = "0123456789";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.StringConstraintsAdmitNoValue);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        // Analyse each chain once, from its outermost call.
        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || factory.TargetMethod.Name != "String") { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        string?                            pool        = null;
        string?                            poolName    = null;
        bool                               requireUpper = false;
        bool                               requireLower = false;
        bool                               hasValueSet  = false;
        List<(string Text, IOperation At)> fragments   = [];
        int?                               fixedLength = null;
        int?                               maximum     = null;

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                case "Alpha":         pool = UpperLetters + LowerLetters;          poolName = "Alpha()";         break;
                case "Numeric":       pool = Digits;                               poolName = "Numeric()";       break;
                case "AlphaNumeric":  pool = UpperLetters + LowerLetters + Digits; poolName = "AlphaNumeric()";  break;

                // Casing is not a character set: it constrains the CASE of a fragment's letters and says nothing
                // about its other characters. UpperCase().StartingWith("ORD-") is legal — the '-' is not a letter —
                // while UpperCase().StartingWith("abc") is not.
                case "UpperCase": requireUpper = true; break;
                case "LowerCase": requireLower = true; break;

                // A terminal value set changes what the fragments are checked against: they are matched against the
                // pooled values rather than laid out side by side, so the length budget below no longer applies.
                case "OneOf": hasValueSet = true; break;

                case "WithChars" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string declared):
                    pool     = declared;
                    poolName = $"WithChars(\"{declared}\")";

                    break;

                case "StartingWith" or "EndingWith" or "Containing" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string fragment):
                    fragments.Add((fragment, constraint.Arguments[0].Value));

                    break;

                case "WithLength" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int length):
                    fixedLength = length;

                    break;

                case "WithMaxLength" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int max):
                    maximum = maximum is null ? max : System.Math.Min(maximum.Value, max);

                    break;
            }
        }

        if (ReportCharacterOutsidePool(context, pool, poolName, fragments)) { return; }
        if (ReportLetterAgainstCasing(context, requireUpper, requireLower, fragments)) { return; }
        if (hasValueSet) { return; }

        ReportLengthBudget(context, fragments, fixedLength, maximum);
    }

    private static bool ReportLetterAgainstCasing(OperationAnalysisContext context, bool requireUpper, bool requireLower, List<(string Text, IOperation At)> fragments) {
        if (!requireUpper && !requireLower) { return false; }

        foreach ((string text, IOperation at) in fragments) {
            foreach (char character in text) {
                if (!char.IsLetter(character)) { continue; }

                bool offends = requireUpper ? char.IsLower(character) : char.IsUpper(character);
                if (!offends) { continue; }

                string constraint = requireUpper ? "UpperCase()" : "LowerCase()";
                string wrongCase  = requireUpper ? "lowercase" : "uppercase";

                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.StringConstraintsAdmitNoValue, at.Syntax.GetLocation(),
                    $"{constraint} forbids the {wrongCase} letter '{character}'"));

                return true;
            }
        }

        return false;
    }

    private static bool ReportCharacterOutsidePool(OperationAnalysisContext context, string? pool, string? poolName, List<(string Text, IOperation At)> fragments) {
        if (pool is null || pool.Length == 0) { return false; }

        foreach ((string text, IOperation at) in fragments) {
            foreach (char character in text) {
                if (pool.IndexOf(character) >= 0) { continue; }

                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.StringConstraintsAdmitNoValue, at.Syntax.GetLocation(),
                    $"'{character}' is not a character {poolName} can draw"));

                return true;
            }
        }

        return false;
    }

    private static void ReportLengthBudget(OperationAnalysisContext context, List<(string Text, IOperation At)> fragments, int? fixedLength, int? maximum) {
        if (fragments.Count == 0) { return; }

        int required = fragments.Sum(fragment => fragment.Text.Length);
        int? cap     = fixedLength ?? maximum;
        if (cap is null || required <= cap.Value) { return; }

        string capName = fixedLength is not null ? $"WithLength({fixedLength})" : $"WithMaxLength({maximum})";

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, fragments[fragments.Count - 1].At.Syntax.GetLocation(),
            $"the anchored fragments need at least {required} characters, which {capName} cannot hold"));
    }

}
