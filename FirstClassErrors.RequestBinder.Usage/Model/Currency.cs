namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     An ISO-4217-style currency code — a reference-type value object parsed from a request string. Used to show an
///     <b>optional-with-fallback</b> binding: absent, it falls back to a configured default rather than failing.
/// </summary>
public sealed class Currency : IEquatable<Currency> {

    #region Constructors declarations

    private Currency(string code) {
        Code = code;
    }

    #endregion

    /// <summary>The three-letter, upper-case currency code (for example <c>EUR</c>).</summary>
    public string Code { get; }

    #region Statics members declarations

    /// <summary>
    ///     Parses <paramref name="raw" /> into a <see cref="Currency" />, or fails with a documented
    ///     <see cref="InvalidCurrencyError" />. Kept deliberately shallow (three upper-case letters): a sample checks
    ///     the shape, not the full ISO register.
    /// </summary>
    public static Outcome<Currency> Parse(string raw) {
        if (raw is not { Length: 3 } || !IsThreeUpperCaseLetters(raw)) {
            return Outcome<Currency>.Failure(InvalidCurrencyError.Malformed(raw));
        }

        return Outcome<Currency>.Success(new Currency(raw));
    }

    private static bool IsThreeUpperCaseLetters(string raw) {
        foreach (char c in raw) {
            if (c is < 'A' or > 'Z') { return false; }
        }

        return true;
    }

    #endregion

    /// <inheritdoc />
    public bool Equals(Currency? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return string.Equals(Code, other.Code, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || (obj is Currency other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(Code);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Code;
    }

}
