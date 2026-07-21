#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace Dummies.UnitTests;

[TestSubject(typeof(AnyStringOneOf))]
public sealed class AnyStringOneOfTests {

    private const int SampleCount = 200;

    #region Statics members declarations

    private static IEnumerable<string> Samples(IAny<string> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    #endregion

    [Fact(DisplayName = "OneOf draws only the supplied values.")]
    public void DrawsOnlyTheSuppliedValues() {
        string[] allowed = ["Apple", "Microsoft", "Google"];
        foreach (string value in Samples(Any.String().OneOf(allowed))) {
            Check.That(allowed.Contains(value)).IsTrue();
        }
    }

    [Fact(DisplayName = "OneOf eventually reaches every supplied value.")]
    public void ReachesEverySuppliedValue() {
        HashSet<string> seen = new(Samples(Any.String().OneOf("EUR", "USD", "GBP")));

        Check.That(seen).Contains("EUR", "USD", "GBP");
    }

    [Fact(DisplayName = "A single value pins the generated string.")]
    public void SingleValueIsPinned() {
        foreach (string value in Samples(Any.String().OneOf("SOLE"))) {
            Check.That(value).IsEqualTo("SOLE");
        }
    }

    [Fact(DisplayName = "OneOf varies from draw to draw when the set holds more than one value.")]
    public void VariesAcrossDraws() {
        HashSet<string> seen = new(Samples(Any.String().OneOf("a", "b", "c", "d")));

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "Duplicate values are collapsed: both distinct values are still drawn, nothing else.")]
    public void DuplicatesAreCollapsed() {
        HashSet<string> seen = new(Samples(Any.String().OneOf("a", "a", "b")));

        Check.That(seen).IsOnlyMadeOf("a", "b");
        Check.That(seen).Contains("a", "b");
    }

    [Fact(DisplayName = "An empty string is a legitimate member of the set.")]
    public void EmptyStringIsAllowed() {
        Check.That(Any.String().OneOf("").Generate()).IsEqualTo(string.Empty);
    }

    [Fact(DisplayName = "OneOf is reproducible under a seed.")]
    public void ReproducibleUnderASeed() {
        string first  = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).String().OneOf("a", "b", "c", "d").Generate()));
        string second = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).String().OneOf("a", "b", "c", "d").Generate()));

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "OneOf composes into a value object through As.")]
    public void ComposesThroughAs() {
        IAny<OrderReference> generator = Any.String().OneOf("ORD-12345678", "ORD-87654321").As(OrderReference.Create);

        for (int i = 0; i < SampleCount; i++) {
            OrderReference reference = generator.Generate();
            Check.That(reference.Value).StartsWith("ORD-");
            Check.That(reference.Value.Length).IsEqualTo(12);
        }
    }

    [Fact(DisplayName = "OrNull makes the value set generator null about half the time, otherwise a member of the set.")]
    public void OrNullIsSometimesNull() {
        IAny<string?> generator = Any.WithSeed(20260721).String().OneOf("a", "b").OrNull();

        List<string?> values = new();
        for (int i = 0; i < SampleCount; i++) {
            values.Add(generator.Generate());
        }

        Check.That(values.Any(value => value is null)).IsTrue();
        Check.That(values.Where(value => value is not null)).IsOnlyMadeOf("a", "b");
    }

    [Fact(DisplayName = "A distinct set over OneOf is gated by the set's cardinality, both ways.")]
    public void CardinalityGatesDistinctCollections() {
        // Two distinct values cannot fill a set of three: caught eagerly, like any cardinality conflict.
        Check.ThatCode(() => Any.SetOf(Any.String().OneOf("a", "b")).WithCount(3)).Throws<ConflictingAnyConstraintException>();

        // Within the domain it fills the set with the requested distinct values.
        HashSet<string> set = Any.SetOf(Any.String().OneOf("a", "b", "c")).WithCount(3).Generate();
        Check.That(set.Count).IsEqualTo(3);
        Check.That(set).IsOnlyMadeOf("a", "b", "c");
    }

    [Fact(DisplayName = "OneOf after another constraint conflicts: the value set is terminal.")]
    public void OneOfAfterAConstraintConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().NonEmpty().OneOf("a", "b"));

        Check.That(conflict.Message).Contains("OneOf");
        Check.That(conflict.Message).Contains("terminal");
    }

    [Fact(DisplayName = "OneOf after a length, shape or character constraint conflicts too, whatever the constraint.")]
    public void OneOfAfterVariousConstraintsConflicts() {
        Check.ThatCode(() => Any.String().WithLength(3).OneOf("abc")).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.String().StartingWith("ORD-").OneOf("ORD-12345678")).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.String().Numeric().OneOf("123")).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.String().WithMaxLength(10).OneOf("x")).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "OneOf rejects null, empty, or null-containing value lists as arguments.")]
    public void RejectsInvalidValueLists() {
        Check.ThatCode(() => Any.String().OneOf()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.String().OneOf(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().OneOf("a", null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "OneOf accepts a sequence, drawing only from its values.")]
    public void AcceptsASequence() {
        IEnumerable<string> vendors = new List<string> { "Apple", "Microsoft", "Google" };

        HashSet<string> seen = new(Samples(Any.String().OneOf(vendors)));

        Check.That(seen).IsOnlyMadeOf("Apple", "Microsoft", "Google");
        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "The sequence overload validates null, empty and null elements like the params one.")]
    public void SequenceOverloadValidates() {
        Check.ThatCode(() => Any.String().OneOf((IEnumerable<string>)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().OneOf(Enumerable.Empty<string>())).Throws<ArgumentException>();
        Check.ThatCode(() => Any.String().OneOf(new List<string> { "a", null! })).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "The sequence overload is terminal too: it conflicts after another constraint.")]
    public void SequenceOverloadIsTerminal() {
        Check.ThatCode(() => Any.String().NonEmpty().OneOf(new List<string> { "a", "b" })).Throws<ConflictingAnyConstraintException>();
    }

}
