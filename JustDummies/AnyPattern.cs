#region Usings declarations

using System.Text.RegularExpressions;

#endregion

namespace JustDummies;

/// <summary>
///     A generator of arbitrary strings that <b>match a regular expression</b> — the dummy for a value whose format is
///     defined by a pattern (an order reference, a SKU, a currency code). The pattern is the whole specification, so
///     this is a <i>terminal</i> generator: unlike <see cref="AnyString" /> it exposes no further shape or length
///     constraints — express those inside the pattern instead. It still composes like any other generator: pipe it
///     through <c>As(...)</c> into a value object, make it optional with <c>OrNull()</c>, or fold it into
///     <c>Combine(...)</c> and the collection generators.
/// </summary>
/// <remarks>
///     <para>
///         The pattern is parsed once, when the generator is created; each <see cref="Generate" /> then walks the
///         parsed tree, drawing every choice and repetition count from the generator's random context — so a run is
///         reproducible under a seed, exactly like every other generator. Wherever the pattern leaves a character
///         free, values are drawn from <b>printable ASCII</b> (<c>\s</c> may also yield a tab); a character the pattern
///         names explicitly is emitted as written, control characters included. Values are built directly rather than
///         generated-and-filtered.
///     </para>
///     <para>
///         A built value is then checked against the real .NET engine and, on the rare miss, redrawn. The structural
///         build mirrors the regular subset of the engine, but a few implementation-defined corners of empty-match
///         handling — a nullable alternative under a quantifier, whose emptiness the engine accepts or refuses
///         depending on branch order and form — cannot be mirrored structurally. Rather than model those corners, the
///         invariant "a generated value matches its pattern" is kept by construction: the check is the last word, so a
///         value the engine would reject is never returned.
///     </para>
///     <para>
///         Only the <b>regular</b> subset of the pattern language is supported (see <see cref="Any.StringMatching(string)" />);
///         a non-regular construct is refused eagerly with an <see cref="UnsupportedRegexException" /> rather than
///         silently mis-generated.
///     </para>
///     <example>
///         <code>
///         string reference = Any.StringMatching(@"^ORD-\d{8}$").Generate();
///         IAny&lt;OrderReference&gt; any = Any.StringMatching(@"^ORD-\d{8}$").As(OrderReference.Create);
///         </code>
///     </example>
/// </remarks>
public sealed class AnyPattern : IAny<string>, IHasRandomSource {

    #region Statics members declarations

    // A nested unbounded quantifier can, in principle, expand super-linearly; this ceiling turns that into a clear
    // AnyGenerationException instead of an out-of-memory. It is far above any realistic format-validation pattern.
    private const int GenerationLimit = 65536;

    // The structural build occasionally produces a value the real engine rejects (see the class remarks). Each build
    // is verified and redrawn on a miss; the cap turns a pattern the generator cannot satisfy at all into a clear
    // error instead of an unbounded loop. A supported pattern matches on the first build save for these rare corners,
    // where a valid value appears within a handful of draws, so the cap is never approached in practice.
    private const int MatchAttemptLimit = 1000;

    // A safety net against catastrophic backtracking while verifying a non-matching draw — a generated value matching
    // its own pattern is near-instant, so this bites only a pathological pattern, which is treated as a miss.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    internal static AnyPattern FromPattern(RandomSource source, string pattern, bool ignoreCase) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        // Parse first: it raises the specific ArgumentException / UnsupportedRegexException for an invalid or
        // unsupported pattern. The verifier Regex is NOT built here — the Lazy<Regex> field below defers it to the
        // first actual need — so a pattern whose generation can never succeed (an unbounded quantifier with a
        // minimum in the billions, say) never pays, or risks, compiling it.
        RegexNode    root    = RegexParser.Parse(pattern, ignoreCase);
        RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

        return new AnyPattern(source, root, pattern, options);
    }

    #endregion

    #region Fields declarations

    private readonly RegexNode    _root;
    private readonly RandomSource _source;

    // Compiled at most once, on first need inside Generate() — see the FromPattern and Generate() remarks.
    // Lazy<T>'s default thread-safety mode guarantees the factory runs exactly once even under concurrent
    // Generate() calls on the same instance (see the "concurrent draws" test); no thread ever sees, or pays for, a
    // second compilation. Anchored with ^(?:…)$ so it decides a full match, and honours only the option the
    // generator itself honoured (IgnoreCase), never the rest of a passed Regex's.
    private readonly Lazy<Regex> _verifier;

    #endregion

    internal AnyPattern(RandomSource source, RegexNode root, string pattern, RegexOptions options) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (root is null) { throw new ArgumentNullException(nameof(root)); }
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        _source   = source;
        _root     = root;
        _verifier = new Lazy<Regex>(() => new Regex("^(?:" + pattern + ")$", options, MatchTimeout));
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <inheritdoc />
    public string Generate() {
        for (int attempt = 0; attempt < MatchAttemptLimit; attempt++) {
            RegexGenerationContext context = new(_source.Current, GenerationLimit);
            _root.Append(context);
            string value = context.Result();

            try {
                // _root.Append above already refuses, via AnyGenerationException, a pattern whose generation can
                // never fit the ceiling — so a pattern like 'a{2147483647,}' never reaches this line, and _verifier
                // is never compiled for it. That matters beyond avoiding needless work: compiling a Regex from a
                // pattern with a quantifier bound that large has been observed to exhaust memory on at least one
                // .NET regex engine implementation, and this class must never risk that for a pattern its own
                // ceiling already refuses cleanly.
                if (_verifier.Value.IsMatch(value)) { return value; }
            } catch (RegexMatchTimeoutException) {
                // Could not decide within the budget; treat as a miss and redraw rather than return it unverified.
            }
        }

        throw new AnyGenerationException(
            $"Generation failed: after {MatchAttemptLimit} attempts, every value the pattern generator built was rejected by the .NET engine for the same pattern. This happens only for a degenerate pattern whose empty-match behaviour the generator cannot mirror; rewrite it with the supported subset, or generate the value another way.");
    }

}
