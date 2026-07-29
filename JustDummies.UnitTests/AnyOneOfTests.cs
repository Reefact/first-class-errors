#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

[TestSubject(typeof(AnyOneOf<>))]
public sealed class AnyOneOfTests {

    private const int SampleCount = 200;

    #region Statics members declarations

    private static IEnumerable<T> Samples<T>(IAny<T> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    #endregion

    [Fact(DisplayName = "OneOf draws only the supplied values, including domain objects.")]
    public void DrawsOnlyTheSuppliedValues() {
        Percentage   ten     = Percentage.Create(10);
        Percentage   twenty  = Percentage.Create(20);
        Percentage   thirty  = Percentage.Create(30);
        Percentage[] allowed = [ten, twenty, thirty];

        foreach (Percentage value in Samples(Any.OneOf(ten, twenty, thirty))) {
            Check.That(allowed.Contains(value)).IsTrue();
        }
    }

    [Fact(DisplayName = "OneOf eventually reaches every supplied value.")]
    public void ReachesEverySuppliedValue() {
        HashSet<int> seen = [.. Samples(Any.OneOf(1, 2, 3))];

        Check.That(seen).Contains(1, 2, 3);
    }

    [Fact(DisplayName = "A single value pins the generated value.")]
    public void SingleValueIsPinned() {
        foreach (int value in Samples(Any.OneOf(42))) {
            Check.That(value).IsEqualTo(42);
        }
    }

    [Fact(DisplayName = "OneOf varies from draw to draw when the pool holds more than one value.")]
    public void VariesAcrossDraws() {
        HashSet<int> seen = [.. Samples(Any.OneOf(1, 2, 3, 4))];

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "Duplicate values are collapsed under the default comparer: both distinct values are still drawn, nothing else.")]
    public void DuplicatesAreCollapsed() {
        HashSet<int> seen = [.. Samples(Any.OneOf(1, 1, 2))];

        Check.That(seen).IsOnlyMadeOf(1, 2);
        Check.That(seen).Contains(1, 2);
    }

