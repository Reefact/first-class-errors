namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <typeparamref name="TEnum" /> values, drawn uniformly from the enum's
///     <b>declared</b> members — never from undeclared numeric values. Constraints narrow the pool
///     (<see cref="OneOf" />, <see cref="Except" />, <see cref="DifferentFrom" />), and a combination that empties it
///     fails eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both sides.
/// </summary>
/// <typeparam name="TEnum">The enum type to draw values from.</typeparam>
public sealed class AnyEnum<TEnum> : IAny<TEnum>, IHasRandomSource
    where TEnum : struct, Enum {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnyEnum{TEnum}" /> can be used wherever a
    ///     <typeparamref name="TEnum" /> is expected. Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator TEnum(AnyEnum<TEnum> generator) {
        return generator.Generate();
    }

    // The declared-members set of an enum type is a process constant; cached once per closed generic type
    // instead of reflecting on every Any.Enum<T>() call.
    private static readonly TEnum[] Declared = ((TEnum[])Enum.GetValues(typeof(TEnum))).Distinct().ToArray();

    internal static AnyEnum<TEnum> Create(RandomSource source) {
        if (Declared.Length == 0) {
            throw new AnyGenerationException($"Cannot generate an arbitrary {typeof(TEnum).Name} value because the enum declares no members.");
        }

        return new AnyEnum<TEnum>(source, Declared, null, null, []);
    }

    private static string V(TEnum value) {
        return value.ToString();
    }

    private static string Join(TEnum[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<TEnum>? _allowed;
    private readonly string?               _allowedConstraint;
    private readonly IReadOnlyList<TEnum>  _declared;
    private readonly IReadOnlyList<TEnum>  _excluded;
    private readonly List<TEnum>           _pool;
    private readonly RandomSource          _source;

    #endregion

    private AnyEnum(RandomSource source, IReadOnlyList<TEnum> declared,
                    IReadOnlyList<TEnum>? allowed, string? allowedConstraint, IReadOnlyList<TEnum> excluded) {
        _source            = source;
        _declared          = declared;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        // Materialized once here — "constrain once, draw many": Generate never refilters the pool.
        _pool = (allowed ?? declared).Where(value => !excluded.Contains(value)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Requires the value to be one of the supplied members. Declared once per generator.</summary>
    /// <param name="values">The allowed members; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyEnum<TEnum> OneOf(params TEnum[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        string constraint = $"OneOf({Join(values)})";
        if (_allowedConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {constraint} because {_allowedConstraint} is already defined."); }

        return Validated(new AnyEnum<TEnum>(_source, _declared, values.Distinct().ToArray(), constraint, _excluded), constraint);
    }

    /// <summary>Requires the value to be none of the supplied members.</summary>
    /// <param name="values">The forbidden members.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyEnum<TEnum> Except(params TEnum[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return WithExcluded(values, $"Except({Join(values)})");
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The member the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyEnum<TEnum> DifferentFrom(TEnum value) {
        return WithExcluded([value], $"DifferentFrom({V(value)})");
    }

    /// <inheritdoc />
    public TEnum Generate() {
        return _pool[_source.Current.Random.Next(_pool.Count)];
    }

    private AnyEnum<TEnum> WithExcluded(TEnum[] values, string applying) {
        List<TEnum> excluded = new(_excluded);
        excluded.AddRange(values);

        return Validated(new AnyEnum<TEnum>(_source, _declared, _allowed, _allowedConstraint, excluded), applying);
    }

    private AnyEnum<TEnum> Validated(AnyEnum<TEnum> candidate, string applying) {
        if (candidate._pool.Count > 0) { return candidate; }

        string pool = candidate._allowedConstraint is null
                          ? $"no declared {typeof(TEnum).Name} member remains available"
                          : $"no value {candidate._allowedConstraint} allows remains available";

        throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {pool}.");
    }

}
