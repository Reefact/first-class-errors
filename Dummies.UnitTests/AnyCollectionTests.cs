#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

public sealed class AnyCollectionTests {

    #region Statics members declarations

    private const int SampleCount = 200;

    private enum Suit {

        Clubs,
        Diamonds,
        Hearts,
        Spades

    }

    #endregion

    [Fact(DisplayName = "ListOf: unconstrained draws vary in size, stay within 0..8, and hold elements from the item generator.")]
    public void ListOfUnconstrained() {
        HashSet<int> sizes = new();
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 9)).Generate();
            sizes.Add(list.Count);
            Check.That(list.Count).IsGreaterOrEqualThan(0);
            Check.That(list.Count).IsLessOrEqualThan(8);
            Check.That(list).ContainsOnlyElementsThatMatch(value => value is >= 1 and <= 9);
        }
        Check.That(sizes.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "ListOf: the count family fixes, floors, caps and ranges the size.")]
    public void ListOfCountFamily() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.ListOf(Any.Int32()).WithCount(5).Generate().Count).IsEqualTo(5);
            Check.That(Any.ListOf(Any.Int32()).Empty().Generate().Count).IsEqualTo(0);
            Check.That(Any.ListOf(Any.Int32()).NonEmpty().Generate().Count).IsStrictlyGreaterThan(0);
            Check.That(Any.ListOf(Any.Int32()).WithMinCount(3).Generate().Count).IsGreaterOrEqualThan(3);
            Check.That(Any.ListOf(Any.Int32()).WithMaxCount(2).Generate().Count).IsLessOrEqualThan(2);

            int ranged = Any.ListOf(Any.Int32()).WithCountBetween(4, 6).Generate().Count;
            Check.That(ranged is >= 4 and <= 6).IsTrue();
        }
    }

    [Fact(DisplayName = "ListOf: contradictory count constraints fail eagerly naming both sides.")]
    public void ListOfCountConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.ListOf(Any.Int32()).WithCount(3).WithMinCount(5));
        Check.That(conflict.Message).Contains("WithCount(3)");

        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMinCount(5).WithMaxCount(3)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(2).WithCount(3)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "ListOf: count constraints validate their arguments.")]
    public void ListOfCountValidation() {
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMinCount(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCountBetween(6, 4)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.ListOf<int>(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Distinct: a wide-domain distinct list holds only distinct elements.")]
    public void DistinctOverAWideDomain() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 1000)).WithCount(20).Distinct().Generate();
            Check.That(list.Count).IsEqualTo(20);
            Check.That(new HashSet<int>(list).Count).IsEqualTo(20);
        }
    }

    [Fact(DisplayName = "Distinct: a count beyond the element cardinality conflicts eagerly, naming the shortfall.")]
    public void DistinctCardinalityConflictsEagerly() {
        ConflictingAnyConstraintException fromBool = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.Bool()).WithCount(3));
        Check.That(fromBool.Message).Contains("2 distinct value");

        Check.ThatCode(() => Any.SetOf(Any.Enum<Suit>()).WithMinCount(5)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Int32().Between(1, 3)).WithCount(5)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32().Between(1, 3)).WithCount(5).Distinct()).Throws<ConflictingAnyConstraintException>();
        // Order-independent: turning distinct on after the count is set conflicts just the same.
        Check.ThatCode(() => Any.ListOf(Any.Bool()).WithCount(3).Distinct()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Distinct: an unknowable small domain cannot be detected early, so a shortfall surfaces at generation.")]
    public void DistinctFallbackThrowsAtGeneration() {
        // '.As' erases the cardinality hint, so the conflict cannot be seen at declaration — the bounded dedup-draw
        // fallback catches it while generating instead.
        IAny<int> opaque = Any.Int32().Between(1, 3).As(value => value);

        Check.ThatCode(() => Any.SetOf(opaque).WithCount(5).Generate()).Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "SetOf: elements are always distinct and drawn from the item generator.")]
    public void SetOfIsDistinct() {
        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().Between(1, 500)).WithCount(10).Generate();
            Check.That(set.Count).IsEqualTo(10);
            Check.That(set).ContainsOnlyElementsThatMatch(value => value is >= 1 and <= 500);
        }
    }

    [Fact(DisplayName = "SetOf: a comparer merges values, so cardinality is only an upper bound and the fallback still guards.")]
    public void SetOfHonoursAComparer() {
        IEqualityComparer<int> modTen = new ModuloComparer(10);

        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().Between(0, 999), modTen).WithCount(5).Generate();
            Check.That(set.Count).IsEqualTo(5);
            List<int> classes = set.Select(value => value % 10).ToList();
            Check.That(classes.Count).IsEqualTo(new HashSet<int>(classes).Count);
        }

        // Only ten residue classes exist, so twenty distinct-under-the-comparer elements are impossible; the raw
        // cardinality (1000) hides that, so it can only be caught while drawing.
        Check.ThatCode(() => Any.SetOf(Any.Int32().Between(0, 999), modTen).WithCount(20).Generate()).Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "Containing: a required value is present, and a distinct duplicate requirement conflicts.")]
    public void ContainingPlacesValues() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 9)).WithCount(5).Containing(777).Generate();
            Check.That(list).Contains(777);
            Check.That(list.Count).IsEqualTo(5);
        }

        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(1).Containing(1).Containing(2)).Throws<ConflictingAnyConstraintException>();

        ConflictingAnyConstraintException duplicate = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.Int32()).Containing(7).Containing(7));
        Check.That(duplicate.Message).Contains("more than once");
    }

    [Fact(DisplayName = "Containing: a value drawn from a generator is forced into the collection.")]
    public void ContainingFromAGenerator() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 9)).NonEmpty().ContainingAny(Any.Int32().OneOf(4242)).Generate();
            Check.That(list).Contains(4242);
        }
    }

    [Fact(DisplayName = "ArrayOf: produces an array of the requested size, distinct when asked.")]
    public void ArrayOfProducesArrays() {
        for (int i = 0; i < SampleCount; i++) {
            int[] array = Any.ArrayOf(Any.Int32().Between(1, 100)).WithCount(6).Distinct().Generate();
            Check.That(array.Length).IsEqualTo(6);
            Check.That(new HashSet<int>(array).Count).IsEqualTo(6);
        }
    }

    [Fact(DisplayName = "SequenceOf: is fully materialized — enumerating twice yields the same elements without re-drawing.")]
    public void SequenceOfIsMaterialized() {
        IEnumerable<int> sequence = Any.SequenceOf(Any.Int32()).WithCount(5).Generate();

        List<int> first  = sequence.ToList();
        List<int> second = sequence.ToList();

        Check.That(first).ContainsExactly(second);
    }

    [Fact(DisplayName = "DictionaryOf: builds unique-keyed dictionaries and gates the count by the key domain.")]
    public void DictionaryOfBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            Dictionary<int, string> dictionary = Any.DictionaryOf(Any.Int32().Between(1, 1000), Any.String().NonEmpty()).WithCount(8).Generate();
            Check.That(dictionary.Count).IsEqualTo(8);
            Check.That(dictionary.Values).ContainsOnlyElementsThatMatch(value => value.Length > 0);
        }

        Check.ThatCode(() => Any.DictionaryOf(Any.Bool(), Any.Int32()).WithCount(3)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.DictionaryOf<int, int>(null!, Any.Int32())).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "PairOf and TripleOf assemble value tuples from constrained parts.")]
    public void PairAndTriple() {
        for (int i = 0; i < SampleCount; i++) {
            (int first, string second) pair = Any.PairOf(Any.Int32().Positive(), Any.String().NonEmpty()).Generate();
            Check.That(pair.first).IsStrictlyGreaterThan(0);
            Check.That(pair.second).IsNotEmpty();

            (int a, int b, int c) triple = Any.TripleOf(Any.Int32().Between(1, 2), Any.Int32().Between(3, 4), Any.Int32().Between(5, 6)).Generate();
            Check.That(triple.a is 1 or 2).IsTrue();
            Check.That(triple.b is 3 or 4).IsTrue();
            Check.That(triple.c is 5 or 6).IsTrue();
        }
    }

    [Fact(DisplayName = "Collections are reproducible when their element generator draws from a seeded context.")]
    public void CollectionsAreReproducible() {
        HashSet<int> first  = Any.SetOf(Any.WithSeed(4242).Int32()).WithCount(6).Generate();
        HashSet<int> second = Any.SetOf(Any.WithSeed(4242).Int32()).WithCount(6).Generate();

        Check.That(second.OrderBy(value => value)).ContainsExactly(first.OrderBy(value => value));

        List<int> listOne = Any.ListOf(Any.WithSeed(7).Int32().Between(0, 99)).WithCount(5).Generate();
        List<int> listTwo = Any.ListOf(Any.WithSeed(7).Int32().Between(0, 99)).WithCount(5).Generate();
        Check.That(listTwo).ContainsExactly(listOne);
    }

    [Fact(DisplayName = "Collections compose into value objects and aggregates through As and Combine.")]
    public void CollectionsComposeThroughAsAndCombine() {
        IAny<List<OrderReference>> references = Any.ListOf(Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create)).WithCount(3);

        List<OrderReference> list = references.Generate();
        Check.That(list.Count).IsEqualTo(3);
        Check.That(list).ContainsOnlyElementsThatMatch(reference => reference.Value.StartsWith("ORD-"));
    }

    #region Nested types

    private sealed class ModuloComparer : IEqualityComparer<int> {

        private readonly int _modulus;

        public ModuloComparer(int modulus) {
            _modulus = modulus;
        }

        public bool Equals(int x, int y) {
            return x % _modulus == y % _modulus;
        }

        public int GetHashCode(int obj) {
            return obj % _modulus;
        }

    }

    #endregion

}
