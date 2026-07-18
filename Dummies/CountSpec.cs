#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     The immutable count specification shared by every collection generator (<see cref="AnyList{T}" />,
///     <see cref="AnySet{T}" />, ...): a lower bound, an optional upper bound and an optional exact count — each
///     remembering the constraint that set it, so a conflict message can name both sides. It is the collection-count
///     analogue of <see cref="StringSpec" />'s length bounds: every mutation returns a new specification and
///     cross-validates the whole eagerly, so a collection generator that exists can always produce a count.
/// </summary>
/// <remarks>
///     Unconstrained, a collection draws between <c>0</c> and <see cref="DefaultCountSpread" /> elements: an
///     unconstrained collection can therefore be empty — chain <c>NonEmpty()</c> when the surrounding code requires
///     content. The spread is deliberately smaller than <see cref="AnyString" />'s (which is 16): a collection's
///     elements are themselves generated values, heavier than a string's characters, so a smaller default keeps a
///     dummy collection cheap while still exercising the multi-element path.
/// </remarks>
internal sealed class CountSpec {

    /// <summary>The number of extra elements an unconstrained collection may hold above its required minimum.</summary>
    internal const int DefaultCountSpread = 8;

    #region Statics members declarations

    internal static readonly CountSpec Unconstrained = new(null, null, 0, null, null, null);

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Elements(int count) {
        return count == 1 ? "1 element" : $"{V(count)} elements";
    }

    #endregion

    #region Fields declarations

    private readonly int?    _exact;
    private readonly string? _exactConstraint;
    private readonly int?    _max;
    private readonly string? _maxConstraint;
    private readonly int     _min;
    private readonly string? _minConstraint;

    #endregion

    private CountSpec(int? exact, string? exactConstraint,
                      int   min,   string? minConstraint,
                      int?  max,   string? maxConstraint) {
        _exact           = exact;
        _exactConstraint = exactConstraint;
        _min             = min;
        _minConstraint   = minConstraint;
        _max             = max;
        _maxConstraint   = maxConstraint;
    }

    /// <summary>The smallest count the specification allows — the exact count when pinned, otherwise the lower bound.</summary>
    internal int Floor => _exact ?? _min;

    /// <summary>The largest count the specification allows, or <c>null</c> when the upper bound is left open.</summary>
    internal int? Ceiling => _exact ?? _max;

    /// <summary>Fixes the exact count; declared once per generator.</summary>
    internal CountSpec WithExactCount(int count, string applying) {
        if (_exactConstraint is not null) { throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_exactConstraint} is already defined."); }

        return new CountSpec(count, applying, _min, _minConstraint, _max, _maxConstraint).Validated(applying);
    }

    /// <summary>Tightens the minimum count; a looser bound than the current one is a no-op.</summary>
    internal CountSpec WithMinCount(int count, string applying) {
        if (count <= _min) { return this; }

        return new CountSpec(_exact, _exactConstraint, count, applying, _max, _maxConstraint).Validated(applying);
    }

    /// <summary>Tightens the maximum count; a looser bound than the current one is a no-op.</summary>
    internal CountSpec WithMaxCount(int count, string applying) {
        if (_max is not null && count >= _max) { return this; }

        return new CountSpec(_exact, _exactConstraint, _min, _minConstraint, count, applying).Validated(applying);
    }

    /// <summary>
    ///     Draws a count satisfying the specification. <paramref name="requiredMin" /> raises the floor to cover
    ///     elements the collection must contain (see <see cref="CollectionState{T}" />); <paramref name="cap" /> lowers
    ///     the ceiling to the number of distinct values a distinct collection can hold. Both are already known to be
    ///     compatible with the declared bounds — the collection validates them eagerly before generation.
    /// </summary>
    internal int Resolve(Random random, int requiredMin, int? cap) {
        if (_exact is int exact) { return exact; }

        int min = Math.Max(_min, requiredMin);
        // Long arithmetic: a huge declared minimum must saturate instead of overflowing past int.MaxValue.
        int max = _max ?? (int)Math.Min((long)min + DefaultCountSpread, int.MaxValue);
        if (cap is int ceiling && ceiling < max) { max = ceiling; }
        if (max < min) { max = min; }

        return min == max ? min : random.NextInt32Inclusive(min, max);
    }

    /// <summary>
    ///     Ensures the collection may hold the <paramref name="required" /> elements it must contain; throws naming the
    ///     upper bound that leaves no room. Symmetric wording, so the message reads whether the last constraint applied
    ///     was the count cap or the containment requirement.
    /// </summary>
    internal void EnsureFits(int required, string applying) {
        int? cap = _exact ?? _max;
        if (cap is int ceiling && required > ceiling) {
            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {Elements(required)} required to be contained cannot fit in a collection of at most {Elements(ceiling)}.");
        }
    }

    private CountSpec Validated(string applying) {
        if (_exact is int exact) {
            if (exact < _min) {
                throw new ConflictingAnyConstraintException(applying == _exactConstraint
                                                                ? $"Cannot apply {applying} because {_minConstraint} already requires at least {Elements(_min)}."
                                                                : $"Cannot apply {applying} because {_exactConstraint} already fixes the count at {V(exact)}.");
            }

            if (_max is int cappedAt && exact > cappedAt) {
                throw new ConflictingAnyConstraintException(applying == _exactConstraint
                                                                ? $"Cannot apply {applying} because {_maxConstraint} already caps the count at {V(cappedAt)}."
                                                                : $"Cannot apply {applying} because {_exactConstraint} already fixes the count at {V(exact)}.");
            }
        }

        if (_max is int max && _min > max) {
            throw new ConflictingAnyConstraintException(applying == _maxConstraint
                                                            ? $"Cannot apply {applying} because {_minConstraint} already requires at least {Elements(_min)}."
                                                            : $"Cannot apply {applying} because {_maxConstraint} already caps the count at {V(max)}.");
        }

        return this;
    }

}
