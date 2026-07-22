#region Usings declarations

using System.Globalization;
using System.Text;

#endregion

namespace Dummies;

/// <summary>
///     The immutable specification behind <see cref="AnyString" />: length bounds, anchored fragments (prefix,
///     suffix, contained values), a character set, a letter casing and excluded values — each remembering the
///     constraint that set it, so a conflict message can name both sides. Every mutation returns a new specification
///     and cross-validates the whole eagerly: an <see cref="AnyString" /> that exists can always generate — save for
///     an exclusion tight enough to leave the shape unsatisfiable, the one failure deferred to generation (see remarks).
/// </summary>
/// <remarks>
///     <para>
///         Fragments are laid out without overlap analysis: a generated string is
///         <c>prefix + filler + contained values + filler + suffix</c>, so the length budget the fragments require is
///         the plain sum of their lengths. A combination that only a cleverer overlapping layout could satisfy is
///         reported as a conflict — a deliberate V1 simplification, kept explicit in the conflict messages.
///     </para>
///     <para>
///         Exclusions (<c>DifferentFrom</c>/<c>Except</c>) are the one constraint not met by construction: strings are
///         not ordinal-mapped, so an excluded value is avoided by a <b>bounded</b> redraw of the constructive layout —
///         expected collisions are ≈ 0 for any non-trivial shape, the same bounded escape a distinct collection uses to
///         skip a duplicate. An exclusion tight enough to leave the shape unsatisfiable (for example excluding every
///         character a single-character length allows) is therefore the one case that surfaces at generation, as a
///         seed-bearing <see cref="AnyGenerationException" />, rather than eagerly at declaration.
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
                                                            null, null, null, null, null, []);

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Characters(int count) {
        return count == 1 ? "1 character" : $"{V(count)} characters";
    }

    #endregion

    #region Fields declarations

    private readonly LetterCasing?         _casing;
    private readonly string?               _casingConstraint;
    private readonly CharacterSet?         _charset;
    private readonly string?               _charsetConstraint;
    private readonly string?               _customPool;
    private readonly int?                  _exactLength;
    private readonly string?               _exactConstraint;
    private readonly IReadOnlyList<string> _excluded;
    private readonly IReadOnlyList<string> _fragments;
    private readonly int?                  _maxLength;
    private readonly string?               _maxConstraint;
    private readonly int                   _minLength;
    private readonly string?               _minConstraint;
    private readonly string?               _prefix;
    private readonly string?               _prefixConstraint;
    private readonly string?               _suffix;
    private readonly string?               _suffixConstraint;

    #endregion

    private StringSpec(int?    exactLength, string? exactConstraint,
                       int     minLength,   string? minConstraint,
                       int?    maxLength,   string? maxConstraint,
                       string? prefix,      string? prefixConstraint,
                       string? suffix,      string? suffixConstraint,
                       IReadOnlyList<string> fragments,
                       CharacterSet? charset, string? charsetConstraint, string? customPool,
                       LetterCasing? casing,  string? casingConstraint,
                       IReadOnlyList<string> excluded) {
        _exactLength      = exactLength;
        _exactConstraint  = exactConstraint;
        _excluded         = excluded;
        _minLength        = minLength;
        _minConstraint    = minConstraint;
        _maxLength        = maxLength;
        _maxConstraint    = maxConstraint;
        _prefix           = prefix;
        _prefixConstraint = prefixConstraint;
        _suffix           = suffix;
        _suffixConstraint = suffixConstraint;
        _fragments        = fragments;
        _charset          = charset;
        _charsetConstraint = charsetConstraint;
        _customPool       = customPool;
        _casing           = casing;
        _casingConstraint = casingConstraint;
    }

    /// <summary>
    ///     Whether no constraint has been declared yet — the pristine state a generator from <see cref="Any.String" />
    ///     starts in, before any fluent constraint narrows it. Used to keep a terminal constraint (<c>OneOf</c>) from
    ///     being combined with the shaping ones (an exclusion counts as a declared constraint).
    /// </summary>
    internal bool IsUnconstrained =>
        _exactLength is null && _minLength == 0 && _maxLength is null &&
        _prefix is null && _suffix is null && _fragments.Count == 0 &&
        _charset is null && _casing is null && _excluded.Count == 0 && _customPool is null;

    /// <summary>Fixes the exact length; declared once per generator.</summary>
    internal StringSpec WithExactLength(int length, string applying) {
        if (_exactConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_exactConstraint} is already defined."); }

        StringSpec candidate = new(length, applying, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Tightens the minimum length; a looser bound than the current one is a no-op.</summary>
    internal StringSpec WithMinLength(int length, string applying) {
        if (length <= _minLength) { return this; }

        StringSpec candidate = new(_exactLength, _exactConstraint, length, applying, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Tightens the maximum length; a looser bound than the current one is a no-op.</summary>
    internal StringSpec WithMaxLength(int length, string applying) {
        if (_maxLength is not null && length >= _maxLength) { return this; }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, length, applying,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Anchors a prefix; declared once per generator.</summary>
    internal StringSpec WithPrefix(string prefix, string applying) {
        if (_prefixConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_prefixConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   prefix, applying, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Anchors a suffix; declared once per generator.</summary>
    internal StringSpec WithSuffix(string suffix, string applying) {
        if (_suffixConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_suffixConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, suffix, applying, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Adds a value the generated string must contain.</summary>
    internal StringSpec WithFragment(string fragment, string applying) {
        List<string> fragments = new(_fragments) { fragment };

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Restricts the character family; declared once per generator.</summary>
    internal StringSpec WithCharset(CharacterSet charset, string applying) {
        if (_charsetConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_charsetConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   charset, applying, _customPool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>
    ///     Restricts the filler to an explicit character pool — the general form of the named character sets.
    ///     Occupies the charset slot (declared once, and mutually exclusive with the named sets) and, because the
    ///     pool is the whole character definition, cannot combine with a casing. The pool is expected to be
    ///     distinct already.
    /// </summary>
    internal StringSpec WithCharPool(string pool, string applying) {
        if (_charsetConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_charsetConstraint} is already defined."); }
        if (_casingConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_casingConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, applying, pool, _casing, _casingConstraint, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Imposes a letter casing; declared once per generator.</summary>
    internal StringSpec WithCasing(LetterCasing casing, string applying) {
        if (_casingConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_casingConstraint} is already defined."); }
        if (_customPool is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_charsetConstraint} is already defined."); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, casing, applying, _excluded);

        return candidate.Validated(applying);
    }

    /// <summary>Adds values the generated string must avoid; may be declared several times, the exclusions accumulate.</summary>
    internal StringSpec WithExcluded(IReadOnlyList<string> values, string applying) {
        List<string> excluded = new(_excluded);
        foreach (string value in values) {
            if (!excluded.Contains(value, StringComparer.Ordinal)) { excluded.Add(value); }
        }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, excluded);

        return candidate.Validated(applying);
    }

    /// <summary>
    ///     Builds one string satisfying the whole specification — laid out directly, never generate-then-retry. The one
    ///     redraw is to skip an excluded value: a bounded escape (expected collisions ≈ 0 for any non-trivial shape); an
    ///     exhausted budget means the exclusions leave the shape unsatisfiable, reported with the seed to replay.
    /// </summary>
    internal string Generate(RandomSource source) {
        Random random = source.Current.Random;
        if (_excluded.Count == 0) { return BuildCandidate(random); }

        for (int collisions = 0;;) {
            string candidate = BuildCandidate(random);
            if (!_excluded.Contains(candidate, StringComparer.Ordinal)) { return candidate; }
            if (++collisions >= ExclusionRedrawBudget) { throw Exhausted(source); }
        }
    }

    private string BuildCandidate(Random random) {
        int required     = RequiredLength();
        int effectiveMin = Math.Max(_minLength, required);
        // Long arithmetic: a huge declared minimum must saturate instead of overflowing past int.MaxValue.
        int effectiveMax = _maxLength ?? (int)Math.Min((long)effectiveMin + DefaultLengthSpread, int.MaxValue);
        int length       = _exactLength ?? random.NextInt32Inclusive(effectiveMin, effectiveMax);

        string pool         = FillerPool();
        int    fillerLength = length - required;
        int    before       = random.Next(fillerLength + 1);
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
        string replay = source.ReplayHint(seed);
        string message =
            $"Could not generate a string that satisfies the declared shape while excluding {DescribeExcluded()}: " +
            $"no candidate survived {V(ExclusionRedrawBudget)} draws, so the exclusions leave the shape unsatisfiable " +
            "(for example excluding every value a fixed short length allows). Loosen the exclusions or widen the shape. " +
            replay;

        return new AnyGenerationException(message, seed);
    }

    private string DescribeExcluded() {
        return string.Join(", ", _excluded.Select(value => $"\"{value}\""));
    }

    private static void AppendFiller(StringBuilder builder, Random random, string pool, int count) {
        for (int i = 0; i < count; i++) {
            builder.Append(pool[random.Next(pool.Length)]);
        }
    }

    private StringSpec Validated(string applying) {
        ValidateLengthBounds(applying);
        ValidateFragmentBudget(applying);
        ValidateFragmentCharacters(applying);

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

    private IEnumerable<(string Kind, string Fragment)> Fragments() {
        if (_prefix is not null) { yield return ("prefix", _prefix); }
        foreach (string fragment in _fragments) { yield return ("contained value", fragment); }
        if (_suffix is not null) { yield return ("suffix", _suffix); }
    }

    private (string Description, bool Several) DescribeFragments() {
        List<string> parts = new();
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

    private static char? FirstAgainstCasing(string fragment, LetterCasing casing) {
        foreach (char character in fragment) {
            if (casing == LetterCasing.Lower && character is >= 'A' and <= 'Z') { return character; }
            if (casing == LetterCasing.Upper && character is >= 'a' and <= 'z') { return character; }
        }

        return null;
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
