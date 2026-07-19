namespace Dummies;

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
///         reproducible under a seed, exactly like every other generator. Values are drawn from <b>printable ASCII</b>
///         and built directly, never generated then filtered.
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

    internal static AnyPattern FromPattern(RandomSource source, string pattern, bool ignoreCase) {
        return new AnyPattern(source, RegexParser.Parse(pattern, ignoreCase));
    }

    #endregion

    #region Fields declarations

    private readonly RegexNode    _root;
    private readonly RandomSource _source;

    #endregion

    internal AnyPattern(RandomSource source, RegexNode root) {
        _source = source;
        _root   = root;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <inheritdoc />
    public string Generate() {
        RegexGenerationContext context = new(_source.Current.Random, GenerationLimit);
        _root.Append(context);

        return context.Result();
    }

}
