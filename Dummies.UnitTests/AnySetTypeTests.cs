#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

public sealed class AnySetTypeTests {

    private const int SampleCount = 200;

    private enum OrderStatus {

        Draft,
        Validated,
        Cancelled

    }

    [Fact(DisplayName = "Bool: unconstrained draws hit both values; pins pin; contradictory pins conflict.")]
    public void BoolBehaves() {
        HashSet<bool> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Bool().Generate()); }
        Check.That(seen.Count).IsEqualTo(2);

        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Bool().True().Generate()).IsTrue();
            Check.That(Any.Bool().False().Generate()).IsFalse();
            Check.That(Any.Bool().DifferentFrom(true).Generate()).IsFalse();
        }

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Bool().True().False());
        Check.That(conflict.Message).Contains("False()");
        Check.That(conflict.Message).Contains("True()");

        bool value = Any.Bool().True().Generate();
        Check.That(value).IsTrue();
    }

    [Fact(DisplayName = "Guid: unconstrained draws are non-empty, varied, and reproducible under a context seed.")]
    public void GuidBehaves() {
        HashSet<Guid> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            Guid value = Any.Guid().Generate();
            seen.Add(value);
            Check.That(value).IsNotEqualTo(Guid.Empty);
        }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        Check.That(Any.WithSeed(42).Guid().Generate()).IsEqualTo(Any.WithSeed(42).Guid().Generate());
    }

    [Fact(DisplayName = "Guid: Empty pins, NonEmpty excludes, and the pair conflicts in both orders.")]
    public void GuidEmptyFamily() {
        Check.That(Any.Guid().Empty().Generate()).IsEqualTo(Guid.Empty);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Guid().NonEmpty().Generate()).IsNotEqualTo(Guid.Empty);
        }

        Check.ThatCode(() => Any.Guid().Empty().NonEmpty()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Guid().NonEmpty().Empty()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Guid: OneOf stays within, exhausting it conflicts, DifferentFrom never yields the value.")]
    public void GuidSets() {
        Guid first  = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        for (int i = 0; i < SampleCount; i++) {
            Guid value = Any.Guid().OneOf(first, second).Generate();
            Check.That(value == first || value == second).IsTrue();
            Check.That(Any.Guid().OneOf(first, second).DifferentFrom(first).Generate()).IsEqualTo(second);
        }

        Check.ThatCode(() => Any.Guid().OneOf(first).Except(first)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Guid: excluding all 256 last-byte variants of the drawn prefix escapes by carry, never hangs, and stays reproducible.")]
    public async Task GuidExclusionByteWraparoundTerminates() {
        const int seed = 20260718;

        // The first unconstrained draw under this seed fixes the 15-byte prefix the escape starts from; a
        // second context with the same seed replays that same first draw, since Except() consumes no randomness.
        Guid   drawn  = Any.WithSeed(seed).Guid().Generate();
        byte[] prefix = drawn.ToByteArray();

        // Every identifier sharing that prefix and differing only in the last byte — the exact block the former
        // last-byte-only walk cycled inside forever.
        Guid[] block = new Guid[256];
        for (int last = 0; last < 256; last++) {
            byte[] variant = (byte[])prefix.Clone();
            variant[15] = (byte)last;
            block[last]  = new Guid(variant);
        }

        // Generate off-thread and race a deadline: a regression that reintroduces the unbounded loop loses the
        // race and fails the test instead of hanging the whole suite.
        Task<Guid> run   = Task.Run(() => Any.WithSeed(seed).Guid().Except(block).Generate());
        Task       first = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Check.That(first == run).IsTrue();

        Guid escaped = await run;
        Check.That(block.Contains(escaped)).IsFalse();
        Check.That(escaped).IsNotEqualTo(drawn);

        // Same seed and same exclusions yield the same escaped identifier.
        Guid again = Any.WithSeed(seed).Guid().Except(block).Generate();
        Check.That(again).IsEqualTo(escaped);
    }

    [Fact(DisplayName = "Enum: unconstrained draws yield only declared members and reach all of them.")]
    public void EnumDrawsDeclaredMembers() {
        HashSet<OrderStatus> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            OrderStatus value = Any.Enum<OrderStatus>().Generate();
            seen.Add(value);
            Check.That(System.Enum.IsDefined(typeof(OrderStatus), value)).IsTrue();
        }
        Check.That(seen.Count).IsEqualTo(3);
    }

    [Fact(DisplayName = "Enum: OneOf restricts, Except removes, exhausting the pool conflicts.")]
    public void EnumSets() {
        for (int i = 0; i < SampleCount; i++) {
            OrderStatus restricted = Any.Enum<OrderStatus>().OneOf(OrderStatus.Draft, OrderStatus.Validated).Generate();
            Check.That(restricted == OrderStatus.Draft || restricted == OrderStatus.Validated).IsTrue();
            Check.That(Any.Enum<OrderStatus>().Except(OrderStatus.Cancelled).Generate()).IsNotEqualTo(OrderStatus.Cancelled);
            Check.That(Any.Enum<OrderStatus>().OneOf(OrderStatus.Draft, OrderStatus.Validated).DifferentFrom(OrderStatus.Draft).Generate()).IsEqualTo(OrderStatus.Validated);
        }

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<OrderStatus>().Except(OrderStatus.Draft, OrderStatus.Validated, OrderStatus.Cancelled));
        Check.That(conflict.Message).Contains("Except(");
    }

    [Fact(DisplayName = "Enum: OneOf rejects undeclared numeric values — the declared-members-only contract holds.")]
    public void EnumOneOfRejectsUndeclaredValues() {
        ArgumentException rejected = Assert.Throws<ArgumentException>(
            () => Any.Enum<OrderStatus>().OneOf((OrderStatus)42));
        Check.That(rejected.Message).Contains("42");
        Check.That(rejected.Message).Contains("OrderStatus");

        Check.ThatCode(() => Any.Enum<OrderStatus>().OneOf(OrderStatus.Draft, (OrderStatus)42)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Char: the default pool is ASCII letters and digits; families narrow it.")]
    public void CharPools() {
        for (int i = 0; i < SampleCount; i++) {
            char value = Any.Char().Generate();
            Check.That(value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9').IsTrue();
            Check.That(Any.Char().Numeric().Generate() is >= '0' and <= '9').IsTrue();
            Check.That(Any.Char().Alpha().Generate() is >= 'A' and <= 'Z' or >= 'a' and <= 'z').IsTrue();
            Check.That(Any.Char().LowerCase().Generate() is >= 'A' and <= 'Z').IsFalse();
            Check.That(Any.Char().Alpha().UpperCase().Generate() is >= 'A' and <= 'Z').IsTrue();
        }
    }

    [Fact(DisplayName = "Char: OneOf restricts, exclusions apply, and contradictions conflict.")]
    public void CharSets() {
        for (int i = 0; i < SampleCount; i++) {
            char value = Any.Char().OneOf('a', 'b').Generate();
            Check.That(value == 'a' || value == 'b').IsTrue();
            Check.That(Any.Char().OneOf('a', 'b').DifferentFrom('a').Generate()).IsEqualTo('b');
        }

        Check.ThatCode(() => Any.Char().Numeric().Alpha()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Char().OneOf('a').Except('a')).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Char().OneOf('a').Numeric()).Throws<ConflictingAnyConstraintException>();
    }

}
