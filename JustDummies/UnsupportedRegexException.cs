namespace JustDummies;

/// <summary>
///     Thrown when a pattern passed to <see cref="Any.StringMatching(string)" /> is well-formed but uses a construct
///     outside the <b>regular</b> subset the library generates from — a lookahead or lookbehind, a backreference, a
///     balancing group, a Unicode category, a word boundary. These constructs are either not regular (so no finite generator can honour
///     them) or deliberately out of scope; the library refuses to guess rather than silently emit a value that does
///     not actually match. A syntactically malformed pattern is a caller mistake and surfaces as an
///     <see cref="ArgumentException" /> instead.
/// </summary>
public sealed class UnsupportedRegexException : DummyException {

    #region Statics members declarations

    /// <summary>
    ///     Builds the exception for a construct outside the regular subset the library generates from — a lookaround, a
    ///     backreference, a balancing group, a Unicode category, a word boundary. A grammar limit: the pattern is
    ///     well-formed, and the construct is one no finite generator can honour or one deliberately out of scope.
    /// </summary>
    internal static UnsupportedRegexException OutsideRegularSubset(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "It builds values from the regular subset of the pattern language; lookarounds, backreferences, word boundaries and Unicode categories are outside it. " +
                        "Express the requirement with the supported subset, or generate the value another way.");
    }

    /// <summary>
    ///     Builds the exception for a negated character class that excludes the whole alphabet the library draws from.
    /// </summary>
    /// <remarks>
    ///     A universe limit rather than a grammar limit, which is why it has a name of its own: the class is regular and
    ///     the real engine compiles it, so refusing it as malformed would claim the caller wrote a broken pattern. What
    ///     it excludes is every character JustDummies can produce, and the remedy is about that range rather than about
    ///     the supported subset.
    /// </remarks>
    internal static UnsupportedRegexException EmptyNegatedClass(string pattern, int position) {
        return Sentence(pattern, "a negated character class that excludes every character JustDummies can generate (printable ASCII U+0020 to U+007E)", position,
                        "It draws values from printable ASCII; express the requirement with characters inside that range, or generate the value another way.");
    }

    /// <summary>
    ///     Writes the refusal sentence both factories share, naming the construct, where it occurs, and what to do
    ///     instead.
    /// </summary>
    /// <remarks>
    ///     Private on purpose: it names the grammar of the message, not a failure, so every caller is a named case
    ///     above. Nothing here guards its arguments — building an exception must never throw, or the failure being
    ///     reported is replaced by a failure about reporting it (ADR-0045, which exempts exception types for exactly
    ///     that reason).
    /// </remarks>
    private static UnsupportedRegexException Sentence(string pattern, string construct, int position, string remedy) {
        return new UnsupportedRegexException($"The regular expression pattern \"{pattern}\" uses {construct} at position {position}, which JustDummies cannot generate from. {remedy}");
    }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnsupportedRegexException" /> class.
    /// </summary>
    /// <param name="message">A description naming the unsupported construct and where it occurs.</param>
    public UnsupportedRegexException(string message) : base(message) { }

}
