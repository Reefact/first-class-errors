namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     A guest e-mail address — a reference-type value object parsed from an untrusted request string.
/// </summary>
/// <remarks>
///     Its <see cref="Parse" /> factory returns an <see cref="Outcome{T}" /> and is the method group a binder passes to
///     <c>AsRequired</c> / <c>AsOptionalReference</c>. Like every value object in FirstClassErrors, it is a
///     <c>sealed class</c> with a private constructor, so its validating factory is the single entry point.
/// </remarks>
public sealed class EmailAddress : IEquatable<EmailAddress> {

    #region Constructors declarations

    private EmailAddress(string value) {
        Value = value;
    }

    #endregion

    /// <summary>The validated e-mail address.</summary>
    public string Value { get; }

    #region Statics members declarations

    /// <summary>
    ///     Parses <paramref name="raw" /> into an <see cref="EmailAddress" />, or fails with a documented
    ///     <see cref="InvalidEmailAddressError" />. Intentionally minimal (a single <c>@</c>): a sample validates just
    ///     enough to show the failure path, not to be a production e-mail parser.
    /// </summary>
    public static Outcome<EmailAddress> Parse(string raw) {
        if (string.IsNullOrWhiteSpace(raw) || raw.IndexOf('@') <= 0 || raw.IndexOf('@') != raw.LastIndexOf('@') || raw.EndsWith("@", StringComparison.Ordinal)) {
            return Outcome<EmailAddress>.Failure(InvalidEmailAddressError.Malformed(raw));
        }

        return Outcome<EmailAddress>.Success(new EmailAddress(raw));
    }

    #endregion

    /// <inheritdoc />
    public bool Equals(EmailAddress? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || (obj is EmailAddress other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Value;
    }

}
