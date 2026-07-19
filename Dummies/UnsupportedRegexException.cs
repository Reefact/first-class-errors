namespace Dummies;

/// <summary>
///     Thrown when a pattern passed to <see cref="Any.StringMatching(string)" /> is well-formed but uses a construct
///     outside the <b>regular</b> subset the library generates from — a lookahead or lookbehind, a backreference, a
///     Unicode category, a word boundary. These constructs are either not regular (so no finite generator can honour
///     them) or deliberately out of scope; the library refuses to guess rather than silently emit a value that does
///     not actually match. A syntactically malformed pattern is a caller mistake and surfaces as an
///     <see cref="System.ArgumentException" /> instead.
/// </summary>
public sealed class UnsupportedRegexException : AnyException {

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnsupportedRegexException" /> class.
    /// </summary>
    /// <param name="message">A description naming the unsupported construct and where it occurs.</param>
    public UnsupportedRegexException(string message) : base(message) { }

}
