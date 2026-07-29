#region Usings declarations

using System.Globalization;
using System.Text;

#endregion

namespace JustDummies;

/// <summary>
///     The immutable specification behind <see cref="AnyString" />: length bounds, anchored fragments (prefix,
///     suffix, contained values), a character set, a letter casing, an optional allow-list (<c>OneOf</c>) and
///     excluded values — each remembering the constraint that set it, so a conflict message can name both sides.
///     Every mutation returns a new specification and cross-validates the whole eagerly: an <see cref="AnyString" />
///     that exists can always generate — save for an exclusion tight enough to leave a <i>shaped</i> string
///     unsatisfiable, the one failure deferred to generation (see remarks).
/// </summary>
/// <remarks>
///     <para>
///         Without an allow-list the specification is <b>constructive</b>: a generated string is laid out as
///         <c>prefix + filler + contained values + filler + suffix</c>, without overlap analysis, so the length budget
///         the fragments require is the plain sum of their lengths. A combination that only a cleverer overlapping
///         layout could satisfy is reported as a conflict — a deliberate V1 simplification, kept explicit in the
///         conflict messages.
///     </para>
///     <para>
///         With an allow-list the specification is a <b>filter</b> instead: the caller supplied the values, so nothing
///         is laid out and every other constraint is answered by testing each pooled value. The layout budget
///         therefore does not apply — <c>Containing("ab").Containing("ba")</c> accepts the pooled <c>"aba"</c>, which
///         the constructive path could not have built — and satisfiability is the plain question "does any pooled
///         value survive every declared constraint?", answered eagerly at declaration. Exclusions are eager too on
///         that path, since the domain is finite and enumerable.
///     </para>
///     <para>
///         Exclusions (<c>DifferentFrom</c>/<c>Except</c>) on a <i>shaped</i> string are the one constraint not met by
///         construction: strings are not ordinal-mapped, so an excluded value is avoided by a <b>bounded</b> redraw of
///         the constructive layout — expected collisions are ≈ 0 for any non-trivial shape, the same bounded escape a
///         distinct collection uses to skip a duplicate. An exclusion tight enough to leave the shape unsatisfiable
///         (for example excluding every character a single-character length allows) is therefore the one case that
///         surfaces at generation, as a seed-bearing <see cref="AnyGenerationException" />, rather than eagerly at
///         declaration.
///     </para>
///     <para>
///         The default spread governs every draw, bounded or not (ADR-0050): a declared maximum composes with it
///         rather than replacing it, so an upper bound only narrows the draw and never widens it. Only a minimum, an
///         exact length or required fragments enlarge a string.
///     </para>
/// </remarks>
internal sealed class StringSpec {

    private const int DefaultLengthSpread = 16;

    // Bounded escape for exclusions: even the tightest realistic satisfiable shape — a single free character in a
    // ~60-value pool with all but one value excluded — is found with overwhelming probability well within this many
    // draws, while a genuinely unsatisfiable exclusion fails fast. Mirrors the fixed floor of the collection dedup draw.
    private const int ExclusionRedrawBudget = 10_000;

    #region Statics members declarations

