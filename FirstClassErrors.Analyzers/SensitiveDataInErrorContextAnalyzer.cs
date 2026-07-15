using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE017 — reports an <c>ErrorContextKey.Create&lt;T&gt;("name", ...)</c> whose name denotes a secret, credential
///     or piece of personal data (password, token, secret, api key, connection string, credit card, SSN...). Error
///     context is copied into logs and catalogs, so such a value leaks wherever the error travels. This is a name
///     heuristic — it never sees the runtime value — so it is opt-in and disabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SensitiveDataInErrorContextAnalyzer : DiagnosticAnalyzer {

    // Compound, unambiguous terms matched anywhere in the separators-stripped, lower-cased name (e.g. "USER_PASSWORD"
    // and "clientSecret" both collapse to a form containing one of these). Long enough that an accidental hit is rare.
    private static readonly ImmutableArray<string> SensitiveSubstrings = ImmutableArray.Create(
        "password", "passwd", "passphrase", "secret", "apikey", "accesstoken", "refreshtoken",
        "clientsecret", "privatekey", "connectionstring", "creditcard", "cardnumber", "socialsecurity", "credential");

    // Short or ambiguous terms matched only as a whole word (after splitting on separators and camelCase humps), so
    // "pin" does not fire on "endpoint" and "otp" does not fire inside an unrelated identifier.
    private static readonly ImmutableHashSet<string> SensitiveWholeWords = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "pwd", "pin", "ssn", "otp", "cvv", "cvc", "token", "bearer");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.SensitiveDataInErrorContext);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? keyType = context.Compilation.GetTypeByMetadataName(ErrorContextKeyFacts.ErrorContextKeyMetadataName);
        if (keyType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, keyType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol keyType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!ErrorContextKeyFacts.TryGetCreatedKey(invocation, keyType, out IOperation? nameArgument, out _)) { return; }
        if (!ErrorContextKeyFacts.TryGetLiteralName(nameArgument!, out string name)) { return; }
        if (!LooksSensitive(name)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.SensitiveDataInErrorContext, nameArgument!.Syntax.GetLocation(), name));
    }

    private static bool LooksSensitive(string name) {
        string compact = Compact(name);
        foreach (string term in SensitiveSubstrings) {
            if (compact.Contains(term)) { return true; }
        }

        foreach (string word in Tokenize(name)) {
            if (SensitiveWholeWords.Contains(word)) { return true; }
        }

        return false;
    }

    // Lower-cases the name and drops every non-alphanumeric character, so "API_KEY", "ApiKey" and "api-key" all reduce
    // to "apikey" for substring matching.
    private static string Compact(string name) {
        StringBuilder builder = new(name.Length);
        foreach (char character in name) {
            if (char.IsLetterOrDigit(character)) { builder.Append(char.ToLowerInvariant(character)); }
        }

        return builder.ToString();
    }

    // Splits the name into lower-cased words on separators and on lower→upper camelCase humps, so "AuthToken",
    // "USER_PIN" and "otpCode" yield the words {auth, token}, {user, pin} and {otp, code}.
    private static IEnumerable<string> Tokenize(string name) {
        StringBuilder current  = new(name.Length);
        char          previous = '\0';

        foreach (char character in name) {
            if (!char.IsLetterOrDigit(character)) {
                if (current.Length > 0) {
                    yield return current.ToString();
                    current.Clear();
                }

                previous = '\0';

                continue;
            }

            if (current.Length > 0 && char.IsUpper(character) && char.IsLower(previous)) {
                yield return current.ToString();
                current.Clear();
            }

            current.Append(char.ToLowerInvariant(character));
            previous = character;
        }

        if (current.Length > 0) { yield return current.ToString(); }
    }

}
