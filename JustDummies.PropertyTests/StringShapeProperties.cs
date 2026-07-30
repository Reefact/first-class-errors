#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="AnyString" />'s shape algebra — lengths, anchored affixes, character
///     families, casing and exclusions. The example-based suite pins one length and one affix per invariant
///     (<c>WithLength(10)</c>, <c>StartingWith("ORD-")</c>) and can only prove the layout right for those; these
///     quantify over the lengths <b>and</b> over the affix values themselves, so a filler budget that miscounts for
///     one length in a hundred, or a fragment check that lets one character through, is found and shrunk to its
///     minimal counter-example.
/// </summary>
/// <remarks>
///     <para>
///         Two of these properties are of a kind an example cannot express at all: the same call shape is legal or
///         illegal depending on the <b>argument value</b>. <c>WithLength(n).StartingWith(prefix)</c> holds exactly
///         when <c>n</c> leaves room for the prefix, and <c>Numeric().StartingWith(prefix)</c> holds exactly when
///         every character of the prefix is a digit. Both are written as a single property branching on that
///         relationship, so what gets tested is the boundary itself rather than a hand-picked point on either side.
///     </para>
///     <para>
///         Conflicts are asserted by <b>type</b> and at the fluent call that declares them, never on message text:
///         the messages are direction-aware — they name whichever side was declared first — so pinning them here
///         would test the wording instead of the algebra.
///     </para>
/// </remarks>
[TestSubject(typeof(AnyString))]
public sealed class StringShapeProperties {

    /// <summary>The alphabet an unconstrained generator draws from: ASCII letters and digits.</summary>
    private const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    ///     The number of characters a string may reach above whatever its declared minimum is — the library's
    ///     unconstrained spread, mirrored here because the suite is black-box.
    /// </summary>
    private const int DefaultLengthSpread = 16;

    /// <summary>The digits alone, so an affix can be drawn from inside <c>Numeric()</c>'s own charset.</summary>
    private const string DigitAlphabet = "0123456789";

    /// <summary>
    ///     The source alphabet for a <c>WithChars</c> pool. It reaches beyond letters and digits — a custom pool is
    ///     precisely how a caller expresses an alphabet the named sets cannot — while staying free of surrogates,
    ///     which the pool rejects as an argument error.
    /// </summary>
    private const string PoolAlphabet = "ABCDEFabcdef0123456789-_.:/+*#@%&";

    #region Statics members declarations

    /// <summary>
    ///     A non-empty affix of at most <paramref name="maxLength" /> characters drawn from
    ///     <paramref name="alphabet" />. Affixes are drawn from an explicit alphabet rather than from arbitrary text
    ///     so that a charset conflict never fires by accident: the properties that probe the charset boundary declare
    ///     it deliberately, with an affix chosen for it.
    /// </summary>
    private static Gen<string> Affix(string alphabet, int maxLength) {
        return from characters in Gen.NonEmptyListOf(Gen.Elements(alphabet.ToCharArray()))
               from length in Gen.Choose(1, maxLength)
               select new string(characters.Take(length).ToArray());
    }

    /// <summary>A non-empty, duplicate-free character pool for <see cref="AnyString.WithChars" />.</summary>
    private static Gen<string> CharacterPool() {
        return Gen.NonEmptyListOf(Gen.Elements(PoolAlphabet.ToCharArray()))
                  .Select(characters => new string(characters.Distinct().Take(12).ToArray()));
    }

    /// <summary>
    ///     Applies one of the four character families by index — 0 <c>Alpha</c>, 1 <c>Numeric</c>, 2
    ///     <c>AlphaNumeric</c>, 3 <c>WithChars</c> — so a property can quantify over the family itself instead of
    ///     restating the same invariant four times over.
    /// </summary>
    private static AnyString ApplyCharacterFamily(AnyString generator, int family, string pool) {
        return family switch {
            0 => generator.Alpha(),
            1 => generator.Numeric(),
            2 => generator.AlphaNumeric(),
            _ => generator.WithChars(pool)
        };
    }

    /// <summary>Whether <paramref name="character" /> belongs to the alphabet the family <paramref name="family" /> selects.</summary>
    private static bool AllowedByFamily(char character, int family, string pool) {
        return family switch {
            0 => IsAsciiLetter(character),
            1 => IsAsciiDigit(character),
            2 => IsAsciiLetter(character) || IsAsciiDigit(character),
            _ => pool.Contains(character)
        };
    }

