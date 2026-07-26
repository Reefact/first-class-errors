#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The named cases of <see cref="AnyEnum{TEnum}.AllowingCombinations" />: the exact universe a given enum shape
///     yields, the conflict wording, and the boundary between the declared-members default and the opt-in. The
///     universal half — that every draw belongs to the universe whatever the constraints — lives in
///     <c>JustDummies.PropertyTests</c>.
/// </summary>
public sealed class AnyEnumCombinationTests {

    // Enough draws that an eight-value universe is exhausted with overwhelming probability, while a missing value is
    // not attributed to bad luck. The assertions below are on the SET observed, so they are reachability claims.
    private const int SampleCount = 2000;

    [Flags]
    private enum Permissions {

        None  = 0,
        Read  = 1,
        Write = 2,
        Exec  = 4

    }

    // No zero member: the empty combination is not a value this enum defines.
    [Flags]
    private enum Sides {

        Left  = 1,
        Right = 2

    }

    // A declared composite: ReadWrite is already Read | Write, so it must not widen the universe.
    [Flags]
    private enum Access {

        Read      = 1,
        Write     = 2,
        ReadWrite = 3

    }

    private enum OrderStatus {

        Draft,
        Validated,
        Cancelled

    }

    [Fact(DisplayName = "A [Flags] enum still draws only declared members until combinations are allowed.")]
    public void FlagsEnumDrawsDeclaredMembersByDefault() {
        HashSet<Permissions> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Permissions>().Generate()); }

        // The contract the opt-in exists to leave untouched: the default never depends on the [Flags] attribute, so a
        // combination is unreachable until the test asks for one.
        Check.That(seen).IsOnlyMadeOf(Permissions.None, Permissions.Read, Permissions.Write, Permissions.Exec);
    }

    [Fact(DisplayName = "AllowingCombinations: the universe is every combination, and the declared zero value.")]
    public void CombinationsCoverTheWholeUniverse() {
        HashSet<Permissions> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Permissions>().AllowingCombinations().Generate()); }

        Check.That(seen.Count).IsEqualTo(8);
        for (int bits = 0; bits <= 7; bits++) { Check.That(seen).Contains((Permissions)bits); }
    }

    [Fact(DisplayName = "AllowingCombinations: an enum declaring no zero member never yields the empty combination.")]
    public void CombinationsOmitZeroWhenItIsNotDeclared() {
        HashSet<Sides> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Sides>().AllowingCombinations().Generate()); }

        Check.That(seen).IsOnlyMadeOf(Sides.Left, Sides.Right, Sides.Left | Sides.Right);
    }

    [Fact(DisplayName = "AllowingCombinations: a declared composite adds nothing — it already is a combination.")]
    public void DeclaredCompositeDoesNotWidenTheUniverse() {
        HashSet<Access> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Access>().AllowingCombinations().Generate()); }

        Check.That(seen).IsOnlyMadeOf(Access.Read, Access.Write, Access.ReadWrite);
    }

    [Fact(DisplayName = "AllowingCombinations: applying it to an enum that is not [Flags] conflicts, naming why.")]
    public void CombinationsRequireAFlagsEnum() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<OrderStatus>().AllowingCombinations());

        Check.That(conflict.Message).Contains("AllowingCombinations()");
        Check.That(conflict.Message).Contains("OrderStatus");
        Check.That(conflict.Message).Contains("[Flags]");
    }

    [Fact(DisplayName = "AllowingCombinations: applying it twice is a no-op, not a conflict.")]
    public void CombinationsAreIdempotent() {
        AnyEnum<Permissions> generator = Any.Enum<Permissions>().AllowingCombinations().AllowingCombinations();

        HashSet<Permissions> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }

        // Idempotent, not cumulative: the universe is the same eight values a single application yields.
        Check.That(seen.Count).IsEqualTo(8);
    }

    [Fact(DisplayName = "OneOf: a combination is refused before the opt-in, and the message names the missing one.")]
    public void OneOfRefusesACombinationBeforeTheOptIn() {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Any.Enum<Permissions>().OneOf(Permissions.Read | Permissions.Write));

        Check.That(error.Message).Contains("AllowingCombinations()");
    }

    [Fact(DisplayName = "OneOf: a combination is accepted once combinations are allowed.")]
    public void OneOfAcceptsACombinationAfterTheOptIn() {
        AnyEnum<Permissions> generator = Any.Enum<Permissions>()
                                            .AllowingCombinations()
                                            .OneOf(Permissions.Read | Permissions.Write, Permissions.Exec);

        HashSet<Permissions> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }

        Check.That(seen).IsOnlyMadeOf(Permissions.Read | Permissions.Write, Permissions.Exec);
    }

    [Fact(DisplayName = "Except: exclusions compare by equality, so a combination carrying an excluded bit survives.")]
    public void ExclusionsCompareByEquality() {
        AnyEnum<Permissions> generator = Any.Enum<Permissions>().AllowingCombinations().Except(Permissions.Read);

        HashSet<Permissions> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }

        // Read itself is gone; Read | Write is a different value and stays drawable — Except is not a bit mask.
        Check.That(seen).Not.Contains(Permissions.Read);
        Check.That(seen).Contains(Permissions.Read | Permissions.Write);
        Check.That(seen.Count).IsEqualTo(7);
    }

    [Fact(DisplayName = "AllowingCombinations: the widened universe feeds the distinct-collection cardinality check.")]
    public void CombinationsWidenTheCardinalityHint() {
        // Eight distinct values exist, so eight are obtainable and nine conflict eagerly — the same check that caps a
        // declared-members draw at four.
        HashSet<Permissions> eight = Any.SetOf(Any.Enum<Permissions>().AllowingCombinations()).WithCount(8).Generate();
        Check.That(eight.Count).IsEqualTo(8);

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.Enum<Permissions>().AllowingCombinations()).WithCount(9).Generate());
        Check.That(conflict.Message).Contains("9");

        ConflictingAnyConstraintException capped = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.Enum<Permissions>()).WithCount(5).Generate());
        Check.That(capped.Message).Contains("5");
    }

    [Fact(DisplayName = "AllowingCombinations: excluding the whole universe conflicts, naming both sides.")]
    public void ExcludingTheWholeUniverseConflicts() {
        Permissions[] everything = Enumerable.Range(0, 8).Select(bits => (Permissions)bits).ToArray();

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<Permissions>().AllowingCombinations().Except(everything));

        Check.That(conflict.Message).Contains("Except(");
        Check.That(conflict.Message).Contains("Permissions");
    }

    [Fact(DisplayName = "AllowingCombinations: an enum with too many members to enumerate is refused, naming the ceiling.")]
    public void TooManyMembersIsRefused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<Wide>().AllowingCombinations());

        Check.That(conflict.Message).Contains("AllowingCombinations()");
        Check.That(conflict.Message).Contains("21");
        Check.That(conflict.Message).Contains("20");
        Check.That(conflict.Message).Contains("OneOf");
    }

    [Fact(DisplayName = "AllowingCombinations: a seeded context replays the same combinations.")]
    public void CombinationsReplayUnderASeed() {
        List<Permissions> Batch(int seed) {
            AnyContext          context   = Any.WithSeed(seed);
            AnyEnum<Permissions> generator = context.Enum<Permissions>().AllowingCombinations();

            return Enumerable.Range(0, 20).Select(_ => generator.Generate()).ToList();
        }

        Check.That(Batch(4242)).ContainsExactly(Batch(4242));
    }

    // Twenty-one single-bit members: one past the ceiling AllowingCombinations() will enumerate.
    [Flags]
    private enum Wide {

        B00 = 1 << 0, B01 = 1 << 1, B02 = 1 << 2, B03 = 1 << 3, B04 = 1 << 4, B05 = 1 << 5, B06 = 1 << 6,
        B07 = 1 << 7, B08 = 1 << 8, B09 = 1 << 9, B10 = 1 << 10, B11 = 1 << 11, B12 = 1 << 12, B13 = 1 << 13,
        B14 = 1 << 14, B15 = 1 << 15, B16 = 1 << 16, B17 = 1 << 17, B18 = 1 << 18, B19 = 1 << 19, B20 = 1 << 20

    }

}
