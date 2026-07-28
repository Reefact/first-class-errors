#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Shared FsCheck generators for the property suite. They generate the <b>constraints</b> — the bounds, lengths,
///     counts and seeds a caller declares — while JustDummies generates the value that must satisfy them. That split is
///     the point of this project: the example-based suite pins a handful of hand-picked constraints
///     (<c>Between(10, 20)</c>, <c>WithLength(12)</c>) and can only prove the generator right for those, whereas a
///     property quantifies over the whole constraint space and lets FsCheck shrink a failure down to its minimal
///     counter-example.
/// </summary>
/// <remarks>
///     Drawing the constraints from FsCheck rather than from <see cref="Any" /> also breaks a circularity: the suite
///     no longer uses the component under test to decide what to test it with.
/// </remarks>
internal static class Generators {

    #region Statics members declarations

    /// <summary>
    ///     Pairs two draws of <paramref name="values" /> into an ordered <c>(min, max)</c> tuple, so a bound pair is
    ///     always well-formed. Degenerate pairs (<c>min == max</c>) are deliberately kept: pinning a single value is a
    ///     legitimate — and historically fragile — corner of every interval generator.
    /// </summary>
    public static Gen<(T Min, T Max)> OrderedPair<T>(Gen<T> values, IComparer<T>? comparer = null) {
        IComparer<T> order = comparer ?? Comparer<T>.Default;

        return from first in values
               from second in values
               select order.Compare(first, second) <= 0 ? (first, second) : (second, first);
    }

    /// <summary>
    ///     Mixes FsCheck's own draws with the edges an off-by-one hides behind. FsCheck's default numeric generator is
    ///     size-bounded and clusters around zero, so the extremes of the range would otherwise almost never be drawn —
    ///     exactly where an interval generator overflows or silently truncates.
    /// </summary>
    public static Gen<T> WithEdges<T>(Gen<T> values, params T[] edges) {
        return Gen.OneOf(values, Gen.Elements(edges));
    }

    /// <summary>Arbitrary <see cref="int" />s, biased towards the ends of the range.</summary>
    public static Gen<int> Int32() {
        return WithEdges(ArbMap.Default.GeneratorFor<int>(), int.MinValue, int.MinValue + 1, -1, 0, 1, int.MaxValue - 1, int.MaxValue);
    }

    /// <summary>Arbitrary <see cref="long" />s, biased towards the ends of the range.</summary>
    public static Gen<long> Int64() {
        return WithEdges(ArbMap.Default.GeneratorFor<long>(), long.MinValue, long.MinValue + 1, -1, 0, 1, long.MaxValue - 1, long.MaxValue);
    }

    /// <summary>Arbitrary finite <see cref="double" />s. NaN and the infinities are excluded: the library rejects them as argument errors.</summary>
    public static Gen<double> Double() {
        return WithEdges(ArbMap.Default.GeneratorFor<double>().Where(value => !double.IsNaN(value) && !double.IsInfinity(value)),
                         double.MinValue, -1d, 0d, 1d, double.MaxValue);
    }

    /// <summary>Arbitrary <see cref="decimal" />s, biased towards the ends of the range.</summary>
    public static Gen<decimal> Decimal() {
        return WithEdges(ArbMap.Default.GeneratorFor<decimal>(), decimal.MinValue, -1m, 0m, 1m, decimal.MaxValue);
    }

    /// <summary>A collection or string length: small enough to stay cheap, wide enough to cross the empty and single-element cases.</summary>
    public static Gen<int> Count(int max = 12) {
        return Gen.Choose(0, max);
    }

    /// <summary>An arbitrary seed, including the values a hand-written test would never pick.</summary>
    public static Gen<int> Seed() {
        return Int32();
    }

    #endregion

}

/// <summary>
///     Assertion helpers usable from inside an FsCheck property, where the property's verdict is a returned
///     <see cref="bool" /> rather than a thrown assertion.
/// </summary>
internal static class Expect {

    #region Statics members declarations

    /// <summary>
    ///     Returns <c>true</c> when <paramref name="action" /> throws an exception assignable to
    ///     <typeparamref name="TException" />; otherwise <c>false</c>.
    /// </summary>
    public static bool Throws<TException>(Action action)
        where TException : Exception {
        try {
            action();

            return false;
        } catch (TException) {
            return true;
        }
    }

    /// <summary>
    ///     Returns <c>true</c> when <paramref name="action" /> completes without throwing. The counterpart of
    ///     <see cref="Throws{TException}" />, for a property whose subject is that ordinary use of a generated value
    ///     stays uneventful — <c>decimal</c> arithmetic, which signals its overflow by throwing rather than by
    ///     saturating.
    /// </summary>
    public static bool DoesNotThrow(Action action) {
        try {
            action();

            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    ///     Draws <paramref name="count" /> values from <paramref name="generator" /> and returns <c>true</c> when every
    ///     one of them satisfies <paramref name="invariant" />. A generator is a recipe, not a value, so one draw per
    ///     FsCheck case would leave most of its randomness untested; a handful of draws per case multiplies the
    ///     coverage without making the property expensive.
    /// </summary>
    public static bool EveryDraw<T>(IAny<T> generator, Func<T, bool> invariant, int count = 8) {
        for (int i = 0; i < count; i++) {
            if (!invariant(generator.Generate())) { return false; }
        }

        return true;
    }

    /// <summary>
    ///     Materializes <paramref name="count" /> draws from <paramref name="generator" />, for the properties that
    ///     reason over a batch rather than over each value in isolation (reachability, distinctness, ...).
    /// </summary>
    public static List<T> Draws<T>(IAny<T> generator, int count) {
        List<T> values = new(count);
        for (int i = 0; i < count; i++) {
            values.Add(generator.Generate());
        }

        return values;
    }

    #endregion

}
