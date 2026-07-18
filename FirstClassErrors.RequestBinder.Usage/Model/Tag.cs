namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     A free-form booking tag (for example <c>vip</c> or <c>late-checkout</c>) — a reference-type value object parsed
///     from each element of a request list. Used to show binding a <b>list of simple properties</b>, where every
///     failing element is reported under its own indexed path.
/// </summary>
public sealed class Tag : IEquatable<Tag> {

    #region Constructors declarations

    private Tag(string value) {
        Value = value;
    }

    #endregion

    /// <summary>The validated tag.</summary>
    public string Value { get; }

    #region Statics members declarations

    /// <summary>
    ///     Parses <paramref name="raw" /> into a <see cref="Tag" />, or fails with a documented
    ///     <see cref="InvalidTagError" />. A tag is a single non-empty token without whitespace.
    /// </summary>
    public static Outcome<Tag> Parse(string raw) {
        if (string.IsNullOrEmpty(raw) || raw.Length > 32 || ContainsWhitespace(raw)) {
            return Outcome<Tag>.Failure(InvalidTagError.Malformed(raw));
        }

        return Outcome<Tag>.Success(new Tag(raw));
    }

    private static bool ContainsWhitespace(string raw) {
        foreach (char c in raw) {
            if (char.IsWhiteSpace(c)) { return true; }
        }

        return false;
    }

    #endregion

    /// <inheritdoc />
    public bool Equals(Tag? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || (obj is Tag other && Equals(other));
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