    /// <summary>Applies one of the two casings, so a property can quantify over the casing itself.</summary>
    private static AnyString ApplyCasing(AnyString generator, bool upper) {
        return upper ? generator.UpperCase() : generator.LowerCase();
    }

    /// <summary>Anchors <paramref name="affix" /> at one end or the other, so a property can quantify over the end.</summary>
    private static AnyString ApplyAffix(AnyString generator, bool asSuffix, string affix) {
        return asSuffix ? generator.EndingWith(affix) : generator.StartingWith(affix);
    }

    // char.IsAsciiLetter/IsAsciiDigit are .NET 7+, and this suite also runs on the netstandard2.0 asset from the
    // net472 floor — so the two classifications the library itself uses are restated here.

    private static bool IsAsciiLetter(char character) {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsAsciiDigit(char character) {
        return character is >= '0' and <= '9';
    }

    #endregion

    [Fact(DisplayName = "WithLength fixes the length exactly, for every length.")]
    public void WithLengthFixesTheLengthExactly() {
        Prop.ForAll(Generators.Count(40).ToArbitrary(),
                    length => Expect.EveryDraw(Any.String().WithLength(length), value => value.Length == length))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithMinLength is an inclusive floor: every draw is at least that long.")]
    public void WithMinLengthIsAnInclusiveFloor() {
        Prop.ForAll(Generators.Count(40).ToArbitrary(),
                    minimum => Expect.EveryDraw(Any.String().WithMinLength(minimum), value => value.Length >= minimum))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithMaxLength is an inclusive ceiling: every draw is at most that long.")]
    public void WithMaxLengthIsAnInclusiveCeiling() {
        Prop.ForAll(Generators.Count(40).ToArbitrary(),
                    maximum => Expect.EveryDraw(Any.String().WithMaxLength(maximum), value => value.Length <= maximum))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithMaxLength only caps: it never widens the draw beyond the unconstrained spread.")]
    public void WithMaxLengthNeverWidensTheDraw() {
        // ADR-0050: a maximum is a permission, not a size hint. It composes with the default spread instead of
        // replacing it, so declaring a loose cap must keep yielding the small unconstrained string. The maxima
        // generated here straddle the spread on both sides — that is where the old "maximum becomes the target"
        // behaviour and this one disagree.
        Prop.ForAll(Generators.WithEdges(Generators.Count(200), 0, 1, DefaultLengthSpread, DefaultLengthSpread + 1, 200).ToArbitrary(),
                    maximum => Expect.EveryDraw(Any.String().WithMaxLength(maximum),
                                                value => value.Length <= Math.Min(maximum, DefaultLengthSpread)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A minimum, not a maximum, is what enlarges a string: the draw spans the spread above it.")]
    public void WithMinLengthIsWhatEnlargesTheDraw() {
        // The counterpart of the property above: since a maximum cannot widen the draw, a minimum is the only
        // one-sided bound that can. Its draw stays within the spread above it, so asking for large strings costs
        // exactly what was asked for and nothing more.
        Prop.ForAll(Generators.WithEdges(Generators.Count(200), 0, 1, 200).ToArbitrary(),
                    minimum => Expect.EveryDraw(Any.String().WithMinLength(minimum),
                                                value => value.Length >= minimum && value.Length <= minimum + DefaultLengthSpread))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithLengthBetween bounds the length inclusively, for every bound pair.")]
    public void WithLengthBetweenIsAnInclusiveRange() {
        Prop.ForAll(Generators.OrderedPair(Generators.Count(40)).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.String().WithLengthBetween(bounds.Min, bounds.Max),
                                               value => value.Length >= bounds.Min && value.Length <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Crossed WithLengthBetween arguments are an argument error, never a silent swap.")]
    public void CrossedLengthBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Generators.Count(40)).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.String().WithLengthBetween(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "NonEmpty never yields the empty string, under every maximum length that leaves room.")]
    public void NonEmptyNeverYieldsTheEmptyString() {
        Prop.ForAll(Generators.Count(40).ToArbitrary(),
                    maximum => {
                        // NonEmpty is a minimum of one character, so capping the length at zero leaves nothing to
                        // draw: the pair is rejected at declaration, not at generation.
                        if (maximum == 0) {
                            return Expect.Throws<ConflictingAnyConstraintException>(() => Any.String().NonEmpty().WithMaxLength(0));
                        }

                        return Expect.EveryDraw(Any.String().NonEmpty().WithMaxLength(maximum),
                                                value => value.Length >= 1 && value.Length <= maximum);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "StartingWith anchors the prefix, whatever the prefix.")]
    public void StartingWithAnchorsThePrefix() {
        Prop.ForAll(Affix(DefaultAlphabet, 8).ToArbitrary(),
                    prefix => Expect.EveryDraw(Any.String().StartingWith(prefix),
                                               value => value.StartsWith(prefix, StringComparison.Ordinal)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "EndingWith anchors the suffix, whatever the suffix.")]
    public void EndingWithAnchorsTheSuffix() {
        Prop.ForAll(Affix(DefaultAlphabet, 8).ToArbitrary(),
                    suffix => Expect.EveryDraw(Any.String().EndingWith(suffix),
                                               value => value.EndsWith(suffix, StringComparison.Ordinal)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Containing embeds the value, whatever the value.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2249:Consider using String.Contains instead of String.IndexOf",
                                                     Justification =
                                                         "string.Contains(string, StringComparison) is not on the netstandard2.0 / net472 floor this suite runs " +
                                                         "against (ADR-0022); IndexOf with the same StringComparison.Ordinal carries the identical comparison and " +
                                                         "compiles on every leg. The rule is right on net10.0 only.")]
    public void ContainingEmbedsTheValue() {
        Prop.ForAll(Affix(DefaultAlphabet, 8).ToArbitrary(),
                    fragment => Expect.EveryDraw(Any.String().Containing(fragment),
                                                 // string.Contains(string, StringComparison) is not on the netstandard2.0
                                                 // floor; IndexOf carries the same ordinal comparison.
                                                 value => value.IndexOf(fragment, StringComparison.Ordinal) >= 0))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Every character family draws only from its own alphabet, at every length.")]
    public void CharacterFamiliesDrawOnlyFromTheirOwnAlphabet() {
        Gen<(int Family, string Pool, int Length)> cases =
            from family in Gen.Choose(0, 3)
            from pool in CharacterPool()
            from length in Generators.Count(20)
            select (Family: family, Pool: pool, Length: length);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => Expect.EveryDraw(ApplyCharacterFamily(Any.String(), testCase.Family, testCase.Pool).WithLength(testCase.Length),
                                                 value => value.All(character => AllowedByFamily(character, testCase.Family, testCase.Pool))))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A casing constrains every cased character, at every length.")]
    public void ACasingConstrainsEveryCasedCharacter() {
        Gen<(bool Upper, int Length)> cases =
            from upper in Gen.Elements(false, true)
            from length in Generators.Count(20)
            select (Upper: upper, Length: length);

        Prop.ForAll(cases.ToArbitrary(),
                    // A casing constrains the letters only: digits stay drawable under either of them.
                    testCase => Expect.EveryDraw(ApplyCasing(Any.String(), testCase.Upper).WithLength(testCase.Length),
                                                 value => value.All(character => testCase.Upper
                                                                                     ? !(character is >= 'a' and <= 'z')
                                                                                     : !(character is >= 'A' and <= 'Z'))))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithLength and StartingWith hold together exactly when the length leaves room for the prefix.")]
    public void ExactLengthAndPrefixHoldTogetherExactlyWhenThereIsRoom() {
        Gen<(string Prefix, int Length)> cases =
            from prefix in Affix(DefaultAlphabet, 8)
            from length in Generators.Count(12)
            select (Prefix: prefix, Length: length);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // The boundary an example test can only sample: one character below it the pair is a
                        // declaration-time conflict, at it and above it the pair is generable — same call shape,
                        // legality decided by the argument values.
                        if (testCase.Length < testCase.Prefix.Length) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.String().WithLength(testCase.Length).StartingWith(testCase.Prefix));
                        }

                        return Expect.EveryDraw(Any.String().WithLength(testCase.Length).StartingWith(testCase.Prefix),
                                                value => value.Length == testCase.Length
                                                         && value.StartsWith(testCase.Prefix, StringComparison.Ordinal));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Numeric accepts a prefix exactly when every one of its characters is a digit.")]
    public void NumericAcceptsAPrefixExactlyWhenEveryCharacterIsADigit() {
        // Half the prefixes come from the digits themselves and half from the full default alphabet, so both sides of
        // the boundary are reached often — Numeric().StartingWith("123") is valid where
        // Numeric().StartingWith("ORD-") conflicts, on the argument value alone.
        Gen<string> prefixes = Gen.OneOf(Affix(DigitAlphabet, 4), Affix(DefaultAlphabet, 4));

        Prop.ForAll(prefixes.ToArbitrary(),
                    prefix => prefix.All(IsAsciiDigit)
                                  ? Expect.EveryDraw(Any.String().Numeric().StartingWith(prefix),
                                                     value => value.StartsWith(prefix, StringComparison.Ordinal) && value.All(IsAsciiDigit))
                                  : Expect.Throws<ConflictingAnyConstraintException>(() => Any.String().Numeric().StartingWith(prefix)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Exclusions accumulate and never yield an excluded value, while preserving the declared shape.")]
    public void ExclusionsNeverYieldAnExcludedValue() {
        Prop.ForAll(Gen.Choose(3, 8).ToArbitrary(),
                    length => {
                        // The excluded values are drawn from the very generator they are then excluded from, so the
                        // exclusion is never vacuous. Three letters already allow 52^3 candidates, so removing a
                        // handful leaves the shape amply satisfiable: the redraw budget is not what is under test.
                        AnyString shaped   = Any.String().Alpha().WithLength(length);
                        string[]  excluded = Expect.Draws(shaped, 3).Distinct().ToArray();
                        string    banned   = shaped.Generate();
                        AnyString narrowed = shaped.Except(excluded).DifferentFrom(banned);

                        return Expect.EveryDraw(narrowed,
                                                value => value.Length == length
                                                         && value.All(IsAsciiLetter)
                                                         && !excluded.Contains(value)
                                                         && value != banned);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second character family conflicts unless it repeats the first, whichever two are combined.")]
    public void ASecondCharacterFamilyConflictsUnlessItRepeatsTheFirst() {
        Gen<(int First, int Second, string Pool)> cases =
            from first in Gen.Choose(0, 3)
            from second in Gen.Choose(0, 3)
            from pool in CharacterPool()
            select (First: first, Second: second, Pool: pool);

        Prop.ForAll(cases.ToArbitrary(),
                    // The pair may name the same family twice. Repeating it asks for the alphabet already in force, so
                    // it is a no-op and the alphabet still holds; naming a different family contradicts it. Both halves
                    // in one property, because the verdict follows the argument and not the call shape.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(ApplyCharacterFamily(ApplyCharacterFamily(Any.String(), testCase.First, testCase.Pool),
                                                                            testCase.Second, testCase.Pool).NonEmpty(),
                                                       value => value.All(character => AllowedByFamily(character, testCase.First, testCase.Pool)))
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                        () => ApplyCharacterFamily(ApplyCharacterFamily(Any.String(), testCase.First, testCase.Pool),
                                                                   testCase.Second, testCase.Pool)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second casing conflicts unless it repeats the first, whichever two are combined.")]
    public void ASecondCasingConflictsUnlessItRepeatsTheFirst() {
        Gen<(bool First, bool Second)> cases =
            from first in Gen.Elements(false, true)
            from second in Gen.Elements(false, true)
            select (First: first, Second: second);

        Prop.ForAll(cases.ToArbitrary(),
                    // Value-dependent legality: the same call is a no-op or a conflict depending on its argument, so
                    // the property branches on the value rather than on the call shape. Re-declaring the same casing
                    // asks for exactly the domain already in force; asking for the other one contradicts it.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(ApplyCasing(ApplyCasing(Any.String(), testCase.First), testCase.Second).NonEmpty(),
                                                       value => value.All(character => testCase.First ? !char.IsLower(character) : !char.IsUpper(character)))
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                        () => ApplyCasing(ApplyCasing(Any.String(), testCase.First), testCase.Second)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second exact length conflicts unless it repeats the first, whatever the two lengths.")]
    public void ASecondExactLengthConflictsUnlessItRepeatsTheFirst() {
        Gen<(int First, int Second)> cases =
            from first in Generators.Count(40)
            from second in Generators.Count(40)
            select (First: first, Second: second);

        Prop.ForAll(cases.ToArbitrary(),
                    // Repeating the same length is not a contradiction — the domain asked for is the one already in
                    // force — so it is a no-op, and the generator still produces exactly that length.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(Any.String().WithLength(testCase.First).WithLength(testCase.Second),
                                                       value => value.Length == testCase.First)
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                        () => Any.String().WithLength(testCase.First).WithLength(testCase.Second)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second prefix or a second suffix conflicts unless it repeats the first, whatever the two values.")]
    public void ASecondPrefixOrSuffixConflictsUnlessItRepeatsTheFirst() {
        Gen<(bool AsSuffix, string First, string Second)> cases =
            from asSuffix in Gen.Elements(false, true)
            from first in Affix(DefaultAlphabet, 6)
            from second in Affix(DefaultAlphabet, 6)
            select (AsSuffix: asSuffix, First: first, Second: second);

        Prop.ForAll(cases.ToArbitrary(),
                    // Same rule, on the two affix slots: an identical re-declaration is a no-op and the affix still
                    // holds; a different value for the same slot is the contradiction.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(ApplyAffix(ApplyAffix(Any.String(), testCase.AsSuffix, testCase.First),
                                                                  testCase.AsSuffix, testCase.Second),
                                                       value => testCase.AsSuffix ? value.EndsWith(testCase.First, StringComparison.Ordinal)
                                                                                  : value.StartsWith(testCase.First, StringComparison.Ordinal))
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                        () => ApplyAffix(ApplyAffix(Any.String(), testCase.AsSuffix, testCase.First),
                                                         testCase.AsSuffix, testCase.Second)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OneOf on an already constrained generator narrows it, and conflicts only when the values leave nothing.")]
    public void OneOfOnAConstrainedGeneratorNarrowsOrConflicts() {
        Gen<string[]> pools = Gen.NonEmptyListOf(Affix(DefaultAlphabet, 6)).Select(values => values.Distinct().ToArray());

        Gen<(int Length, string[] Pool)> cases =
            from length in Generators.Count(20)
            from pool in pools
            select (Length: length, Pool: pool);

        Prop.ForAll(cases.ToArbitrary(),
                    // The verdict follows the values, not the mere presence of a constraint: whatever the length and
                    // the pool, the generator survives exactly when some pooled value has that length, and every
                    // draw is then one of those values.
                    testCase => {
                        string[] surviving = testCase.Pool.Where(value => value.Length == testCase.Length).ToArray();

                        return surviving.Length == 0
                                   ? Expect.Throws<ConflictingAnyConstraintException>(
                                       () => Any.String().WithLength(testCase.Length).OneOf(testCase.Pool))
                                   : Expect.EveryDraw(Any.String().WithLength(testCase.Length).OneOf(testCase.Pool),
                                                      value => surviving.Contains(value));
                    })
            .QuickCheckThrowOnFailure();
    }

    /// <remarks>
    ///     Quantified over a <b>length</b> constraint on purpose. Order is immaterial for every constraint the
    ///     constructive path accepts on its own, which is what this property covers; the one exception — a fragment
    ///     combination the layout budget rejects before any value set can reinterpret it as a filter — is a decision,
    ///     not an invariant, and belongs to the example suite that pins it.
    /// </remarks>
    [Fact(DisplayName = "A length constraint and a value set reach the same domain whichever is declared first.")]
    public void OneOfIsOrderIndependentWithALengthConstraint() {
        Gen<string[]> pools = Gen.NonEmptyListOf(Affix(DefaultAlphabet, 6)).Select(values => values.Distinct().ToArray());

        Gen<(int Length, string[] Pool)> cases =
            from length in Generators.Count(20)
            from pool in pools
            select (Length: length, Pool: pool);

        Prop.ForAll(cases.ToArbitrary(),
                    // Declaration order is a call-site accident; the domain it describes is not. Both orders conflict
                    // together, or both draw from the same surviving values — the verdict alone would not catch an
                    // order that survives with a different domain.
                    testCase => {
                        string[] surviving = testCase.Pool.Where(value => value.Length == testCase.Length).ToArray();

                        return surviving.Length == 0
                                   ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.String().OneOf(testCase.Pool).WithLength(testCase.Length))
                                     && Expect.Throws<ConflictingAnyConstraintException>(() => Any.String().WithLength(testCase.Length).OneOf(testCase.Pool))
                                   : Expect.EveryDraw(Any.String().OneOf(testCase.Pool).WithLength(testCase.Length), value => surviving.Contains(value))
                                     && Expect.EveryDraw(Any.String().WithLength(testCase.Length).OneOf(testCase.Pool), value => surviving.Contains(value));
                    })
            .QuickCheckThrowOnFailure();
    }

}
