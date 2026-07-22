#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     The count-constraint facade shared by every collection generator, defined once over
///     <see cref="CollectionState{T}" />. The <see cref="AnyCollection{TItem,TResult,TSelf}" /> builders
///     (a list, an array, a sequence, a set) and <see cref="AnyDictionary{TKey,TValue}" /> — whose keys run through
///     the very same <see cref="CollectionState{T}" /> — each expose <c>NonEmpty</c>/<c>Empty</c>/<c>WithCount</c>/
///     <c>WithMinCount</c>/<c>WithMaxCount</c>/<c>WithCountBetween</c> by delegating here, so the argument
///     validation and the constraint labels that surface in a <see cref="ConflictingAnyConstraintException" /> live
///     in exactly one place and cannot drift between the two surfaces.
/// </summary>
/// <remarks>
///     Each method takes a state and returns the tightened state; the caller wraps that state back into its own
///     immutable generator. The label strings (<c>"NonEmpty()"</c>, <c>"WithCount(3)"</c>, ...) are part of the
///     user-facing conflict messages, so they are produced here rather than in the label-agnostic
///     <see cref="CollectionState{T}" />, which only records whichever label its caller hands it.
/// </remarks>
internal static class CountConstraints {

    #region Statics members declarations

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static int RequireNonNegative(int count, string parameterName) {
        if (count < 0) { throw new ArgumentOutOfRangeException(parameterName, count, "The count must not be negative."); }

        return count;
    }

    #endregion

    /// <summary>Requires at least one element.</summary>
    internal static CollectionState<T> NonEmpty<T>(CollectionState<T> state) {
        return state.WithMinCount(1, "NonEmpty()");
    }

    /// <summary>Fixes the collection to no elements.</summary>
    internal static CollectionState<T> Empty<T>(CollectionState<T> state) {
        return state.WithExactCount(0, "Empty()");
    }

    /// <summary>Fixes the exact number of elements.</summary>
    internal static CollectionState<T> WithCount<T>(CollectionState<T> state, int count) {
        RequireNonNegative(count, nameof(count));

        return state.WithExactCount(count, $"WithCount({V(count)})");
    }

    /// <summary>Requires at least <paramref name="count" /> elements.</summary>
    internal static CollectionState<T> WithMinCount<T>(CollectionState<T> state, int count) {
        RequireNonNegative(count, nameof(count));

        return state.WithMinCount(count, $"WithMinCount({V(count)})");
    }

    /// <summary>Requires at most <paramref name="count" /> elements.</summary>
    internal static CollectionState<T> WithMaxCount<T>(CollectionState<T> state, int count) {
        RequireNonNegative(count, nameof(count));

        return state.WithMaxCount(count, $"WithMaxCount({V(count)})");
    }

    /// <summary>Requires a number of elements within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    internal static CollectionState<T> WithCountBetween<T>(CollectionState<T> state, int minimum, int maximum) {
        RequireNonNegative(minimum, nameof(minimum));
        RequireNonNegative(maximum, nameof(maximum));
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        string constraint = $"WithCountBetween({V(minimum)}, {V(maximum)})";

        return state.WithMinCount(minimum, constraint).WithMaxCount(maximum, constraint);
    }

}