    internal static readonly StringSpec Unconstrained = new(null, null, 0, null, null, null,
                                                            null, null, null, null, [],
                                                            null, null, null, null, null, [],
                                                            null, null);

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Characters(int count) {
        return count == 1 ? "1 character" : $"{V(count)} characters";
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<string>? _allowed;
    private readonly string?                _allowedConstraint;
    private readonly LetterCasing?          _casing;
    private readonly string?                _casingConstraint;
    private readonly CharacterSet?          _charset;
    private readonly string?                _charsetConstraint;
    private readonly string?                _customPool;
    private readonly List<string>?          _effectiveAllowed;
    private readonly int?                   _exactLength;
    private readonly string?                _exactConstraint;
    private readonly IReadOnlyList<string>  _excluded;
    private readonly IReadOnlyList<(string Constraint, string[] Values)> _exclusions;
    private readonly IReadOnlyList<string>  _fragments;
    private readonly int?                   _maxLength;
    private readonly string?                _maxConstraint;
    private readonly int                    _minLength;
    private readonly string?                _minConstraint;
    private readonly string?                _prefix;
    private readonly string?                _prefixConstraint;
    private readonly string?                _suffix;
    private readonly string?                _suffixConstraint;

    #endregion

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
                                                     Justification =
                                                         "This private constructor carries the engine's whole immutable state: the 'constrain once, draw many' design rebuilds the spec on " +
                                                         "every With* call, so every field has to be threaded through it. A parameter object would only rename the same list, and the " +
                                                         "constructor is private — no caller ever writes this argument list.")]
    private StringSpec(int?    exactLength, string? exactConstraint,
                       int     minLength,   string? minConstraint,
                       int?    maxLength,   string? maxConstraint,
                       string? prefix,      string? prefixConstraint,
                       string? suffix,      string? suffixConstraint,
                       IReadOnlyList<string> fragments,
                       CharacterSet? charset, string? charsetConstraint, string? customPool,
                       LetterCasing? casing,  string? casingConstraint,
                       IReadOnlyList<(string Constraint, string[] Values)> exclusions,
                       IReadOnlyList<string>? allowed, string? allowedConstraint) {
        _exactLength       = exactLength;
        _exactConstraint   = exactConstraint;
        _minLength         = minLength;
        _minConstraint     = minConstraint;
        _maxLength         = maxLength;
        _maxConstraint     = maxConstraint;
        _prefix            = prefix;
        _prefixConstraint  = prefixConstraint;
        _suffix            = suffix;
        _suffixConstraint  = suffixConstraint;
        _fragments         = fragments;
        _charset           = charset;
        _charsetConstraint = charsetConstraint;
        _customPool        = customPool;
        _casing            = casing;
        _casingConstraint  = casingConstraint;
        _exclusions        = exclusions;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        // The flat, deduplicated value list drives the redraw and the exhaustion message; the provenance in
        // _exclusions is consulted only when a conflict message must name the excluding constraint. Materialized
        // once here — "constrain once, draw many" — in first-declared order.
        _excluded = exclusions.SelectMany(pair => pair.Values).Distinct(StringComparer.Ordinal).ToList();
        // Same "constrain once, draw many" rule for the allow-list: the surviving pool is the exact domain the draw
        // samples, the cardinality a distinct collection gates on, and the set a satisfiability check counts.
        if (allowed is not null) { _effectiveAllowed = allowed.Where(Admits).ToList(); }
    }

    /// <summary>Fixes the exact length; declared once per generator.</summary>
    internal StringSpec WithExactLength(int length, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_exactConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_exactConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_exactConstraint} is already defined."); }

        StringSpec candidate = new(length, applying, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Tightens the minimum length; a looser bound than the current one is a no-op.</summary>
    internal StringSpec WithMinLength(int length, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (length <= _minLength) { return this; }

        StringSpec candidate = new(_exactLength, _exactConstraint, length, applying, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Tightens the maximum length; a looser bound than the current one is a no-op.</summary>
    internal StringSpec WithMaxLength(int length, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (_maxLength is not null && length >= _maxLength) { return this; }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, length, applying,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Anchors a prefix; declared once per generator.</summary>
    internal StringSpec WithPrefix(string prefix, string applying) {
        if (prefix is null) { throw new ArgumentNullException(nameof(prefix)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_prefixConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_prefixConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_prefixConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   prefix, applying, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Anchors a suffix; declared once per generator.</summary>
    internal StringSpec WithSuffix(string suffix, string applying) {
        if (suffix is null) { throw new ArgumentNullException(nameof(suffix)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_suffixConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_suffixConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_suffixConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, suffix, applying, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Adds a value the generated string must contain.</summary>
    internal StringSpec WithFragment(string fragment, string applying) {
        if (fragment is null) { throw new ArgumentNullException(nameof(fragment)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        List<string> fragments = [.. _fragments, fragment];

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Restricts the character family; declared once per generator.</summary>
    internal StringSpec WithCharset(CharacterSet charset, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_charsetConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_charsetConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_charsetConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   charset, applying, _customPool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>
    ///     Restricts the filler to an explicit character pool — the general form of the named character sets.
    ///     Occupies the charset slot (declared once, and mutually exclusive with the named sets) and, because the
    ///     pool is the whole character definition, cannot combine with a casing. The pool is expected to be
    ///     distinct already.
    /// </summary>
    internal StringSpec WithCharPool(string pool, string applying) {
        if (pool is null) { throw new ArgumentNullException(nameof(pool)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_charsetConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_charsetConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_charsetConstraint} is already defined."); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_casingConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_casingConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_casingConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, applying, pool, _casing, _casingConstraint, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Imposes a letter casing; declared once per generator.</summary>
    internal StringSpec WithCasing(LetterCasing casing, string applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_casingConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_casingConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_casingConstraint} is already defined."); }
        if (_customPool is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_charsetConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, casing, applying, _exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Adds values the generated string must avoid; may be declared several times, the exclusions accumulate.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with LINQ expressions",
                                                     Justification =
                                                         "The condition reads the collection the body mutates. Where is lazily evaluated, so lifting the filter out would run each " +
                                                         "predicate against a snapshot taken before the additions it is meant to see, and let duplicates through.")]
    internal StringSpec WithExcluded(IReadOnlyList<string> values, string applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        // The applied constraint tags its own values, so a conflict message can name the exclusion that actually
        // emptied an allow-list rather than a shape constraint that merely borders it.
        List<(string Constraint, string[] Values)> exclusions = [.. _exclusions, (applying, values.ToArray())];

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, exclusions,
                                   _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>
    ///     Restricts the domain to an explicit allow-list; declared once per generator. From here on the specification
    ///     is a filter over the supplied values rather than a layout to build, so every other constraint — those
    ///     already declared and those declared later — narrows the pool instead of shaping a string.
    /// </summary>
    internal StringSpec WithAllowed(IReadOnlyList<string> values, string applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (string.Equals(_allowedConstraint, applying, StringComparison.Ordinal)) { return this; }
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_allowedConstraint} is already defined."); }

        string[] distinct = values.Distinct(StringComparer.Ordinal).ToArray();

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   distinct, applying);

        return candidate.Validated(applying, this);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce, or <c>null</c> when no allow-list bounds it —
    ///     a shaped string draws from a domain too wide, and too dependent on the layout, to count. Feeds
    ///     <see cref="ICardinalityHint{T}" />, so a distinct collection over a pooled generator fails eagerly.
    /// </summary>
    internal long? Cardinality => _effectiveAllowed?.Count;

    /// <summary>
    ///     Whether <paramref name="value" /> is one the specification could produce — the exact pool
    ///     <see cref="Generate" /> draws from when an allow-list is in force. Without one the answer is <c>false</c>
    ///     for every value: the two <see cref="ICardinalityHint{T}" /> members travel together, and a shaped string
    ///     advertises no cardinality, so a distinct collection never consults membership on that path (it gates on the
    ///     bound alone, then falls back to the bounded dedup draw). Answering "outside" is also the side the interface
    ///     documents as safe — it can only defer, never refuse a satisfiable specification.
    /// </summary>
    internal bool Contains(string value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        return _effectiveAllowed is not null && _effectiveAllowed.Contains(value, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Builds one string satisfying the whole specification. With an allow-list the draw is a uniform pick from
    ///     the surviving pool — every constraint was already applied to it, so there is nothing to redraw. Without
    ///     one the string is laid out directly, never generate-then-retry; the one redraw is to skip an excluded
    ///     value, a bounded escape (expected collisions ≈ 0 for any non-trivial shape) whose exhausted budget is
    ///     reported as the spent budget it is, with the seed to replay.
    /// </summary>
    internal string Generate(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        SeededRandom random = source.Current;
        if (_effectiveAllowed is not null) { return _effectiveAllowed[random.Next(_effectiveAllowed.Count)]; }
        if (_excluded.Count == 0) { return BuildCandidate(random); }

        for (int collisions = 0;;) {
            string candidate = BuildCandidate(random);
            if (!_excluded.Contains(candidate, StringComparer.Ordinal)) { return candidate; }
            if (++collisions >= ExclusionRedrawBudget) { throw Exhausted(source); }
        }
    }

    private string BuildCandidate(SeededRandom random) {
        int required     = RequiredLength();
        int effectiveMin = Math.Max(_minLength, required);
        // A declared maximum composes with the default spread instead of replacing it (ADR-0050): it may only narrow
        // the draw, never widen it, so a loose cap still yields the small unconstrained string. Long arithmetic: a
        // huge required length must saturate instead of overflowing past int.MaxValue.
        long spreadCeiling = (long)effectiveMin + DefaultLengthSpread;
        int  effectiveMax  = (int)Math.Min(_maxLength is int declared ? Math.Min(spreadCeiling, declared) : spreadCeiling, int.MaxValue);
        int  length        = _exactLength ?? random.NextInt32Inclusive(effectiveMin, effectiveMax);

        string pool         = FillerPool();
        int    fillerLength = length - required;
        int    before       = random.NextInt32Inclusive(0, fillerLength);
        int    after        = fillerLength - before;

        StringBuilder builder = new(length);
        if (_prefix is not null) { builder.Append(_prefix); }
        AppendFiller(builder, random, pool, before);
        foreach (string fragment in _fragments) { builder.Append(fragment); }
        AppendFiller(builder, random, pool, after);
        if (_suffix is not null) { builder.Append(_suffix); }

        return builder.ToString();
    }

    private AnyGenerationException Exhausted(RandomSource source) {
        int seed = source.Current.Seed;
        // A string generator draws only from its own source, so the seed replays the run fully — never the partial hint.
        string replay = source.ReplayGuidance(seed);
        // The claim is the budget, not impossibility. The redraw is bounded, so an exhausted budget is overwhelming
        // evidence that almost nothing survives the exclusions — and the usual cause really is a shape with no value
        // left — but it is not a proof: a shape with one free value in a few hundred thousand exhausts the budget
        // most of the time and is still satisfiable. Reporting "unsatisfiable" would send a caller hunting for a
        // contradiction that need not exist.
        string message =
            $"Could not generate a string that satisfies the declared shape while excluding {DescribeExcluded()}: " +
            $"no candidate survived {V(ExclusionRedrawBudget)} draws. The redraw is bounded, so this is an exhausted " +
            "budget rather than a proof that no value remains — though the usual cause is a shape the exclusions " +
            "leave nothing of (excluding every value a fixed short length allows). Loosen the exclusions or widen " +
            "the shape. " +
            replay;

        return new AnyGenerationException(message, seed);
    }

    private string DescribeExcluded() {
        return string.Join(", ", _excluded.Select(value => $"\"{value}\""));
    }

    /// <summary>
    ///     Cross-validates the whole specification. The layout checks belong to the constructive path only: once an
    ///     allow-list is in force nothing is laid out, so the single satisfiability question is whether any pooled
    ///     value survives every declared constraint. <paramref name="previous" /> is the specification this one was
    ///     derived from — it tells a conflict message which side was already narrowed and which is the new one.
    /// </summary>
    private StringSpec Validated(string applying, StringSpec previous) {
        ValidateLengthBounds(applying);
        if (_allowed is null) {
            ValidateFragmentBudget(applying);
            ValidateFragmentCharacters(applying);

            return this;
        }

        ValidateAllowedSurvives(applying, previous);

        return this;
    }

    private void ValidateLengthBounds(string applying) {
        if (_exactLength is int exact) {
            if (exact < _minLength) {
                throw new ConflictingAnyConstraintException(applying == _exactConstraint
                                                                ? $"Cannot apply {applying} because {_minConstraint} already requires at least {Characters(_minLength)}."
                                                                : $"Cannot apply {applying} because {_exactConstraint} already fixes the length at {V(exact)}.");
            }

            if (_maxLength is int cappedAt && exact > cappedAt) {
                throw new ConflictingAnyConstraintException(applying == _exactConstraint
                                                                ? $"Cannot apply {applying} because {_maxConstraint} already caps the length at {V(cappedAt)}."
                                                                : $"Cannot apply {applying} because {_exactConstraint} already fixes the length at {V(exact)}.");
            }
        }

        if (_maxLength is int max && _minLength > max) {
            throw new ConflictingAnyConstraintException(applying == _maxConstraint
                                                            ? $"Cannot apply {applying} because {_minConstraint} already requires at least {Characters(_minLength)}."
                                                            : $"Cannot apply {applying} because {_maxConstraint} already caps the length at {V(max)}.");
        }
    }

    private void ValidateFragmentBudget(string applying) {
        int required = RequiredLength();
        if (required == 0) { return; }

        (string description, bool several) = DescribeFragments();
        string requires = several ? "require" : "requires";

        if (_exactLength is int exact && required > exact) {
            throw new ConflictingAnyConstraintException(applying == _exactConstraint
                                                            ? $"Cannot apply {applying} because {description} already {requires} {Characters(required)}."
                                                            : $"Cannot apply {applying} because {_exactConstraint} allows only {Characters(exact)} while {description} {requires} {V(required)}.");
        }

        if (_maxLength is int max && required > max) {
            throw new ConflictingAnyConstraintException(applying == _maxConstraint
                                                            ? $"Cannot apply {applying} because {description} already {requires} {Characters(required)}."
                                                            : $"Cannot apply {applying} because {_maxConstraint} allows at most {Characters(max)} while {description} {requires} {V(required)}.");
        }
    }

    private void ValidateFragmentCharacters(string applying) {
        foreach ((string kind, string fragment) in Fragments()) {
            char? offendingCharacter = FirstDisallowedCharacter(fragment);
            if (offendingCharacter is char outside) {
                throw new ConflictingAnyConstraintException(applying == _charsetConstraint
                                                                ? $"Cannot apply {applying} because the {kind} \"{fragment}\" contains '{outside}', which it does not allow."
                                                                : $"Cannot apply {applying} because {_charsetConstraint} does not allow its character '{outside}'.");
            }

            if (_casing is LetterCasing casing) {
                char? offending = FirstAgainstCasing(fragment, casing);
                if (offending is char against) {
                    string caseName = casing == LetterCasing.Lower ? "uppercase" : "lowercase";
                    throw new ConflictingAnyConstraintException(applying == _casingConstraint
                                                                    ? $"Cannot apply {applying} because the {kind} \"{fragment}\" contains the {caseName} letter '{against}'."
                                                                    : $"Cannot apply {applying} because {_casingConstraint} forbids its {caseName} letter '{against}'.");
                }
            }
        }
    }

    /// <summary>
    ///     Fails when no pooled value survives every declared constraint, with a message naming exactly the two sides
    ///     in play and claiming only what the surviving pools establish.
    /// </summary>
    private void ValidateAllowedSurvives(string applying, StringSpec previous) {
        if (_effectiveAllowed!.Count > 0) { return; }

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {DescribeEmptyPool(applying, previous)}.");
    }

    private string DescribeEmptyPool(string applying, StringSpec previous) {
        // The allow-list is the constraint being applied: the values are new, and the constraints already declared
        // are the other side. Name those that reject every single value — the ones the caller must loosen — and stay
        // generic when it took a combination of them, since no individual constraint is then the culprit.
        if (previous._allowed is null) {
            IReadOnlyList<string> culprits = previous.ConstraintsRejectingAll(_allowed!);
            if (culprits.Count == 0) { return "no value it offers satisfies the constraints already declared"; }
            if (culprits.Count == 1) { return $"{culprits[0]} allows none of its values"; }

            return $"{string.Join(", ", culprits)} allow none of its values";
        }

        // The allow-list was already in force: it is the other side, and the constraint being applied is what
        // emptied it. Qualify only when the applied constraint is not the whole story — it admits some value the
        // allow-list declared, so the emptiness genuinely took the other constraints too. When it admits none of
        // them, loosening the others cannot help, and the qualified form would send the caller at the wrong
        // constraint, so the plain claim is both true and the useful one.
        return _allowed!.Any(AdmittedBy(applying))
                   ? $"no value {previous._allowedConstraint} allows that the other constraints leave satisfies it"
                   : $"no value {previous._allowedConstraint} allows satisfies it";
    }

    /// <summary>
    ///     The declared constraints that reject <b>every</b> value of <paramref name="values" />, in declaration
    ///     order. A constraint some value satisfies is not a culprit — naming it would blame a constraint the caller
    ///     could loosen without changing the verdict.
    /// </summary>
    private IReadOnlyList<string> ConstraintsRejectingAll(IReadOnlyList<string> values) {
        List<string> culprits = [];
        foreach ((string constraint, Func<string, bool> admits) in DeclaredConstraints()) {
            if (!values.Any(admits)) { culprits.Add(constraint); }
        }

        return culprits;
    }

    /// <summary>
    ///     The test a value must pass to satisfy <paramref name="constraint" /> <b>alone</b>. A constraint the
    ///     specification does not carry admits everything, which keeps a message that cannot identify its own
    ///     applied constraint on the weaker, still-true claim rather than the stronger one.
    /// </summary>
    private Func<string, bool> AdmittedBy(string constraint) {
        Func<string, bool>[] tests = DeclaredConstraints()
                                     .Where(entry => string.Equals(entry.Constraint, constraint, StringComparison.Ordinal))
                                     .Select(entry => entry.Admits)
                                     .ToArray();

        return value => tests.All(test => test(value));
    }

    /// <summary>
    ///     Every declared constraint paired with the test a value must pass to satisfy it — the single definition of
    ///     what the specification demands of a value it did not build. It drives the pool filter, the culprit search
    ///     and the blame qualification, so the three can never drift apart.
    /// </summary>
    /// <remarks>
    ///     Entries are grouped by the constraint <b>as the caller wrote it</b>, and a group's tests are conjoined.
    ///     One call can set two internal bounds — <c>WithLengthBetween(2, 3)</c> sets both, under one name — and the
    ///     caller can only loosen the call: judging its halves separately would let a constraint that alone rejects
    ///     every value escape the blame, because each half on its own admits one.
    /// </remarks>
    private IEnumerable<(string Constraint, Func<string, bool> Admits)> DeclaredConstraints() {
        return Declarations()
               .GroupBy(entry => entry.Constraint, StringComparer.Ordinal)
               .Select(group => {
                   Func<string, bool>[] tests = group.Select(entry => entry.Admits).ToArray();

                   return (group.Key, (Func<string, bool>)(value => tests.All(test => test(value))));
               });
    }

    private IEnumerable<(string Constraint, Func<string, bool> Admits)> Declarations() {
        if (_exactLength is int exact) { yield return (_exactConstraint!, value => value.Length == exact); }
        if (_minLength > 0) { yield return (_minConstraint!, value => value.Length >= _minLength); }
        if (_maxLength is int max) { yield return (_maxConstraint!, value => value.Length <= max); }
        if (_prefix is not null) { yield return (_prefixConstraint!, value => value.StartsWith(_prefix, StringComparison.Ordinal)); }
        if (_suffix is not null) { yield return (_suffixConstraint!, value => value.EndsWith(_suffix, StringComparison.Ordinal)); }
        foreach (string fragment in _fragments) {
            // AnyString renders the constraint from the fragment itself, so it is reconstructed identically here.
            yield return ($"Containing(\"{fragment}\")", value => value.IndexOf(fragment, StringComparison.Ordinal) >= 0);
        }
        if (_charsetConstraint is not null) { yield return (_charsetConstraint, value => FirstDisallowedCharacter(value) is null); }
        if (_casing is LetterCasing casing) { yield return (_casingConstraint!, value => FirstAgainstCasing(value, casing) is null); }
        foreach ((string constraint, string[] excluded) in _exclusions) {
            yield return (constraint, value => !excluded.Contains(value, StringComparer.Ordinal));
        }
    }

    /// <summary>Whether <paramref name="value" /> satisfies every declared constraint — the allow-list filter.</summary>
    private bool Admits(string value) {
        foreach ((string _, Func<string, bool> admits) in DeclaredConstraints()) {
            if (!admits(value)) { return false; }
        }

        return true;
    }

    private IEnumerable<(string Kind, string Fragment)> Fragments() {
        if (_prefix is not null) { yield return ("prefix", _prefix); }
        foreach (string fragment in _fragments) { yield return ("contained value", fragment); }
        if (_suffix is not null) { yield return ("suffix", _suffix); }
    }

    private (string Description, bool Several) DescribeFragments() {
        List<string> parts = [];
        if (_prefix is not null) { parts.Add($"the prefix \"{_prefix}\""); }
        foreach (string fragment in _fragments) { parts.Add($"the contained value \"{fragment}\""); }
        if (_suffix is not null) { parts.Add($"the suffix \"{_suffix}\""); }

        return (string.Join(" and ", parts), parts.Count > 1);
    }

    private int RequiredLength() {
        int required = (_prefix?.Length ?? 0) + (_suffix?.Length ?? 0);
        foreach (string fragment in _fragments) { required += fragment.Length; }

        return required;
    }

    private char? FirstDisallowedCharacter(string fragment) {
        if (_customPool is not null) {
            foreach (char character in fragment) {
                if (_customPool.IndexOf(character) < 0) { return character; }
            }

            return null;
        }

        return _charset is CharacterSet charset ? FirstOutsideCharset(fragment, charset) : null;
    }

    private static char? FirstOutsideCharset(string fragment, CharacterSet charset) {
        foreach (char character in fragment) {
            bool allowed = charset switch {
                CharacterSet.Alpha        => CharacterPools.IsAsciiLetter(character),
                CharacterSet.Numeric      => CharacterPools.IsAsciiDigit(character),
                CharacterSet.AlphaNumeric => CharacterPools.IsAsciiLetter(character) || CharacterPools.IsAsciiDigit(character),
                _                         => true
            };
            if (!allowed) { return character; }
        }

        return null;
    }

    /// <summary>
    ///     The first character of <paramref name="fragment" /> the declared casing forbids. The test is the Unicode
    ///     one, not an ASCII range: the constructive filler is ASCII, but an anchored fragment and a pooled value are
    ///     the caller's own text, so an accented or non-Latin letter must be judged on its actual case rather than
    ///     waved through — the constraint says "every alphabetic character", and a generator must not emit a value
    ///     that violates the constraint it was given.
    /// </summary>
    private static char? FirstAgainstCasing(string fragment, LetterCasing casing) {
        foreach (char character in fragment) {
            if (casing == LetterCasing.Lower && char.IsUpper(character)) { return character; }
            if (casing == LetterCasing.Upper && char.IsLower(character)) { return character; }
        }

        return null;
    }

    private static void AppendFiller(StringBuilder builder, SeededRandom random, string pool, int count) {
        for (int i = 0; i < count; i++) {
            builder.Append(pool[random.Next(pool.Length)]);
        }
    }

    private string FillerPool() {
        if (_customPool is not null) { return _customPool; }

        string letters = _casing switch {
            LetterCasing.Lower => CharacterPools.LowerLetters,
            LetterCasing.Upper => CharacterPools.UpperLetters,
            _                  => CharacterPools.UpperLetters + CharacterPools.LowerLetters
        };

        return _charset switch {
            CharacterSet.Alpha   => letters,
            CharacterSet.Numeric => CharacterPools.Digits,
            _                    => letters + CharacterPools.Digits
        };
    }

}
