#region Usings declarations

using System.Globalization;
using System.Text.RegularExpressions;

#endregion

namespace JustDummies;

/// <summary>
///     A generator of arbitrary strings that <b>match a regular expression</b> — the dummy for a value whose format is
///     defined by a pattern (an order reference, a SKU, a currency code). The pattern is the whole <i>shape</i> of the
///     specification: unlike <see cref="AnyString" /> this generator exposes no further shape or length constraints —
///     express those inside the pattern instead. What it does expose is the exclusion pair <see cref="Except" /> and
///     <see cref="DifferentFrom" />, which every other generator carries. It also composes like any other generator:
///     pipe it through <c>As(...)</c> into a value object, make it optional with <c>OrNull()</c>, or fold it into
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
///         A shape constraint is refused because it would mean building a value in the intersection of two regular
///         languages, which the library has no machinery for. An <b>exclusion</b> asks for nothing of the sort: it
///         never constructs, it rejects. The value is built from the pattern exactly as before and redrawn on a hit —
///         one more predicate inside a loop that already turns. That places it under the exception the library already
///         documents for strings: with no ordinal mapping to build the exclusion into, it is met by a <b>bounded</b>
///         redraw, so an exclusion tight enough to leave nothing surfaces at <see cref="Generate" /> as a seed-bearing
///         <see cref="AnyGenerationException" /> rather than eagerly at declaration.
///     </para>
///     <para>
///         Only the <b>regular</b> subset of the pattern language is supported (see <see cref="Any.StringMatching(string)" />);
///         a non-regular construct is refused eagerly with an <see cref="UnsupportedRegexException" /> rather than
///         silently mis-generated.
///     </para>
///     <example>
///         <code>
///         string reference = Any.StringMatching(@"^ORD-\d{8}$").Generate();
///         string other     = Any.StringMatching(@"^ORD-\d{8}$").DifferentFrom(existing).Generate();
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

    // Bounded escape for exclusions, kept separate from the match budget above so the two failures never borrow each
    // other's evidence: this one counts values the pattern produced and the engine accepted, which an exclusion then
    // rejected. Mirrors the string generator's budget, and a genuinely emptied language fails fast against it.
    private const int ExclusionRedrawBudget = 10_000;

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

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(IReadOnlyList<string> values) {
        return string.Join(", ", values.Select(value => $"\"{value}\""));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<string> _excluded;
    private readonly string                _pattern;
    private readonly RegexNode             _root;
    private readonly RandomSource          _source;

    // Compiled at most once, on first need inside Generate() — see the FromPattern and Generate() remarks.
    // Lazy<T>'s default thread-safety mode guarantees the factory runs exactly once even under concurrent
    // Generate() calls on the same instance (see the "concurrent draws" test); no thread ever sees, or pays for, a
    // second compilation. Anchored with ^(?:…)$ so it decides a full match, and honours only the option the
    // generator itself honoured (IgnoreCase), never the rest of a passed Regex's. Shared, not rebuilt, when an
    // exclusion derives a new generator: the pattern it verifies is unchanged.
    private readonly Lazy<Regex> _verifier;

    #endregion

    internal AnyPattern(RandomSource source, RegexNode root, string pattern, RegexOptions options) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (root is null) { throw new ArgumentNullException(nameof(root)); }
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        _source   = source;
        _root     = root;
        _pattern  = pattern;
        _excluded = [];
        _verifier = new Lazy<Regex>(() => new Regex("^(?:" + pattern + ")$", options, MatchTimeout));
    }

    private AnyPattern(AnyPattern origin, IReadOnlyList<string> excluded) {
        _source   = origin._source;
        _root     = origin._root;
        _pattern  = origin._pattern;
        _verifier = origin._verifier;
        _excluded = excluded;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>
    ///     Requires the generated value to be none of the supplied <paramref name="values" />. May be declared several
    ///     times; the exclusions accumulate. The pattern still builds the value — an exclusion only rejects and
    ///     redraws — so a pattern whose language the exclusions leave nothing of surfaces at <see cref="Generate" />
    ///     as a seed-bearing <see cref="AnyGenerationException" />, never as a declaration-time conflict: the library
    ///     does not enumerate a regular language to prove it empty. Comparison is ordinal, like string equality
    ///     itself, whether or not the pattern ignores case.
    /// </summary>
    /// <param name="values">The values the generated value must differ from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public AnyPattern Except(params string[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element.", nameof(values)); }

        return Excluding(values);
    }

    /// <summary>
    ///     Requires the generated value to differ from <paramref name="value" /> — typically an existing value the
    ///     test already holds, to exercise an inequality path while keeping the format the pattern defines
    ///     (<c>Any.StringMatching(@"^ORD-\d{8}$").DifferentFrom(existing)</c>). Semantically equivalent to
    ///     <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    public AnyPattern DifferentFrom(string value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        return Excluding([value]);
    }

    /// <inheritdoc />
    public string Generate() {
        if (_excluded.Count == 0) { return BuildVerified(); }

        for (int collisions = 0;;) {
            string candidate = BuildVerified();
            if (!_excluded.Contains(candidate, StringComparer.Ordinal)) { return candidate; }
            if (++collisions >= ExclusionRedrawBudget) { throw Exhausted(); }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method",
                                                     Justification =
                                                         "The loop body MUTATES the very list its condition reads: each accepted value is appended to `excluded`, " +
                                                         "so a later duplicate in `values` is rejected against the values already taken. A Where clause would read " +
                                                         "as a filter over a fixed collection, which is precisely what this is not.")]
    private AnyPattern Excluding(IReadOnlyList<string> values) {
        List<string> excluded = [.. _excluded];
        foreach (string value in values) {
            if (!excluded.Contains(value, StringComparer.Ordinal)) { excluded.Add(value); }
        }

        return new AnyPattern(this, excluded);
    }

    /// <summary>Builds one value the .NET engine agrees matches the pattern, redrawing past the rare structural miss.</summary>
    private string BuildVerified() {
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
            $"Generation failed: after {V(MatchAttemptLimit)} attempts, every value the pattern generator built was rejected by the .NET engine for the same pattern. This happens only for a degenerate pattern whose empty-match behaviour the generator cannot mirror; rewrite it with the supported subset, or generate the value another way.");
    }

    private AnyGenerationException Exhausted() {
        int seed = _source.Current.Seed;
        // A pattern generator draws only from its own source, so the seed replays the run fully — never the partial hint.
        string replay = _source.ReplayGuidance(seed);
        // The claim is the budget, not impossibility. The library builds values from the pattern; it never enumerates
        // the language, so it cannot prove one holds no other value. Excluding both words of "^[ab]$" really does
        // empty it — but a pattern with one free value in a few hundred thousand exhausts the same budget and is
        // still satisfiable, so the message states what was established and no more.
        string message =
            $"Could not generate a value matching \"{_pattern}\" while excluding {Join(_excluded)}: no candidate " +
            $"survived {V(ExclusionRedrawBudget)} draws. The redraw is bounded, so this is an exhausted budget rather " +
            "than a proof that the pattern matches no other value — though the usual cause is a pattern the " +
            "exclusions leave nothing of (excluding every word of a language with only a few). Loosen the exclusions " +
            "or widen the pattern. " +
            replay;

        return new AnyGenerationException(message, seed);
    }

}
