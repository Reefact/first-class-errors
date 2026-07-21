#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace Dummies.UnitTests;

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
        HashSet<int> seen = new(Samples(Any.OneOf(1, 2, 3)));

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
        HashSet<int> seen = new(Samples(Any.OneOf(1, 2, 3, 4)));

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "Duplicate values are collapsed under the default comparer: both distinct values are still drawn, nothing else.")]
    public void DuplicatesAreCollapsed() {
        HashSet<int> seen = new(Samples(Any.OneOf(1, 1, 2)));

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

        List<Percentage?> values = new();
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
        IReadOnlyList<int> pool = new List<int> { 1, 2, 3 };

        HashSet<int> seen = new(Samples(Any.ElementOf(pool)));

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
        Check.ThatCode(() => Any.ElementOf(new List<string> { "a", null! })).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A seeded context makes OneOf and ElementOf deterministic — the mirrored surface draws from the context's seed.")]
    public void SeededContextIsDeterministic() {
        List<int> pool = new() { 10, 20, 30, 40 };

        string oneOfFirst  = string.Join("|", Samples(Any.WithSeed(11).OneOf(10, 20, 30, 40)).Take(20));
        string oneOfSecond = string.Join("|", Samples(Any.WithSeed(11).OneOf(10, 20, 30, 40)).Take(20));
        Check.That(oneOfSecond).IsEqualTo(oneOfFirst);

        string elementFirst  = string.Join("|", Samples(Any.WithSeed(11).ElementOf(pool)).Take(20));
        string elementSecond = string.Join("|", Samples(Any.WithSeed(11).ElementOf(pool)).Take(20));
        Check.That(elementSecond).IsEqualTo(elementFirst);
    }

}
