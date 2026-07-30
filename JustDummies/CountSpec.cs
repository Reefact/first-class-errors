#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

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
///     <para>
///         That spread governs every draw, bounded or not (ADR-0050): a declared maximum composes with it rather than
///         replacing it, so an upper bound only narrows the draw and never widens it. Only a minimum, an exact count
///         or required elements enlarge a collection.
///     </para>
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
    private readonly ConstraintCall? _exactConstraint;
    private readonly int?    _max;
    private readonly ConstraintCall? _maxConstraint;
    private readonly int     _min;
    private readonly ConstraintCall? _minConstraint;

    #endregion

    private CountSpec(int? exact, ConstraintCall? exactConstraint,
                      int   min,   ConstraintCall? minConstraint,
                      int?  max,   ConstraintCall? maxConstraint) {
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
    internal CountSpec WithExactCount(int count, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_exactConstraint == applying) { return this; }
        if (_exactConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _exactConstraint); }

        return new CountSpec(count, applying, _min, _minConstraint, _max, _maxConstraint).Validated(applying);
    }

    /// <summary>Tightens the minimum count; a looser bound than the current one is a no-op.</summary>
    internal CountSpec WithMinCount(int count, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (count <= _min) { return this; }

        return new CountSpec(_exact, _exactConstraint, count, applying, _max, _maxConstraint).Validated(applying);
    }

    /// <summary>Tightens the maximum count; a looser bound than the current one is a no-op.</summary>
    internal CountSpec WithMaxCount(int count, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (_max is not null && count >= _max) { return this; }

        return new CountSpec(_exact, _exactConstraint, _min, _minConstraint, count, applying).Validated(applying);
    }

    /// <summary>
    ///     Draws a count satisfying the specification. <paramref name="requiredMin" /> raises the floor to cover
    ///     elements the collection must contain (see <see cref="CollectionState{T}" />); <paramref name="cap" /> lowers
    ///     the ceiling to the number of distinct values a distinct collection can hold. Both are already known to be
    ///     compatible with the declared bounds — the collection validates them eagerly before generation.
    /// </summary>
    internal int Resolve(SeededRandom random, int requiredMin, int? cap) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }
        if (_exact is int exact) { return exact; }

        int min = Math.Max(_min, requiredMin);
        // A declared maximum composes with the default spread instead of replacing it (ADR-0050): it may only narrow
        // the draw, never widen it, so a loose cap still yields the small unconstrained collection. Long arithmetic: a
        // huge required minimum must saturate instead of overflowing past int.MaxValue.
        long spreadCeiling = (long)min + DefaultCountSpread;
        int  max           = (int)Math.Min(_max is int declared ? Math.Min(spreadCeiling, declared) : spreadCeiling, int.MaxValue);
        if (cap is int ceiling && ceiling < max) { max = ceiling; }
        if (max < min) { max = min; }

        return min == max ? min : random.NextInt32Inclusive(min, max);
    }

    /// <summary>
    ///     Ensures the collection may hold the <paramref name="required" /> elements it must contain; throws naming the
    ///     upper bound that leaves no room. Symmetric wording, so the message reads whether the last constraint applied
    ///     was the count cap or the containment requirement.
    /// </summary>
    internal void EnsureFits(int required, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        int? cap = _exact ?? _max;
        if (cap is int ceiling && required > ceiling) {
            throw ConflictingAnyConstraintException.ContainedElementsDoNotFit(applying, Elements(required), Elements(ceiling));
        }
    }

    private CountSpec Validated(ConstraintCall applying) {
        if (_exact is int exact) { EnsureExactAgreesWithBounds(applying, exact); }

        if (_max is int max && _min > max) {
            // Both bounds carry their constraint name: each is written as a pair by the constructor. And this branch
            // needs _min > max, with max >= 0 because the entry points reject a negative count — so _min > 0, which
            // only WithMinCount can produce, and it names the constraint as it sets the value.
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                ConstraintClaim.Of(_maxConstraint!, $"already caps the count at {V(max)}"),
                                                                ConstraintClaim.Of(_minConstraint!, $"already requires at least {Elements(_min)}"));
        }

        return this;
    }

    /// <summary>
    ///     Ensures a fixed count does not contradict a bound already applied; throws naming the bound it contradicts.
    ///     Symmetric wording, so the message reads whether the last constraint applied was the fixed count or the bound.
    /// </summary>
    private void EnsureExactAgreesWithBounds(ConstraintCall applying, int exact) {
        if (exact < _min) {
            // Same reasoning as above: exact >= 0 is guaranteed by the entry points, so exact < _min needs _min > 0
            // — a declared minimum, hence a named one — and a declared exact count carries its name too.
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                ConstraintClaim.Of(_exactConstraint!, $"already fixes the count at {V(exact)}"),
                                                                ConstraintClaim.Of(_minConstraint!, $"already requires at least {Elements(_min)}"));
        }

        if (_max is int cappedAt && exact > cappedAt) {
            // Both values are declared here, and each was written as a pair with the constraint that declared it.
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                ConstraintClaim.Of(_exactConstraint!, $"already fixes the count at {V(exact)}"),
                                                                ConstraintClaim.Of(_maxConstraint!, $"already caps the count at {V(cappedAt)}"));
        }
    }

}
