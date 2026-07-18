#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

#endregion

namespace FirstClassErrors.RequestBinder.PropertyTests;

#region Request DTO and value object

/// <summary>A request DTO carrying a single list property — the surface the list-binding invariants bind against.</summary>
internal sealed record TokenListRequest(IReadOnlyList<string?>? Tokens);

/// <summary>
///     A minimal value object: valid exactly when the raw token is a non-empty run of letters or digits. It
///     deliberately rejects whitespace, so a generated token can be made valid or invalid on demand — which is what
///     lets a property know, before binding, which elements the binder must reject.
/// </summary>
internal sealed class Token {

    private Token(string value) {
        Value = value;
    }

    public string Value { get; }

    public static Outcome<Token> Parse(string raw) {
        return raw.Length > 0 && raw.All(char.IsLetterOrDigit)
                   ? Outcome<Token>.Success(new Token(raw))
                   : Outcome<Token>.Failure(TokenError.Invalid(raw));
    }

}

#endregion

#region Error factories

/// <summary>The leaf error a token fails to parse with.</summary>
internal static class TokenError {

    private static readonly ErrorCode Code = ErrorCode.Create("PROP_TOKEN_INVALID");

    public static DomainError Invalid(string raw) {
        return DomainError.Create(Code, $"'{raw}' is not a valid token.")
                          .WithPublicMessage("The token is invalid.");
    }

}

/// <summary>The envelope the binder groups every recorded violation into.</summary>
internal static class CommandError {

    private static readonly ErrorCode Code = ErrorCode.Create("PROP_COMMAND_INVALID");

    public static PrimaryPortError Invalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(Code, "The command is invalid.", violations)
                               .WithPublicMessage("The request is invalid.");
    }

}

#endregion

#region Generators

/// <summary>
///     Generates lists of token slots for the property-based tests. A slot pairs the raw value a request would
///     carry with whether that value must fail to bind — a classification that is a pure function of a generated
///     integer, so the expected outcome (which elements fail, and at which index) is known before binding.
/// </summary>
internal static class BinderGen {

    #region Statics members declarations

    /// <summary>
    ///     Classifies an arbitrary integer into a token slot: one third bind as a valid token, one third as an
    ///     invalid (whitespace-bearing) token, one third as a <c>null</c> element. Both invalid and null are failing
    ///     slots — the binder records exactly one error for each.
    /// </summary>
    public static (string? Raw, bool Fails) ToSlot(int seed) {
        int nonNegative = seed & int.MaxValue;

        return (nonNegative % 3) switch {
                   0 => ("t" + nonNegative, false),  // letters and digits only → binds
                   1 => ("t " + nonNegative, true),  // embeds a space          → REQUEST_ARGUMENT_INVALID
                   _ => ((string?)null, true)        // absent element          → REQUEST_ARGUMENT_REQUIRED
               };
    }

    /// <summary>
    ///     Generates a list of token slots of arbitrary length — the empty list included — each element
    ///     independently classified as valid, invalid, or null.
    /// </summary>
    public static Gen<(string? Raw, bool Fails)[]> Slots() {
        return ArbMap.Default.GeneratorFor<int[]>().Select(seeds => seeds.Select(ToSlot).ToArray());
    }

    #endregion

}

#endregion
