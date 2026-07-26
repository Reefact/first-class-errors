#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="AnyEnum{TEnum}.AllowingCombinations" />. Where the example-based suite pins
///     the universe a handful of named enum shapes yield, these quantify over the <b>constraints</b> applied on top of
///     it: the allow-list and the exclusion set are drawn from the universe itself, so a draw escaping the universe, an
///     exclusion silently read as a bit mask, or a pool that empties without conflicting is found and shrunk.
/// </summary>
/// <remarks>
///     The universe is fixed per enum type, so it is the constraint sets — not the type — that carry the input space.
///     <see cref="Permissions" /> is used throughout because its four declared members give a universe of eight values:
///     small enough to enumerate in the assertion, wide enough that a subset drawn from it is rarely trivial.
/// </remarks>
[TestSubject(typeof(AnyEnum<>))]
public sealed class EnumCombinationProperties {

    #region Statics members declarations

    /// <summary>Every value <c>AllowingCombinations()</c> must be able to draw for <see cref="Permissions" />.</summary>
    private static readonly Permissions[] Universe = Enumerable.Range(0, 8).Select(bits => (Permissions)bits).ToArray();

    /// <summary>
    ///     A non-empty subset of the universe, in an arbitrary order and possibly with repetitions — an allow-list or
    ///     an exclusion set as a caller would write it. Repetitions are kept on purpose: <c>Except</c> and
    ///     <c>OneOf</c> both have to absorb a duplicate without changing the pool they compute.
    /// </summary>
    private static Gen<Permissions[]> Subsets() {
        return Gen.NonEmptyListOf(Gen.Elements(Universe)).Select(values => values.ToArray());
    }

    #endregion

    [Fact(DisplayName = "AllowingCombinations: every draw is a combination of declared members, for every exclusion set.")]
    public void EveryDrawStaysInTheUniverse() {
        Prop.ForAll(Subsets().ToArbitrary(),
                    excluded => {
                        // A subset drawn with repetition can cover the whole universe; only then is a conflict owed,
                        // so the property branches on the drawn values rather than on the call shape.
                        Permissions[] distinct = excluded.Distinct().ToArray();
                        if (distinct.Length == Universe.Length) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.Enum<Permissions>().AllowingCombinations().Except(excluded));
                        }

                        return Expect.EveryDraw(Any.Enum<Permissions>().AllowingCombinations().Except(excluded),
                                                value => Universe.Contains(value) && !distinct.Contains(value));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "AllowingCombinations: exclusions compare by equality, never as a bit mask.")]
    public void ExclusionsCompareByEquality() {
        Prop.ForAll(Gen.Elements(Universe.Where(value => value != 0).ToArray()).ToArbitrary(),
                    excluded => {
                        // Every strict superset of the excluded value's bits is a DIFFERENT value, so it must remain
                        // reachable: reading Except as "no value carrying these bits" would make all of them vanish.
                        Permissions[] survivors = Universe.Where(value => value != excluded && (value & excluded) == excluded).ToArray();
                        if (survivors.Length == 0) { return true; }

                        List<Permissions> draws = Expect.Draws(Any.Enum<Permissions>().AllowingCombinations().Except(excluded), 200);

                        return draws.All(value => value != excluded) && survivors.Any(draws.Contains);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "AllowingCombinations: an allow-list of combinations is honoured exactly, for every subset.")]
    public void AnAllowListOfCombinationsIsHonoured() {
        Prop.ForAll(Subsets().ToArbitrary(),
                    allowed => {
                        Permissions[] distinct = allowed.Distinct().ToArray();

                        return Expect.EveryDraw(Any.Enum<Permissions>().AllowingCombinations().OneOf(allowed),
                                                distinct.Contains);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "AllowingCombinations: the declared-members default is untouched, for every exclusion set.")]
    public void TheDefaultRemainsDeclaredMembersOnly() {
        Permissions[] declared = [Permissions.None, Permissions.Read, Permissions.Write, Permissions.Exec];

        Prop.ForAll(Gen.Choose(0, 2).ToArbitrary(),
                    size => {
                        Permissions[] excluded = declared.Take(size).ToArray();
                        AnyEnum<Permissions> generator = excluded.Length == 0
                                                             ? Any.Enum<Permissions>()
                                                             : Any.Enum<Permissions>().Except(excluded);

                        // Without the opt-in no combination is ever drawn, whatever else was declared — the contract
                        // AllowingCombinations() exists precisely to leave alone.
                        return Expect.EveryDraw(generator, value => declared.Contains(value) && !excluded.Contains(value));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "AllowingCombinations: two contexts on the same seed draw the same combinations, for every seed.")]
    public void CombinationsAreReproducibleForEverySeed() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        List<Permissions> first  = Expect.Draws(Any.WithSeed(seed).Enum<Permissions>().AllowingCombinations(), 12);
                        List<Permissions> second = Expect.Draws(Any.WithSeed(seed).Enum<Permissions>().AllowingCombinations(), 12);

                        return first.SequenceEqual(second);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Flags]
    private enum Permissions {

        None  = 0,
        Read  = 1,
        Write = 2,
        Exec  = 4

    }

}
