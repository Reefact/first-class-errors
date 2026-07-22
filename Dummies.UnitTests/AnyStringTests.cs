#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace Dummies.UnitTests;

[TestSubject(typeof(AnyString))]
public sealed class AnyStringTests {

    private const int SampleCount = 200;

    #region Statics members declarations

    private static IEnumerable<string> Samples(IAny<string> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    #endregion

    [Fact(DisplayName = "An unconstrained String yields 0 to 16 ASCII letters and digits.")]
    public void UnconstrainedYieldsShortAlphanumeric() {
        foreach (string value in Samples(Any.String())) {
            Check.That(value.Length).IsLessOrEqualThan(16);
            Check.That(value.All(char.IsLetterOrDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "NonEmpty yields at least one character.")]
    public void NonEmptyHasAtLeastOneCharacter() {
        foreach (string value in Samples(Any.String().NonEmpty())) {
            Check.That(value.Length).IsStrictlyGreaterThan(0);
        }
    }

    [Fact(DisplayName = "WithLength yields exactly that many characters.")]
    public void WithLengthIsExact() {
        foreach (string value in Samples(Any.String().WithLength(10))) {
            Check.That(value.Length).IsEqualTo(10);
        }
    }

    [Fact(DisplayName = "WithLength(0) yields the empty string.")]
    public void WithLengthZeroIsEmpty() {
        Check.That(Any.String().WithLength(0).Generate()).IsEqualTo(string.Empty);
    }

    [Fact(DisplayName = "WithMinLength and WithMaxLength bound the length inclusively.")]
    public void MinAndMaxLengthAreInclusiveBounds() {
        foreach (string value in Samples(Any.String().WithMinLength(3).WithMaxLength(5))) {
            Check.That(value.Length).IsGreaterOrEqualThan(3);
            Check.That(value.Length).IsLessOrEqualThan(5);
        }
    }

    [Fact(DisplayName = "WithLengthBetween bounds the length inclusively and reaches its bounds.")]
    public void WithLengthBetweenIsInclusive() {
        HashSet<int> lengths = new();
        foreach (string value in Samples(Any.String().WithLengthBetween(2, 4))) {
            lengths.Add(value.Length);
            Check.That(value.Length).IsGreaterOrEqualThan(2);
            Check.That(value.Length).IsLessOrEqualThan(4);
        }

        Check.That(lengths.Contains(2)).IsTrue();
        Check.That(lengths.Contains(4)).IsTrue();
    }

    [Fact(DisplayName = "StartingWith anchors the prefix.")]
    public void StartingWithAnchorsThePrefix() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-"))) {
            Check.That(value).StartsWith("ORD-");
        }
    }

    [Fact(DisplayName = "EndingWith anchors the suffix.")]
    public void EndingWithAnchorsTheSuffix() {
        foreach (string value in Samples(Any.String().EndingWith("-FR"))) {
            Check.That(value).EndsWith("-FR");
        }
    }

    [Fact(DisplayName = "Containing embeds the value.")]
    public void ContainingEmbedsTheValue() {
        foreach (string value in Samples(Any.String().Containing("ABC"))) {
            Check.That(value).Contains("ABC");
        }
    }

    [Fact(DisplayName = "Prefix, contained value, suffix and exact length hold together.")]
    public void FragmentsAndExactLengthHoldTogether() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-").Containing("X").EndingWith("-FR").WithLength(12))) {
            Check.That(value.Length).IsEqualTo(12);
            Check.That(value).StartsWith("ORD-");
            Check.That(value).Contains("X");
            Check.That(value).EndsWith("-FR");
        }
    }

    [Fact(DisplayName = "A fragment-only budget is generable: length equals the fragment sum.")]
    public void FragmentsExactlyFillingTheLengthAreGenerable() {
        Check.That(Any.String().StartingWith("AB").EndingWith("CD").WithLength(4).Generate()).IsEqualTo("ABCD");
    }

    [Fact(DisplayName = "Alpha yields ASCII letters only.")]
    public void AlphaYieldsLettersOnly() {
        foreach (string value in Samples(Any.String().Alpha().NonEmpty())) {
            Check.That(value.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')).IsTrue();
        }
    }

    [Fact(DisplayName = "Numeric yields ASCII digits only.")]
    public void NumericYieldsDigitsOnly() {
        foreach (string value in Samples(Any.String().Numeric().NonEmpty())) {
            Check.That(value.All(character => character is >= '0' and <= '9')).IsTrue();
        }
    }

    [Fact(DisplayName = "AlphaNumeric yields ASCII letters and digits only.")]
    public void AlphaNumericYieldsLettersAndDigitsOnly() {
        foreach (string value in Samples(Any.String().AlphaNumeric().NonEmpty())) {
            Check.That(value.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')).IsTrue();
        }
    }

    [Fact(DisplayName = "LowerCase yields no uppercase letter; digits stay allowed.")]
    public void LowerCaseForbidsUppercaseLetters() {
        foreach (string value in Samples(Any.String().LowerCase().NonEmpty())) {
            Check.That(value.Any(character => character is >= 'A' and <= 'Z')).IsFalse();
        }
    }

    [Fact(DisplayName = "UpperCase yields no lowercase letter; fragments keep their own characters.")]
    public void UpperCaseForbidsLowercaseLetters() {
        foreach (string value in Samples(Any.String().UpperCase().StartingWith("ORD-").NonEmpty())) {
            Check.That(value.Any(character => character is >= 'a' and <= 'z')).IsFalse();
            Check.That(value).StartsWith("ORD-");
        }
    }

    [Fact(DisplayName = "A second WithLength conflicts: the exact length is declared once.")]
    public void SecondWithLengthConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().WithLength(3).WithLength(5));

        Check.That(conflict.Message).Contains("WithLength(5)");
        Check.That(conflict.Message).Contains("WithLength(3)");
    }

    [Fact(DisplayName = "A prefix longer than the exact length conflicts, naming both sides.")]
    public void PrefixLongerThanExactLengthConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().WithLength(3).StartingWith("ORD-"));

        Check.That(conflict.Message).Contains("StartingWith(\"ORD-\")");
        Check.That(conflict.Message).Contains("WithLength(3)");
        Check.That(conflict.Message).Contains("4");
    }

    [Fact(DisplayName = "An exact length shorter than an already declared prefix conflicts, naming both sides.")]
    public void ExactLengthShorterThanPrefixConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().StartingWith("ORD-").WithLength(3));

        Check.That(conflict.Message).Contains("WithLength(3)");
        Check.That(conflict.Message).Contains("ORD-");
        Check.That(conflict.Message).Contains("4");
    }

    [Fact(DisplayName = "A numeric-only string cannot start with a non-numeric prefix.")]
    public void NumericPrefixMismatchConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().Numeric().StartingWith("ORD-"));

        Check.That(conflict.Message).Contains("StartingWith(\"ORD-\")");
        Check.That(conflict.Message).Contains("Numeric()");
    }

    [Fact(DisplayName = "Declaring the charset after an incompatible prefix conflicts too: order does not matter.")]
    public void CharsetAfterIncompatiblePrefixConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().StartingWith("ORD-").Numeric());

        Check.That(conflict.Message).Contains("Numeric()");
        Check.That(conflict.Message).Contains("ORD-");
    }

    [Fact(DisplayName = "A minimum length above the maximum conflicts.")]
    public void MinAboveMaxConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().WithMinLength(10).WithMaxLength(3));

        Check.That(conflict.Message).Contains("WithMaxLength(3)");
        Check.That(conflict.Message).Contains("WithMinLength(10)");
    }

    [Fact(DisplayName = "An exact length above an already declared maximum conflicts.")]
    public void ExactAboveMaxConflicts() {
        Check.ThatCode(() => Any.String().WithMaxLength(3).WithLength(5)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "LowerCase then UpperCase conflicts: one casing per generator.")]
    public void LowerThenUpperCaseConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().LowerCase().UpperCase());

        Check.That(conflict.Message).Contains("UpperCase()");
        Check.That(conflict.Message).Contains("LowerCase()");
    }

    [Fact(DisplayName = "Alpha then Numeric conflicts: one character family per generator.")]
    public void AlphaThenNumericConflicts() {
        Check.ThatCode(() => Any.String().Alpha().Numeric()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "A lowercase-only string cannot anchor an uppercase prefix.")]
    public void LowerCaseUppercasePrefixConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().LowerCase().StartingWith("ORD-"));

        Check.That(conflict.Message).Contains("StartingWith(\"ORD-\")");
        Check.That(conflict.Message).Contains("LowerCase()");
    }

    [Fact(DisplayName = "A second StartingWith conflicts: the prefix is declared once.")]
    public void SecondStartingWithConflicts() {
        Check.ThatCode(() => Any.String().StartingWith("A").StartingWith("B")).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Fragments exceeding the maximum length conflict.")]
    public void FragmentsExceedingMaxLengthConflict() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().WithMaxLength(5).StartingWith("ORD-").EndingWith("-FR"));

        Check.That(conflict.Message).Contains("EndingWith(\"-FR\")");
        Check.That(conflict.Message).Contains("7");
    }

    [Fact(DisplayName = "Length arguments are validated as arguments, not as conflicts.")]
    public void LengthArgumentsAreValidated() {
        Check.ThatCode(() => Any.String().WithLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithMinLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithMaxLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithLengthBetween(5, 3)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Fragment arguments are validated as arguments, not as conflicts.")]
    public void FragmentArgumentsAreValidated() {
        Check.ThatCode(() => Any.String().StartingWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().StartingWith("")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.String().EndingWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().Containing("")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "DifferentFrom never returns the excluded value.")]
    public void DifferentFromNeverReturnsTheExcludedValue() {
        foreach (string value in Samples(Any.String().WithLength(1).Alpha().DifferentFrom("A"))) {
            Check.That(value).IsNotEqualTo("A");
        }
    }

    [Fact(DisplayName = "Except excludes each listed value.")]
    public void ExceptExcludesEachListedValue() {
        string[] forbidden = { "A", "B", "C" };
        foreach (string value in Samples(Any.String().WithLength(1).Alpha().Except("A", "B", "C"))) {
            Check.That(forbidden.Contains(value)).IsFalse();
        }
    }

    [Fact(DisplayName = "An exclusion preserves the declared shape: only shape-matching survivors are drawn.")]
    public void ExclusionPreservesTheDeclaredShape() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-").WithLength(5).DifferentFrom("ORD-A"))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Length).IsEqualTo(5);
            Check.That(value).IsNotEqualTo("ORD-A");
        }
    }

    [Fact(DisplayName = "Exclusions accumulate across several declarations.")]
    public void ExclusionsAccumulateAcrossDeclarations() {
        foreach (string value in Samples(Any.String().WithLength(1).Alpha().Except("A", "B").DifferentFrom("C"))) {
            Check.That(value is "A" or "B" or "C").IsFalse();
        }
    }

    [Fact(DisplayName = "An over-tight exclusion fails at generation with a bounded, seed-bearing AnyGenerationException.")]
    public void OverTightExclusionThrowsSeedBearingGenerationException() {
        string[] everyLetter = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(letter => letter.ToString()).ToArray();

        AnyGenerationException error = Assert.Throws<AnyGenerationException>(
            () => Any.WithSeed(20260721).String().WithLength(1).Alpha().Except(everyLetter).Generate());

        Check.That(error.Seed).IsEqualTo(20260721);
        Check.That(error.Message).Contains("Any.WithSeed(20260721)");
    }

    [Fact(DisplayName = "A seeded exclusion is reproducible: the same seed yields the same value.")]
    public void SeededExclusionIsReproducible() {
        string first  = Any.WithSeed(4242).String().NonEmpty().Alpha().DifferentFrom("Q").Generate();
        string second = Any.WithSeed(4242).String().NonEmpty().Alpha().DifferentFrom("Q").Generate();

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "OneOf cannot combine with an exclusion: it stays terminal.")]
    public void OneOfCannotCombineWithAnExclusion() {
        Check.ThatCode(() => Any.String().DifferentFrom("x").OneOf("a", "b")).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Exclusion arguments are validated as arguments, not as conflicts.")]
    public void ExclusionArgumentsAreValidated() {
        Check.ThatCode(() => Any.String().DifferentFrom(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().Except(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().Except()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.String().Except("a", null!)).Throws<ArgumentException>();
    }

}
