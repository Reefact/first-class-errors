#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <typeparamref name="TEnum" /> values, drawn uniformly from the enum's
///     <b>declared</b> members — never from undeclared numeric values. Constraints narrow the pool
///     (<see cref="OneOf" />, <see cref="Except" />, <see cref="DifferentFrom" />), and a combination that empties it
///     fails eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both sides.
/// </summary>
/// <remarks>
///     A <see cref="FlagsAttribute">[Flags]</see> enum declares bits meant to be combined, so its <b>valid</b> values
///     are the combinations, not only the declared members: <c>Read | Write</c> is a legitimate value the type never
///     declares. The declared-members default holds for those enums too — it is the only default valid for both enum
///     families, and switching on the attribute would make the draw depend on a type's metadata rather than on what
///     the test wrote. Opt in explicitly with <see cref="AllowingCombinations" /> to widen the draw to every
///     combination.
/// </remarks>
/// <typeparam name="TEnum">The enum type to draw values from.</typeparam>
public sealed class AnyEnum<TEnum> : IAny<TEnum>, IHasRandomSource, ICardinalityHint<TEnum>
    where TEnum : struct, Enum {

    // The ceiling on the number of non-zero declared members AllowingCombinations() will enumerate. The universe is
    // materialized so the draw is exactly uniform over the DISTINCT values and the cardinality hint stays exact
    // (a per-member coin flip is neither: with a declared composite such as ReadWrite = Read | Write, several
    // subsets collapse onto the same value). Enumeration is 2^k, so it needs a bound; beyond it the constraint is
    // refused by name rather than silently degraded into a second, non-uniform regime.
    private const int MaxCombinableMembers = 20;

    #region Statics members declarations

    // The declared-members set and the [Flags] marking of an enum type are process constants; cached once per closed
    // generic type instead of reflecting on every Any.Enum<T>() call.
    private static readonly TEnum[] Declared = ((TEnum[])Enum.GetValues(typeof(TEnum))).Distinct().ToArray();
    private static readonly bool    IsFlags  = typeof(TEnum).IsDefined(typeof(FlagsAttribute), false);

    // The combination universe is a process constant too, but far more expensive than Declared, so it is built on
    // first use instead of on every closed generic type. A race computes it twice and stores the same set: benign.
    private static TEnum[]? _combinations;

    internal static AnyEnum<TEnum> Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (Declared.Length == 0) {
            throw new AnyGenerationException($"Cannot generate an arbitrary {typeof(TEnum).Name} value because the enum declares no members.");
        }

        return new AnyEnum<TEnum>(source, Declared, false, null, null, []);
    }

    /// <summary>
    ///     Every value obtained by OR-ing a non-empty subset of the declared members, plus the zero value when a zero
    ///     member is declared. Taking the declared members as the generating set — rather than the individual bits —
    ///     absorbs declared composites (<c>ReadWrite = Read | Write</c> contributes nothing new) without having to
    ///     decide which members "are" bits, and never invents the zero value for an enum that deliberately declares no
    ///     <c>None</c>.
    /// </summary>
    private static TEnum[] Combinations {
        get {
            if (_combinations is not null) { return _combinations; }

            ulong[]        generators = Declared.Select(ToUInt64).Where(bits => bits != 0UL).ToArray();
            HashSet<ulong> reachable  = [];
            foreach (ulong generator in generators) {
                // Union of what was reachable, what becomes reachable by adding this generator to it, and the
                // generator alone — the OR-closure, built without enumerating the 2^k subsets that collapse.
                foreach (ulong existing in reachable.ToArray()) { reachable.Add(existing | generator); }
                reachable.Add(generator);
            }

            // The empty subset ORs to zero, but that value belongs to the universe only when the enum defines it.
            if (Declared.Any(value => ToUInt64(value) == 0UL)) { reachable.Add(0UL); }

            _combinations = reachable.OrderBy(bits => bits).Select(ToEnum).ToArray();

            return _combinations;
        }
    }

    /// <summary>The value's underlying bits, whatever the enum's underlying type — signed members included.</summary>
    private static ulong ToUInt64(TEnum value) {
        // Convert.ToUInt64 throws on a negative signed member, so each signed width is read at its own size and
        // reinterpreted, exactly as the runtime stores it.
        return Type.GetTypeCode(typeof(TEnum)) switch {
            TypeCode.SByte => unchecked((ulong)Convert.ToSByte(value, CultureInfo.InvariantCulture)),
            TypeCode.Int16 => unchecked((ulong)Convert.ToInt16(value, CultureInfo.InvariantCulture)),
            TypeCode.Int32 => unchecked((ulong)Convert.ToInt32(value, CultureInfo.InvariantCulture)),
            TypeCode.Int64 => unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            _              => Convert.ToUInt64(value, CultureInfo.InvariantCulture)
        };
    }

    private static TEnum ToEnum(ulong bits) {
        return (TEnum)Enum.ToObject(typeof(TEnum), bits);
    }

    private static string V(TEnum value) {
        return value.ToString();
    }

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(TEnum[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<TEnum>? _allowed;
    private readonly ConstraintCall?       _allowedConstraint;
    private readonly bool                  _combinable;
    private readonly IReadOnlyList<TEnum>  _excluded;
    private readonly List<TEnum>           _pool;
    private readonly RandomSource          _source;
    private readonly IReadOnlyList<TEnum>  _universe;

    #endregion

    private AnyEnum(RandomSource source, IReadOnlyList<TEnum> universe, bool combinable,
                    IReadOnlyList<TEnum>? allowed, ConstraintCall? allowedConstraint, IReadOnlyList<TEnum> excluded) {
        _source            = source;
        _universe          = universe;
        _combinable        = combinable;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        // Materialized once here — "constrain once, draw many": Generate never refilters the pool.
        _pool = (allowed ?? universe).Where(value => !excluded.Contains(value)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    // The pool is materialized once at construction, so its size is the exact number of values drawable.
    long? ICardinalityHint<TEnum>.DistinctCardinality => _pool.Count;

    // The pool is the exact draw set, so membership is a direct pool lookup.
    bool ICardinalityHint<TEnum>.Contains(TEnum value) => _pool.Contains(value);

    /// <summary>
    ///     Widens the draw from the declared members to every <b>combination</b> of them — the values a
    ///     <see cref="FlagsAttribute">[Flags]</see> enum is designed to hold. Without it, a flags dummy carries at
    ///     most one bit and a branch reading two never runs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The universe is every value obtained by OR-ing a non-empty subset of the declared members, plus the
    ///         zero value when a zero member is declared: <c>{ None = 0, Read = 1, Write = 2, Exec = 4 }</c> yields the
    ///         eight values <c>0</c>–<c>7</c>, while <c>{ Left = 1, Right = 2 }</c> yields only <c>1</c>, <c>2</c> and
    ///         <c>3</c> — never <c>0</c>, which that enum does not define. A declared composite adds nothing:
    ///         <c>ReadWrite = Read | Write</c> is already the combination of the two.
    ///     </para>
    ///     <para>
    ///         <see cref="Except" /> and <see cref="DifferentFrom" /> keep comparing by <b>equality</b>, here as
    ///         everywhere else: <c>Except(Read)</c> forbids the value <c>Read</c> and still allows
    ///         <c>Read | Write</c>. Applied after <see cref="OneOf" />, this constraint changes nothing — an explicit
    ///         allow-list is a terminal enumeration of exact values, so declare it before <c>OneOf</c> when the
    ///         allow-list itself names combinations.
    ///     </para>
    /// </remarks>
    /// <returns>A new generator drawing from the combination universe.</returns>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when <typeparamref name="TEnum" /> is not declared <c>[Flags]</c>, when it declares more non-zero
    ///     members than the enumerable ceiling, or when the constraint contradicts a constraint already declared.
    /// </exception>
    public AnyEnum<TEnum> AllowingCombinations() {
        ConstraintCall constraint = ConstraintCall.Of(nameof(AllowingCombinations));
        if (_combinable) { return this; }

        if (!IsFlags) {
            throw new ConflictingAnyConstraintException(
                $"Cannot apply {constraint} because {typeof(TEnum).Name} is not declared [Flags]: OR-ing its members would produce values the type does not define.");
        }

        int generators = Declared.Count(value => ToUInt64(value) != 0UL);
        if (generators > MaxCombinableMembers) {
            throw new ConflictingAnyConstraintException(
                $"Cannot apply {constraint} because {typeof(TEnum).Name} declares {V(generators)} non-zero members, more than the {V(MaxCombinableMembers)} whose combinations can be enumerated. " +
                $"Draw from an explicit set with OneOf(...) instead.");
        }

        return Validated(new AnyEnum<TEnum>(_source, Combinations, true, _allowed, _allowedConstraint, _excluded), constraint);
    }

    /// <summary>Requires the value to be one of the supplied members. Declared once per generator.</summary>
    /// <param name="values">
    ///     The allowed values; duplicates are ignored. Every value must belong to the generator's universe — the
    ///     declared members, or every combination of them once <see cref="AllowingCombinations" /> has been applied.
    ///     The generator never yields a value outside that universe, not even an explicitly supplied one.
    /// </param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a value outside the generator's universe.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with LINQ expressions",
                                                     Justification =
                                                         "The loop exists to name the FIRST offending element in the exception it throws. A Where clause discards which element failed, so " +
                                                         "the message would have to re-find it, turning one pass into two and one statement into three.")]
    public AnyEnum<TEnum> OneOf(params TEnum[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (TEnum value in values) {
            if (!_universe.Contains(value)) { throw new ArgumentException($"The value {value} {DescribeOutsideUniverse()}", nameof(values)); }
        }

        ConstraintCall constraint = ConstraintCall.Of(nameof(OneOf), Join(values));
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == constraint) { return this; }
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {constraint} because {_allowedConstraint} is already defined."); }

        return Validated(new AnyEnum<TEnum>(_source, _universe, _combinable, values.Distinct().ToArray(), constraint, _excluded), constraint);
    }

    /// <summary>
    ///     Requires the value to be none of the supplied ones, compared by <b>equality</b> — under
    ///     <see cref="AllowingCombinations" /> too, so <c>Except(Read)</c> forbids <c>Read</c> and still allows
    ///     <c>Read | Write</c>.
    /// </summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyEnum<TEnum> Except(params TEnum[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return WithExcluded(values, ConstraintCall.Of(nameof(Except), Join(values)));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyEnum<TEnum> DifferentFrom(TEnum value) {
        return WithExcluded([value], ConstraintCall.Of(nameof(DifferentFrom), V(value)));
    }

    /// <inheritdoc />
    public TEnum Generate() {
        return _pool[_source.Current.Next(_pool.Count)];
    }

    /// <summary>
    ///     Why a supplied value is outside the universe — naming <see cref="AllowingCombinations" /> when the value is
    ///     a flag combination, since that is the constraint the caller is missing rather than a mistyped member.
    /// </summary>
    private string DescribeOutsideUniverse() {
        string subject = $"is not a declared member of {typeof(TEnum).Name}: the generator only ever yields declared members.";
        if (_combinable || !IsFlags) { return subject; }

        return $"{subject} Apply AllowingCombinations() first to draw combinations of them.";
    }

    private AnyEnum<TEnum> WithExcluded(TEnum[] values, ConstraintCall applying) {
        List<TEnum> excluded = [.. _excluded, .. values];

        return Validated(new AnyEnum<TEnum>(_source, _universe, _combinable, _allowed, _allowedConstraint, excluded), applying);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
                                                     Justification =
                                                         "Validated is the uniform validation hook of the fluent builders: every With* method routes its candidate through it, and all " +
                                                         "seven engines declare it with the same signature. It reads the CANDIDATE's state rather than this instance's — which is what " +
                                                         "the rule notices — but that is a builder validating its own successor, not an oversight. Making it static across seven types " +
                                                         "would break a family resemblance the reader relies on, for no measurable gain on a path that runs once per declared " +
                                                         "constraint.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that do not access instance data should be static",
                                                     Justification =
                                                         "Validated is the uniform validation hook of the fluent builders: every With* method routes its candidate through it, and all " +
                                                         "seven engines declare it with the same signature. It reads the CANDIDATE's state rather than this instance's — which is what " +
                                                         "the rule notices — but that is a builder validating its own successor, not an oversight. Making it static across seven types " +
                                                         "would break a family resemblance the reader relies on, for no measurable gain on a path that runs once per declared " +
                                                         "constraint.")]
    private AnyEnum<TEnum> Validated(AnyEnum<TEnum> candidate, ConstraintCall applying) {
        if (candidate._pool.Count > 0) { return candidate; }

        // Three exhausted pools, three different things to tell the reader: an allow-list that nothing survives, a
        // flags enum whose combinations are all excluded, and a plain enum whose declared members are.
        string pool;
        if (candidate._allowedConstraint is not null) { pool = $"no value {candidate._allowedConstraint} allows remains available"; }
        else if (candidate._combinable) { pool = $"no {typeof(TEnum).Name} combination remains available"; }
        else { pool = $"no declared {typeof(TEnum).Name} member remains available"; }

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {pool}.");
    }

}
