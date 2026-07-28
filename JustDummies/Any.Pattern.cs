#region Usings declarations

using System.Text.RegularExpressions;

#endregion

namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Starts a generator of arbitrary strings that <b>match <paramref name="pattern" /></b>, drawing from the
    ///     ambient random context. The pattern is the whole shape of the specification — the returned generator carries
    ///     no further shape or length constraints; express those inside the pattern. It does carry the exclusion pair
    ///     <c>Except</c>/<c>DifferentFrom</c>, which rejects rather than constructs, and it composes through
    ///     <c>As(...)</c>, <c>OrNull()</c>, <c>Combine(...)</c> and the collection generators.
    /// </summary>
    /// <remarks>
    ///     Supported is the <b>regular</b> subset of the pattern language: literals and escapes (metacharacters,
    ///     control characters, <c>\xHH</c>, <c>\uHHHH</c>), the shorthands <c>\d \D \w \W \s \S</c>, character classes
    ///     (ranges, negation), the quantifiers <c>? * + {n} {n,} {n,m}</c> (an unbounded quantifier draws its minimum
    ///     plus 0 to 8 repetitions), alternation, grouping (capturing, non-capturing and named), the dot, and the
    ///     anchors <c>^ $</c> at the start and end of the pattern or of a top-level alternation branch (no-ops there,
    ///     since a whole matching string is generated). Wherever the pattern leaves a character free, values are drawn
    ///     from printable ASCII (<c>\s</c> may also yield a tab); a character the pattern names explicitly is emitted as
    ///     written, control characters included. A well-formed but
    ///     non-regular or not-generatable construct — a lookaround, a backreference, a word boundary, a Unicode
    ///     category, an atomic group, a class subtraction, an anchor placed where it could never match — raises an
    ///     <see cref="UnsupportedRegexException" />; a malformed pattern raises an <see cref="ArgumentException" />,
    ///     mirroring what the real engine rejects.
    /// </remarks>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public static AnyPattern StringMatching(string pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        return AnyPattern.FromPattern(AmbientRandomSource.Instance, pattern, ignoreCase: false);
    }

    /// <summary>
    ///     Starts a generator of arbitrary strings matching <paramref name="pattern" /> — the same contract as
    ///     <see cref="StringMatching(string)" />, taking a compiled <see cref="Regex" /> so a test can reuse the very
    ///     object its production code validates with. <see cref="RegexOptions.IgnoreCase" /> is honoured.
    ///     <see cref="RegexOptions.IgnorePatternWhitespace" /> changes how the pattern text itself is read and is
    ///     rejected; the remaining options do not change which strings the pattern matches and are ignored.
    /// </summary>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern, or carries <see cref="RegexOptions.IgnorePatternWhitespace" />.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public static AnyPattern StringMatching(Regex pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }
        if ((pattern.Options & RegexOptions.IgnorePatternWhitespace) != 0) { throw new ArgumentException("RegexOptions.IgnorePatternWhitespace changes how the pattern text is read; pass the pattern without it (or with its whitespace and comments removed).", nameof(pattern)); }

        return AnyPattern.FromPattern(AmbientRandomSource.Instance, pattern.ToString(), (pattern.Options & RegexOptions.IgnoreCase) != 0);
    }

}