    [Fact(DisplayName = "OneOf is reproducible under a seed.")]
    public void ReproducibleUnderASeed() {
        string first  = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).OneOf("a", "b", "c", "d").Generate()));
        string second = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).OneOf("a", "b", "c", "d").Generate()));

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "OneOf composes into a value object through As.")]
    public void ComposesThroughAs() {
        IAny<OrderReference> generator = Any.OneOf("ORD-12345678", "ORD-87654321").As(OrderReference.Create);

        for (int i = 0; i < SampleCount; i++) {
            OrderReference reference = generator.Generate();
            Check.That(reference.Value).StartsWith("ORD-");
            Check.That(reference.Value.Length).IsEqualTo(12);
        }
    }

    [Fact(DisplayName = "OrNull makes the pool generator null about half the time, otherwise a member of the pool.")]
    public void OrNullIsSometimesNull() {
        Percentage       one       = Percentage.Create(1);
        Percentage       two       = Percentage.Create(2);
        IAny<Percentage?> generator = Any.WithSeed(20260721).OneOf(one, two).OrNull();

        List<Percentage?> values = [];
        for (int i = 0; i < SampleCount; i++) {
            values.Add(generator.Generate());
        }

        Check.That(values.Any(value => value is null)).IsTrue();
        Check.That(values.Where(value => value is not null)).IsOnlyMadeOf(one, two);
    }

    [Fact(DisplayName = "A distinct set over OneOf is gated by the pool's cardinality, both ways.")]
    public void CardinalityGatesDistinctCollections() {
        // Two distinct values cannot fill a set of three: caught eagerly, like any cardinality conflict.
        Check.ThatCode(() => Any.SetOf(Any.OneOf(1, 2)).WithCount(3)).Throws<ConflictingAnyConstraintException>();

        // Within the domain it fills the set with the requested distinct values.
        HashSet<int> set = Any.SetOf(Any.OneOf(1, 2, 3)).WithCount(3).Generate();
        Check.That(set.Count).IsEqualTo(3);
        Check.That(set).IsOnlyMadeOf(1, 2, 3);
    }

    [Fact(DisplayName = "Reference identity keeps equal-valued but distinct instances as separate pool members.")]
    public void ReferenceIdentityKeepsDistinctInstancesDistinct() {
        // Percentage has no value equality, so two instances of the same percentage are distinct under the default
        // comparer — the pool's cardinality is two, and a set of two is fillable.
        Percentage first  = Percentage.Create(50);
        Percentage second = Percentage.Create(50);

        HashSet<Percentage> set = Any.SetOf(Any.OneOf(first, second)).WithCount(2).Generate();

        Check.That(set.Count).IsEqualTo(2);
        Check.That(set).IsOnlyMadeOf(first, second);
    }

    [Fact(DisplayName = "OneOf rejects empty, null, or null-containing pools as arguments — null goes through OrNull.")]
    public void RejectsInvalidPools() {
        Check.ThatCode(() => Any.OneOf<int>()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.OneOf((string[])null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.OneOf("a", null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "The null-element message points the caller at OrNull().")]
    public void NullElementMessagePointsAtOrNull() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.OneOf("a", null!));

        Check.That(error.Message).Contains("OrNull");
    }

    [Fact(DisplayName = "ElementOf draws only from the list it is given.")]
    public void ElementOfDrawsFromTheList() {
        IReadOnlyList<int> pool = [1, 2, 3];

        HashSet<int> seen = [.. Samples(Any.ElementOf(pool))];

        Check.That(seen).IsOnlyMadeOf(1, 2, 3);
        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "ElementOf materializes a lazy sequence once, not once per draw.")]
    public void ElementOfMaterializesTheSequenceOnce() {
        int enumerations = 0;

        IEnumerable<int> Source() {
            enumerations++;
            yield return 1;
            yield return 2;
            yield return 3;
        }

        AnyOneOf<int> generator = Any.ElementOf(Source());
        for (int i = 0; i < SampleCount; i++) {
            generator.Generate();
        }

        Check.That(enumerations).IsEqualTo(1);
    }

    [Fact(DisplayName = "ElementOf validates null, empty and null elements like OneOf, for both the list and the sequence overload.")]
    public void ElementOfValidatesItsPool() {
        Check.ThatCode(() => Any.ElementOf((IReadOnlyList<int>)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.ElementOf((IEnumerable<int>)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.ElementOf(new List<int>())).Throws<ArgumentException>();
        Check.ThatCode(() => Any.ElementOf(Enumerable.Empty<int>())).Throws<ArgumentException>();
        Check.ThatCode(() => Any.ElementOf(["a", null!])).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "DifferentFrom removes a value from the pool — the idiom for drawing another element of a fixture.")]
    public void DifferentFromRemovesTheValue() {
        List<Percentage> orders = [Percentage.Create(10), Percentage.Create(20), Percentage.Create(30)];
        Percentage       used   = orders[1];

        foreach (Percentage value in Samples(Any.ElementOf(orders).DifferentFrom(used))) {
            Check.That(value).IsNotEqualTo(used);
            Check.That(orders.Contains(value)).IsTrue();
        }
    }

    [Fact(DisplayName = "Except removes every supplied value, and the exclusions accumulate across declarations.")]
    public void ExceptRemovesEveryValue() {
        foreach (int value in Samples(Any.OneOf(1, 2, 3, 4).Except(2, 3))) {
            Check.That(new[] { 1, 4 }).Contains(value);
        }

        foreach (int value in Samples(Any.OneOf(1, 2, 3, 4).Except(2).DifferentFrom(3).Except(4))) {
            Check.That(value).IsEqualTo(1);
        }
    }

    [Fact(DisplayName = "A value that is not in the pool removes nothing.")]
    public void ExcludingAnAbsentValueRemovesNothing() {
        HashSet<int> seen = [.. Samples(Any.OneOf(1, 2).DifferentFrom(99))];

        Check.That(seen).IsOnlyMadeOf(1, 2);
        Check.That(seen).Contains(1, 2);
    }

    [Fact(DisplayName = "An exclusion that empties the pool conflicts at declaration, naming both sides.")]
    public void AnExclusionEmptyingThePoolConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.OneOf(1, 2).Except(1, 2));

        Check.That(conflict.Message).IsEqualTo("Cannot apply Except(...) because it forbids every value OneOf(...) allows.");
    }

    [Fact(DisplayName = "The emptying conflict names the factory that declared the pool, on every entry point.")]
    public void TheConflictNamesTheDeclaringFactory() {
        // Each factory carries its own name into the pool, so a swapped or stale literal on any of the six entry
        // points would send the caller looking for a declaration they never wrote.
        static string Emptied(Func<object> declare) {
            return Assert.Throws<ConflictingAnyConstraintException>(() => declare()).Message;
        }

        const string fromOneOf    = "Cannot apply DifferentFrom(...) because it forbids every value OneOf(...) allows.";
        const string fromElement  = "Cannot apply DifferentFrom(...) because it forbids every value ElementOf(...) allows.";

        Check.That(Emptied(() => Any.OneOf(7).DifferentFrom(7))).IsEqualTo(fromOneOf);
        Check.That(Emptied(() => Any.WithSeed(1).OneOf(7).DifferentFrom(7))).IsEqualTo(fromOneOf);

        Check.That(Emptied(() => Any.ElementOf([7]).DifferentFrom(7))).IsEqualTo(fromElement);
        Check.That(Emptied(() => Any.ElementOf(new List<int> { 7 }.Select(value => value)).DifferentFrom(7))).IsEqualTo(fromElement);
        Check.That(Emptied(() => Any.WithSeed(1).ElementOf([7]).DifferentFrom(7))).IsEqualTo(fromElement);
        Check.That(Emptied(() => Any.WithSeed(1).ElementOf(new List<int> { 7 }.Select(value => value)).DifferentFrom(7))).IsEqualTo(fromElement);
    }

    [Fact(DisplayName = "An exclusion that leaves a declared value standing qualifies its claim instead of overstating it.")]
    public void AnExclusionLeavingADeclaredValueQualifiesItsClaim() {
        // DifferentFrom(2) does not forbid 1 — the first exclusion took that one — so it does not forbid *every*
        // value the pool was declared with, only what the first one left. The message says exactly that.
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.OneOf(1, 2).DifferentFrom(1).DifferentFrom(2));

        Check.That(conflict.Message).IsEqualTo("Cannot apply DifferentFrom(...) because it forbids every value OneOf(...) allows that the exclusions already declared leave.");
    }

    [Fact(DisplayName = "An exclusion covering the whole declared pool is not qualified away by an earlier one.")]
    public void AnExclusionCoveringTheWholePoolIsNotQualifiedAway() {
        // Except(1, 2) forbids both declared values, so dropping the earlier exclusion could not help and the
        // message must not suggest it: the claim stays the plain one, as it is without any prior narrowing.
        ConflictingAnyConstraintException afterAnother = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.OneOf(1, 2).DifferentFrom(1).Except(1, 2));
        ConflictingAnyConstraintException onItsOwn = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.OneOf(1, 2).Except(1, 2));

        Check.That(afterAnother.Message).IsEqualTo("Cannot apply Except(...) because it forbids every value OneOf(...) allows.");
        Check.That(afterAnother.Message).IsEqualTo(onItsOwn.Message);
    }

    [Fact(DisplayName = "A distinct set over an excluded pool is gated by the surviving cardinality.")]
    public void CardinalityFollowsTheFilteredPool() {
        // Three values minus one leaves two: a set of three no longer fits, a set of two does and holds exactly
        // the survivors.
        Check.ThatCode(() => Any.SetOf(Any.OneOf(1, 2, 3).DifferentFrom(2)).WithCount(3)).Throws<ConflictingAnyConstraintException>();

        HashSet<int> set = Any.SetOf(Any.OneOf(1, 2, 3).DifferentFrom(2)).WithCount(2).Generate();
        Check.That(set).IsOnlyMadeOf(1, 3);
    }

    [Fact(DisplayName = "The exclusion arguments are validated as arguments, not as conflicts.")]
    public void ExclusionArgumentsAreValidated() {
        Check.ThatCode(() => Any.OneOf("a", "b").Except(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.OneOf("a", "b").Except()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.OneOf("a", "b").Except("a", null!)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.OneOf("a", "b").DifferentFrom(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A seeded context makes OneOf and ElementOf deterministic — the mirrored surface draws from the context's seed.")]
    public void SeededContextIsDeterministic() {
        List<int> pool = [10, 20, 30, 40];

        string oneOfFirst  = string.Join("|", Samples(Any.WithSeed(11).OneOf(10, 20, 30, 40)).Take(20));
        string oneOfSecond = string.Join("|", Samples(Any.WithSeed(11).OneOf(10, 20, 30, 40)).Take(20));
        Check.That(oneOfSecond).IsEqualTo(oneOfFirst);

        string elementFirst  = string.Join("|", Samples(Any.WithSeed(11).ElementOf(pool)).Take(20));
        string elementSecond = string.Join("|", Samples(Any.WithSeed(11).ElementOf(pool)).Take(20));
        Check.That(elementSecond).IsEqualTo(elementFirst);
    }

}
