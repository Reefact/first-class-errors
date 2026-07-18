namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     A strictly-positive number of nights — a <b>value-type</b> value object over <see cref="int" />. It exists to
///     show the binder's value-type overloads: a nullable value-type request property (<c>int?</c>) bound through a
///     converter over the underlying <see cref="int" /> (<see cref="From" />), and the <c>AsOptionalValue</c> path that
///     yields a real <see cref="Nullable{T}" /> when the property is absent.
/// </summary>
/// <remarks>
///     A consumer is free to model a value object as a <c>readonly struct</c>, and the binder supports it first-class.
///     The trade-off — the reason <b>FirstClassErrors' own</b> value objects stay classes — is that a struct always
///     exposes <c>default(NightCount)</c> (here a <c>Value</c> of <c>0</c>) that bypasses <see cref="From" />; a
///     consumer choosing a struct accepts that, and simply never relies on a default-constructed instance being valid.
/// </remarks>
public readonly struct NightCount : IEquatable<NightCount> {

    #region Properties declarations

    /// <summary>The validated number of nights (always ≥ 1 when obtained through <see cref="From" />).</summary>
    public int Value { get; private init; }

    #endregion

    #region Statics members declarations

    /// <summary>
    ///     Creates a <see cref="NightCount" /> from <paramref name="nights" />, or fails with a documented
    ///     <see cref="InvalidNightCountError" /> when it is not strictly positive. This is the
    ///     <c>Func&lt;int, Outcome&lt;NightCount&gt;&gt;</c> a binder passes to <c>AsRequired</c> / <c>AsOptionalValue</c>.
    /// </summary>
    public static Outcome<NightCount> From(int nights) {
        if (nights < 1) {
            return Outcome<NightCount>.Failure(InvalidNightCountError.NotStrictlyPositive(nights));
        }

        return Outcome<NightCount>.Success(new NightCount { Value = nights });
    }

    #endregion

    /// <inheritdoc />
    public bool Equals(NightCount other) {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is NightCount other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return Value;
    }

    /// <inheritdoc />
    public override string ToString() {
        return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

}
