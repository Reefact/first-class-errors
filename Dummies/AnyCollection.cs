#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     The shared fluent surface of the collection generators. Every collection — a <see cref="AnyList{T}" />,
///     an <see cref="AnyArray{T}" />, an <see cref="AnySequence{T}" /> or an <see cref="AnySet{T}" /> — carries a
///     count and, optionally, values it must contain; the concrete generators add only how the elements are shaped
///     (a set is always distinct) and what type <see cref="Generate" /> returns.
/// </summary>
/// <remarks>
///     The contract matches the scalar generators: constraints express what the surrounding code <b>requires</b> of
///     the collection, never what the test asserts; instances are immutable recipes, each method returning a new
///     generator; and a combination that cannot be satisfied fails eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides. Unconstrained, a collection holds 0 to 8
///     elements — chain <see cref="NonEmpty" /> when the surrounding code requires content.
/// </remarks>
/// <typeparam name="TItem">The element type.</typeparam>
/// <typeparam name="TResult">The collection type <see cref="Generate" /> produces.</typeparam>
/// <typeparam name="TSelf">The concrete generator type, so the fluent methods return it.</typeparam>
public abstract class AnyCollection<TItem, TResult, TSelf> : IAny<TResult>, IHasRandomSource
    where TSelf : AnyCollection<TItem, TResult, TSelf> {

    #region Statics members declarations

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static int RequireNonNegative(int count, string parameterName) {
        if (count < 0) { throw new ArgumentOutOfRangeException(parameterName, count, "The count must not be negative."); }

        return count;
    }

    #endregion

    #region Fields declarations

    private protected readonly RandomSource?            SourceOrNull;
    private protected readonly CollectionState<TItem>   State;

    #endregion

    private protected AnyCollection(RandomSource? source, CollectionState<TItem> state) {
        SourceOrNull = source;
        State        = state;
    }

    RandomSource? IHasRandomSource.Source => SourceOrNull;

    /// <summary>Requires at least one element.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf NonEmpty() {
        return With(State.WithMinCount(1, "NonEmpty()"));
    }

    /// <summary>Fixes the collection to no elements.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf Empty() {
        return With(State.WithExactCount(0, "Empty()"));
    }

    /// <summary>Fixes the exact number of elements. Declared once per generator.</summary>
    /// <param name="count">The exact number of elements.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf WithCount(int count) {
        RequireNonNegative(count, nameof(count));

        return With(State.WithExactCount(count, $"WithCount({V(count)})"));
    }

    /// <summary>Requires at least <paramref name="count" /> elements.</summary>
    /// <param name="count">The inclusive minimum number of elements.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf WithMinCount(int count) {
        RequireNonNegative(count, nameof(count));

        return With(State.WithMinCount(count, $"WithMinCount({V(count)})"));
    }

    /// <summary>Requires at most <paramref name="count" /> elements.</summary>
    /// <param name="count">The inclusive maximum number of elements.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf WithMaxCount(int count) {
        RequireNonNegative(count, nameof(count));

        return With(State.WithMaxCount(count, $"WithMaxCount({V(count)})"));
    }

    /// <summary>Requires a number of elements within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive minimum number of elements.</param>
    /// <param name="maximum">The inclusive maximum number of elements.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf WithCountBetween(int minimum, int maximum) {
        RequireNonNegative(minimum, nameof(minimum));
        RequireNonNegative(maximum, nameof(maximum));
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        string constraint = $"WithCountBetween({V(minimum)}, {V(maximum)})";

        return With(State.WithMinCount(minimum, constraint).WithMaxCount(maximum, constraint));
    }

    /// <summary>
    ///     Requires the collection to contain <paramref name="value" />. May be declared several times; each required
    ///     value takes one element's room. In a distinct collection the required values must themselves be distinct.
    /// </summary>
    /// <param name="value">The value the generated collection must contain.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf Containing(TItem value) {
        return With(State.WithContaining(value, $"Containing({AnyDerivation.Display(value)})"));
    }

    /// <summary>
    ///     Requires the collection to contain a value drawn from <paramref name="generator" /> at generation time —
    ///     useful to force a particular shape of element into an otherwise arbitrary collection. Named apart from
    ///     <see cref="Containing" /> because a library generator both <i>is</i> an <see cref="IAny{T}" /> and converts
    ///     implicitly to its value, which would make a single overloaded name ambiguous.
    /// </summary>
    /// <param name="generator">The generator whose drawn value the collection must contain.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public TSelf ContainingAny(IAny<TItem> generator) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        return With(State.WithContaining(generator, "ContainingAny(<generator>)"));
    }

    /// <inheritdoc />
    public TResult Generate() {
        return Build(State.Materialize(SourceOrNull ?? AmbientRandomSource.Instance));
    }

    /// <summary>Wraps a new state in the concrete generator type.</summary>
    private protected abstract TSelf With(CollectionState<TItem> state);

    /// <summary>Converts the materialized elements into the concrete collection type.</summary>
    private protected abstract TResult Build(List<TItem> items);

}
