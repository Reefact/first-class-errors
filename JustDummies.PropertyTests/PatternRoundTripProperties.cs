#region Usings declarations

using System.Globalization;
using System.Text.RegularExpressions;

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="AnyPattern" />, built around a <b>round trip</b>: FsCheck generates a
///     pattern from the supported regular subset, JustDummies generates a value from it, and the real .NET regex
///     engine is asked whether that value matches. The example-based suite pins some fifty hand-written patterns and
///     can only prove the walk right for those; here the pattern itself is the quantified variable, so a class,
///     quantifier or grouping combination nobody thought to write down is reached, and a failure is shrunk to its
///     minimal counter-example.
/// </summary>
/// <remarks>
///     <para>
///         The oracle is <c>^(?:P)$</c> rather than <c>P</c>: JustDummies generates a <b>whole</b> matching string, so
///         anchoring turns the partial-match <see cref="Regex.IsMatch(string)" /> into a whole-string test that catches
///         under-generation (too few characters) and over-generation (trailing junk) alike, and keeps a top-level
///         alternation from binding looser than intended.
///     </para>
///     <para>
///         The pattern generator is deliberately narrower than the supported subset. It emits <b>no anchors</b> — the
///         wrapper supplies them, and a generated <c>^</c> or <c>$</c> would either duplicate them or land where the
///         parser rightly refuses it — and it never nests an unbounded quantifier inside a repeated group, because
///         <c>(a+)+</c> legitimately overruns the generation ceiling with an <see cref="AnyGenerationException" />.
///         Unbounded quantifiers therefore apply to single-character atoms only, group repeats stay at or below two,
///         and nesting stops at <see cref="MaxNestingDepth" />: a narrow round trip that always holds is worth more
///         than a broad one that flakes.
///     </para>
///     <para>
///         Every rejection is asserted by <b>type</b>, never on message text, and the taxonomy itself is under test:
///         a well-formed but non-regular construct is an <see cref="UnsupportedRegexException" />, while a pattern the
///         real engine cannot compile is a plain <see cref="ArgumentException" />. The two are not interchangeable, so
///         the malformed property asks the real engine for its verdict first, and refuses an unsupported-construct
///         answer.
///     </para>
/// </remarks>
[TestSubject(typeof(AnyPattern))]
public sealed class PatternRoundTripProperties {

    /// <summary>How deep a generated pattern may nest groups. Shallow on purpose — see the class remarks.</summary>
    private const int MaxNestingDepth = 3;

    /// <summary>How many parts a generated concatenation may hold.</summary>
    private const int MaxSequenceParts = 3;

    /// <summary>How many branches a generated alternation may hold.</summary>
    private const int MaxAlternationBranches = 3;

    /// <summary>
    ///     How many repetitions above its minimum an unbounded quantifier may add — the library's own
    ///     <c>RegexRepeat.UnboundedExtra</c>, restated here because it is internal.
    /// </summary>
    private const int UnboundedExtra = 8;

    /// <summary>
    ///     The character ceiling a generation may not cross — the library's own <c>AnyPattern.GenerationLimit</c>,
    ///     restated here because it is private. A quantifier minimum above it can only be refused, never built.
    /// </summary>
    private const int GenerationLimit = 65536;

    /// <summary>
    ///     How many values the minimum-honouring property draws per case. Kept low because most of its cases ask for a
    ///     minimum far above <see cref="GenerationLimit" />, and each such draw walks the ceiling before refusing.
    /// </summary>
    private const int MinimumHonouredDrawCount = 3;

    /// <summary>
    ///     How many values the alternation-reachability property draws. Four branches missed by 120 uniform draws is a
    ///     one-in-a-quadrillion event, so the property is deterministic in practice while staying a genuine reachability
    ///     claim rather than a containment one.
    /// </summary>
    private const int BranchSampleCount = 120;

    /// <summary>
    ///     Characters that stand for themselves in a pattern. Metacharacters are excluded (they appear only escaped),
    ///     and so are the space and <c>#</c>: those two are the only characters
    ///     <see cref="RegexOptions.IgnorePatternWhitespace" /> reads differently, and the property that asserts that
    ///     option is refused must not risk the <see cref="Regex" /> constructor throwing before JustDummies is reached.
    /// </summary>
    private const string LiteralAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_:/@=,;!~";

    /// <summary>Letters and digits — what class ranges, hexadecimal escapes and alternation branches are built from.</summary>
    private const string AlphaNumericAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>A group name must open on a letter: a name opening on a digit is an explicit capture number instead.</summary>
    private const string NameHeadAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>After the first character a group name may hold any word character.</summary>
    private const string NameTailAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789_";

    #region Statics members declarations

    /// <summary>
    ///     Escaped single characters: the metacharacters, plus the control escapes the parser resolves to a real
    ///     character rather than to its letter. <c>\n</c> and <c>\r</c> are left out — nothing in the subset needs
    ///     them, and a newline is the one character whose interaction with <c>$</c> in the oracle is not a plain
    ///     end-of-string test.
    /// </summary>
    private static readonly string[] EscapedLiterals = {
        @"\.", @"\*", @"\+", @"\?", @"\(", @"\)", @"\[", @"\]", @"\{", @"\}", @"\|", @"\\", @"\^", @"\$", @"\-", @"\/",
        @"\t", @"\a", @"\f", @"\v", @"\e"
    };

    /// <summary>The class shorthands, all six of them.</summary>
    private static readonly string[] Shorthands = { @"\d", @"\D", @"\w", @"\W", @"\s", @"\S" };

    /// <summary>
    ///     The shorthands a <b>negated</b> class may hold. Only the positive ones: a negated class that excludes the
    ///     whole printable-ASCII universe (<c>[^\s\S]</c>, <c>[^\w\W]</c>) is refused as unsupported, and excluding
    ///     digits, word characters and whitespace always leaves the punctuation behind.
    /// </summary>
    private static readonly string[] PositiveShorthands = { @"\d", @"\w", @"\s" };

    /// <summary>
    ///     Escaped members a character class may hold. The control escapes are dropped and every punctuation member is
    ///     escaped, so no bare <c>-</c>, <c>[</c> or <c>]</c> can ever turn a member into a range endpoint, a
    ///     class subtraction or an early close.
    /// </summary>
    private static readonly string[] ClassEscapedMembers = {
        @"\.", @"\*", @"\+", @"\?", @"\(", @"\)", @"\[", @"\]", @"\{", @"\}", @"\|", @"\\", @"\^", @"\$", @"\-", @"\/"
    };

    /// <summary>A class range stays inside one of these, so its endpoints are always in order and always readable.</summary>
    private static readonly string[] RangeAlphabets = {
        "abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "0123456789"
    };

    /// <summary>
    ///     Options that may accompany <see cref="RegexOptions.IgnorePatternWhitespace" /> without changing whether the
    ///     pattern compiles, so the refusal is proven to depend on that one flag and not on being alone.
    /// </summary>
    private static readonly RegexOptions[] CompanionOptions = {
        RegexOptions.None, RegexOptions.IgnoreCase, RegexOptions.Singleline, RegexOptions.Multiline,
        RegexOptions.ExplicitCapture, RegexOptions.CultureInvariant
    };

    /// <summary>
    ///     Constructs the real engine compiles but JustDummies declines: they are well-formed, and either non-regular
    ///     (lookaround, backreference, balancing group, word boundary) or not honourable by a plain left-to-right walk
    ///     (atomic group, class subtraction, Unicode category, group option, conditional, comment).
    /// </summary>
    private static readonly string[] UnsupportedConstructs = {
        "(?=abc)", "(?!abc)", "(?<=abc)", "(?<!abc)", "(?>abc)", "(?#note)", "(?i:abc)", "(?(a)b|c)",
        @"\bword", @"\Bx", @"\Ax", @"x\z", @"x\Z", @"\Gx", @"\p{L}", @"\P{L}", @"(\w)\1", @"(?<n>a)\k<n>",
        "(?<a>y)?(?<-a>x)", "[a-z-[aeiou]]"
    };

    /// <summary>
    ///     Patterns the real .NET engine refuses to compile. JustDummies must mirror that verdict as a plain
    ///     <see cref="ArgumentException" /> — reporting them as unsupported would claim the caller wrote something
    ///     merely out of scope rather than something broken.
    /// </summary>
    private static readonly string[] MalformedPatterns = {
        "[a-", "(abc", "abc)", "(?", "a{3,1}", "*abc", @"a\", "a*+", "a**", "a*??", "[]", @"\q", @"\x4", @"\c1",
        "{2}", "(?<>a)", "(?<1a>x)", "(?<0>x)", "(?<01>x)", "(?'0'x)", "(?<a b>x)", "(?<a.b>x)"
    };

    /// <summary>
    ///     The oracle: the pattern anchored at both ends, so a partial match cannot pass for a whole one. A match
    ///     timeout is attached as a safety net — a generated pattern that somehow made the backtracking engine crawl
    ///     should fail the suite, never hang it.
    /// </summary>
    private static Regex Anchored(string pattern, RegexOptions options) {
        return new Regex("^(?:" + pattern + ")$", options, TimeSpan.FromSeconds(10));
    }

    /// <summary>Whether the real .NET engine compiles <paramref name="pattern" /> at all — the reference verdict on well-formedness.</summary>
    private static bool CompilesInTheRealEngine(string pattern) {
        try {
            _ = new Regex(pattern);

            return true;
        } catch (ArgumentException) {
            return false;
        }
    }

    /// <summary>
    ///     Whether <paramref name="pattern" /> is refused as <b>malformed</b> — an <see cref="ArgumentException" />.
    ///     An <see cref="UnsupportedRegexException" /> is explicitly <b>not</b> an acceptable answer: the two verdicts
    ///     say different things to the caller, so the taxonomy is asserted rather than merely "it threw".
    /// </summary>
    private static bool ThrowsMalformed(string pattern) {
        try {
            _ = Any.StringMatching(pattern);

            return false;
        } catch (UnsupportedRegexException) {
            return false;
        } catch (ArgumentException) {
            return true;
        }
    }

    /// <summary>An integer rendered for a quantifier bound, independently of the ambient culture.</summary>
    private static string Digits(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A pattern from the supported subset, nesting at most <see cref="MaxNestingDepth" /> levels of groups.</summary>
    private static Gen<string> SupportedPattern() {
        return Pattern(MaxNestingDepth);
    }

    /// <summary>
    ///     An alternation of one to <see cref="MaxAlternationBranches" /> branches. One branch is kept in the mix: the
    ///     unalternated pattern is the common case, and dropping it would leave every generated pattern lopsided.
    /// </summary>
    private static Gen<string> Pattern(int depth) {
        Gen<string> branch = Sequence(depth);

        return from count in Gen.Choose(1, MaxAlternationBranches)
               from branches in Gen.ArrayOf(branch, count)
               select string.Join("|", branches);
    }

    /// <summary>A concatenation of one to <see cref="MaxSequenceParts" /> quantified atoms.</summary>
    private static Gen<string> Sequence(int depth) {
        Gen<string> part = Quantified(depth);

        return from count in Gen.Choose(1, MaxSequenceParts)
               from parts in Gen.ArrayOf(part, count)
               select string.Concat(parts);
    }

    /// <summary>
    ///     An atom with an optional quantifier. The split between the two quantifier generators is what keeps the
    ///     recursion safe: only a single-character atom may carry an unbounded quantifier, so no <c>(a+)+</c> — a
    ///     pattern that legitimately overruns the generation ceiling — can ever be built.
    /// </summary>
    private static Gen<string> Quantified(int depth) {
        Gen<string> quantifiedLeaf = from atom in Leaf()
                                     from quantifier in LeafQuantifier()
                                     select atom + quantifier;

        if (depth <= 0) { return quantifiedLeaf; }

        Gen<string> quantifiedGroup = from atom in Group(depth)
                                      from quantifier in GroupQuantifier()
                                      select atom + quantifier;

        // Groups stay a minority of the atoms: the nesting is what makes a pattern expensive, both to generate and
        // to match, and a suite of 100 cases is worth more spent on breadth than on depth.
        return Gen.Frequency<string>((6, quantifiedLeaf), (2, quantifiedGroup));
    }

    /// <summary>A group in each of the four supported forms: capturing, non-capturing, and named in both syntaxes.</summary>
    private static Gen<string> Group(int depth) {
        Gen<string> body = Pattern(depth - 1);

        return from inner in body
               from name in GroupName()
               from kind in Gen.Choose(0, 3)
               select kind switch {
                   0 => "(" + inner + ")",
                   1 => "(?:" + inner + ")",
                   2 => "(?<" + name + ">" + inner + ")",
                   _ => "(?'" + name + "'" + inner + ")"
               };
    }

    /// <summary>
    ///     A group name the real engine accepts: a letter followed by up to two word characters. Names opening on a
    ///     digit are left out — those are explicit capture <i>numbers</i>, with their own validity rules, and the
    ///     malformed property covers them instead.
    /// </summary>
    private static Gen<string> GroupName() {
        return from head in Gen.Elements(NameHeadAlphabet.ToCharArray())
               from tail in Gen.ArrayOf(Gen.Elements(NameTailAlphabet.ToCharArray()), 2)
               from length in Gen.Choose(0, 2)
               select head.ToString() + new string(tail, 0, length);
    }

    /// <summary>
    ///     An atom that emits exactly one character: a literal, an escaped literal, a shorthand, a hexadecimal escape,
    ///     a character class or the dot.
    /// </summary>
    private static Gen<string> Leaf() {
        return Gen.Frequency<string>((8, Gen.Elements(LiteralAlphabet.ToCharArray()).Select(character => character.ToString())),
                                     (3, Gen.Elements(EscapedLiterals)),
                                     (4, Gen.Elements(Shorthands)),
                                     (2, HexEscape()),
                                     (4, CharacterClass()),
                                     (1, Gen.Constant(".")));
    }

    /// <summary>
    ///     Single-character atoms only — the subset the quantifier-length properties need, where the generated length
    ///     <b>is</b> the repetition count. Hexadecimal escapes are dropped so that a quantifier can never be mistaken
    ///     for a continuation of the escape's digits.
    /// </summary>
    private static Gen<string> SingleCharacterAtom() {
        return Gen.Frequency<string>((4, Gen.Elements(LiteralAlphabet.ToCharArray()).Select(character => character.ToString())),
                                     (2, Gen.Elements(EscapedLiterals)),
                                     (2, Gen.Elements(Shorthands)),
                                     (2, CharacterClass()),
                                     (1, Gen.Constant(".")));
    }

    /// <summary>A <c>\xHH</c> or <c>\uHHHH</c> escape naming a letter or a digit, so the escaped character stays printable.</summary>
    private static Gen<string> HexEscape() {
        return from character in Gen.Elements(AlphaNumericAlphabet.ToCharArray())
               from wide in Gen.Elements(new[] { false, true })
               select wide
                          ? @"\u" + ((int)character).ToString("X4", CultureInfo.InvariantCulture)
                          : @"\x" + ((int)character).ToString("X2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     A character class of one to three members, negated or not. A negated class draws from the restricted member
    ///     set (see <see cref="PositiveShorthands" />), so the negation always leaves characters to draw from.
    /// </summary>
    private static Gen<string> CharacterClass() {
        return from negated in Gen.Elements(new[] { false, true })
               from count in Gen.Choose(1, 3)
               from members in Gen.ArrayOf(ClassMember(negated), count)
               select "[" + (negated ? "^" : string.Empty) + string.Concat(members) + "]";
    }

    /// <summary>One member of a character class: a single character, a range, a shorthand, or an escaped punctuation member.</summary>
    private static Gen<string> ClassMember(bool negatedClass) {
        Gen<string> single    = Gen.Elements(AlphaNumericAlphabet.ToCharArray()).Select(character => character.ToString());
        Gen<string> shorthand = Gen.Elements(negatedClass ? PositiveShorthands : Shorthands);

        if (negatedClass) { return Gen.Frequency<string>((4, single), (3, ClassRange()), (2, shorthand)); }

        return Gen.Frequency<string>((4, single), (3, ClassRange()), (2, shorthand), (2, Gen.Elements(ClassEscapedMembers)));
    }

    /// <summary>A range whose endpoints come from the same alphabet, so the low endpoint never exceeds the high one.</summary>
    private static Gen<string> ClassRange() {
        return from alphabet in Gen.Elements(RangeAlphabets)
               from first in Gen.Choose(0, alphabet.Length - 1)
               from second in Gen.Choose(0, alphabet.Length - 1)
               select $"{alphabet[Math.Min(first, second)]}-{alphabet[Math.Max(first, second)]}";
    }

    /// <summary>
    ///     A quantifier for a single-character atom: nothing, <c>?</c>, a bounded <c>{n}</c>/<c>{n,m}</c>, or an
    ///     unbounded <c>*</c>/<c>+</c>/<c>{n,}</c>. An unbounded quantifier is safe here precisely because the atom
    ///     under it is one character wide.
    /// </summary>
    private static Gen<string> LeafQuantifier() {
        Gen<string> bounded = from minimum in Gen.Choose(0, 2)
                              from extra in Gen.Choose(0, 2)
                              from exact in Gen.Elements(new[] { false, true })
                              select exact
                                         ? "{" + Digits(minimum) + "}"
                                         : "{" + Digits(minimum) + "," + Digits(minimum + extra) + "}";

        Gen<string> unbounded = Gen.OneOf(Gen.Elements(new[] { "*", "+" }),
                                          Gen.Choose(0, 2).Select(minimum => "{" + Digits(minimum) + ",}"));

        return WithOptionalLazyMarker(Gen.Frequency<string>((5, Gen.Constant(string.Empty)),
                                                            (2, Gen.Constant("?")),
                                                            (2, bounded),
                                                            (2, unbounded)));
    }

    /// <summary>
    ///     A quantifier for a group: bounded only, and never above two repetitions. Both restrictions are about size —
    ///     an unbounded repeat of a group is the runaway case, and a large bounded one multiplies out just as fast.
    /// </summary>
    private static Gen<string> GroupQuantifier() {
        Gen<string> bounded = from minimum in Gen.Choose(0, 1)
                              from extra in Gen.Choose(0, 1)
                              select "{" + Digits(minimum) + "," + Digits(minimum + extra) + "}";

        return WithOptionalLazyMarker(Gen.Frequency<string>((6, Gen.Constant(string.Empty)),
                                                            (2, Gen.Constant("?")),
                                                            (2, bounded)));
    }

    /// <summary>
    ///     Occasionally makes a quantifier lazy. A lazy marker changes which match the engine prefers, never which
    ///     strings match, so it must leave the generated language untouched — worth quantifying over rather than
    ///     assuming. It is never appended to an absent quantifier, where a bare <c>?</c> would be a quantifier of its own.
    /// </summary>
    private static Gen<string> WithOptionalLazyMarker(Gen<string> quantifiers) {
        return from quantifier in quantifiers
               from lazy in Gen.Choose(0, 3)
               select quantifier.Length == 0 || lazy != 0 ? quantifier : quantifier + "?";
    }

    /// <summary>
    ///     A minimum for an unbounded quantifier, spread across the whole legal range rather than the small end alone.
    ///     The three pinned regions are where the count arithmetic can go wrong: buildable minimums, the ceiling
    ///     crossing, and the top of the int range where adding <see cref="UnboundedExtra" /> overflows.
    /// </summary>
    private static Gen<int> UnboundedMinimum() {
        return Gen.Frequency<int>((4, Gen.Choose(0, UnboundedExtra)),
                                  (2, Gen.Choose(GenerationLimit - UnboundedExtra, GenerationLimit + UnboundedExtra)),
                                  (3, Gen.Choose(int.MaxValue - (2 * UnboundedExtra), int.MaxValue)),
                                  (2, Gen.Choose(UnboundedExtra + 1, int.MaxValue)));
    }

    /// <summary>A short literal word, the material of the alternation-reachability property's branches.</summary>
    private static Gen<string> Word() {
        return from characters in Gen.ArrayOf(Gen.Elements(AlphaNumericAlphabet.ToCharArray()), 3)
               from length in Gen.Choose(1, 3)
               select new string(characters, 0, length);
    }

    #endregion

    [Fact(DisplayName = "Round trip: every value generated from a supported pattern is fully matched by the real .NET engine.")]
    public void EveryGeneratedValueIsMatchedByTheRealEngine() {
        Prop.ForAll(SupportedPattern().ToArbitrary(),
                    pattern => {
                        // The oracle is built once per case and reused across the draws: it is the pattern that varies
                        // between cases, not between draws.
                        Regex oracle = Anchored(pattern, RegexOptions.None);

                        return Expect.EveryDraw(Any.StringMatching(pattern), oracle.IsMatch);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An excluded value is never generated, and what is generated still matches the pattern.")]
    public void AnExcludedValueIsNeverGenerated() {
        Prop.ForAll(SupportedPattern().ToArbitrary(),
                    pattern => {
                        Regex  oracle   = Anchored(pattern, RegexOptions.None);
                        string existing = Any.StringMatching(pattern).Generate();

                        try {
                            // The exclusion is rejective: it removes a value without touching how the rest are built,
                            // so the round trip must still hold for every draw that comes back.
                            return Expect.EveryDraw(Any.StringMatching(pattern).DifferentFrom(existing),
                                                    value => !string.Equals(value, existing, StringComparison.Ordinal) && oracle.IsMatch(value));
                        } catch (AnyGenerationException) {
                            // A pattern whose language the exclusion leaves nothing of — a single-word one, most
                            // often. The bounded redraw reports its exhausted budget rather than ever returning the
                            // excluded value, which is the invariant under test.
                            return true;
                        }
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Round trip under IgnoreCase: every value is matched by the very regex it was generated from.")]
    public void IgnoreCaseValuesAreMatchedByTheSameRegex() {
        Prop.ForAll(SupportedPattern().ToArbitrary(),
                    pattern => {
                        // The Regex overload exists so a test can reuse the object its production code validates with,
                        // so the oracle is that same pattern under that same option — not a case-folded rewrite of it.
                        Regex source = new(pattern, RegexOptions.IgnoreCase);
                        Regex oracle = Anchored(pattern, RegexOptions.IgnoreCase);

                        return Expect.EveryDraw(Any.StringMatching(source), oracle.IsMatch);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Two contexts sharing a seed yield the same values, for every pattern and every seed.")]
    public void TheSameSeedYieldsTheSameValues() {
        Gen<(string Pattern, int Seed)> cases =
            from pattern in SupportedPattern()
            from seed in Generators.Seed()
            select (Pattern: pattern, Seed: seed);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // A whole sequence, not a single draw: a generator that reseeded itself per value would still
                        // agree on the first one.
                        List<string> first  = Expect.Draws(Any.WithSeed(testCase.Seed).StringMatching(testCase.Pattern), 8);
                        List<string> second = Expect.Draws(Any.WithSeed(testCase.Seed).StringMatching(testCase.Pattern), 8);

                        return first.SequenceEqual(second);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A bounded quantifier keeps the length inside its bounds, whatever the bounds and the form.")]
    public void BoundedQuantifiersKeepTheLengthInsideTheirBounds() {
        Gen<(string Atom, int Min, int Max, int Form)> cases =
            from atom in SingleCharacterAtom()
            from bounds in Generators.OrderedPair(Gen.Choose(0, 4))
            from form in Gen.Choose(0, 2)
            select (Atom: atom, Min: bounds.Min, Max: bounds.Max, Form: form);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // The three bounded forms are one rule with different bounds: '{n}' pins the count, '{n,m}'
                        // brackets it, and '?' is the fixed {0,1} case. Degenerate bounds are kept — '{0}' generating
                        // the empty string is a legitimate corner, not one to filter away.
                        (string Pattern, int Min, int Max) quantified = testCase.Form switch {
                            0 => (testCase.Atom + "{" + Digits(testCase.Min) + "}", testCase.Min, testCase.Min),
                            1 => (testCase.Atom + "{" + Digits(testCase.Min) + "," + Digits(testCase.Max) + "}", testCase.Min, testCase.Max),
                            _ => (testCase.Atom + "?", 0, 1)
                        };

                        return Expect.EveryDraw(Any.StringMatching(quantified.Pattern),
                                                value => value.Length >= quantified.Min && value.Length <= quantified.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An unbounded quantifier draws its minimum plus 0 to 8 repetitions, whatever the minimum and the form.")]
    public void UnboundedQuantifiersDrawTheMinimumPlusUpToEight() {
        Gen<(string Atom, int Min, int Form)> cases =
            from atom in SingleCharacterAtom()
            from minimum in Gen.Choose(0, 4)
            from form in Gen.Choose(0, 2)
            select (Atom: atom, Min: minimum, Form: form);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // '*' is '{0,}' and '+' is '{1,}', so the three forms differ only in their minimum. The ceiling
                        // is the claim worth quantifying: an unbounded quantifier has to pick a spread, and the library
                        // promises the same bounded one it uses everywhere else.
                        (string Pattern, int Min) quantified = testCase.Form switch {
                            0 => (testCase.Atom + "*", 0),
                            1 => (testCase.Atom + "+", 1),
                            _ => (testCase.Atom + "{" + Digits(testCase.Min) + ",}", testCase.Min)
                        };

                        return Expect.EveryDraw(Any.StringMatching(quantified.Pattern),
                                                value => value.Length >= quantified.Min
                                                         && value.Length <= quantified.Min + UnboundedExtra);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An unbounded quantifier never yields a value shorter than its minimum, whatever the minimum.")]
    public void UnboundedQuantifiersNeverYieldAValueShorterThanTheirMinimum() {
        Gen<(string Atom, int Min)> cases =
            from atom in SingleCharacterAtom()
            from minimum in UnboundedMinimum()
            select (Atom: atom, Min: minimum);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // The companion property above pins the spread for minimums small enough to build. This one
                        // states the half that holds for EVERY minimum, including those no generation can satisfy:
                        // either a value of the promised length comes out, or the ceiling refuses — but a value
                        // SHORTER than the minimum is not an outcome. That is the class the int-arithmetic overflow
                        // fell into, where a minimum near int.MaxValue wrapped negative and yielded the empty string.
                        AnyPattern generator = Any.StringMatching(testCase.Atom + "{" + Digits(testCase.Min) + ",}");

                        for (int draw = 0; draw < MinimumHonouredDrawCount; draw++) {
                            try {
                                // The atom is one character wide, so the generated length IS the repetition count.
                                int length = generator.Generate().Length;
                                if (length < testCase.Min || length > (long)testCase.Min + UnboundedExtra) { return false; }
                            } catch (AnyGenerationException) {
                                // Overrunning the ceiling is the honest refusal for a minimum too large to build.
                            }
                        }

                        return true;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Alternation reaches every declared branch and invents none, whatever the branches.")]
    public void AlternationReachesEveryBranchAndInventsNone() {
        Gen<(string[] Branches, int Seed)> cases =
            from words in Gen.ArrayOf(Word(), 4)
            from seed in Generators.Seed()
            select (Branches: words.Distinct().ToArray(), Seed: seed);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // Branches are plain literal words, so a drawn value IS the branch that produced it. SetEquals
                        // then states both halves at once: no branch is dead, and nothing outside the declared set
                        // can come out.
                        AnyPattern      generator = Any.WithSeed(testCase.Seed).StringMatching(string.Join("|", testCase.Branches));
                        HashSet<string> seen      = [.. Expect.Draws(generator, BranchSampleCount)];

                        return seen.SetEquals(testCase.Branches);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "IgnorePatternWhitespace is refused as an argument error, whatever the pattern and the companion options.")]
    public void IgnorePatternWhitespaceIsAnArgumentError() {
        Gen<(string Pattern, RegexOptions Companion, int Seed)> cases =
            from pattern in SupportedPattern()
            from companion in Gen.Elements(CompanionOptions)
            from seed in Generators.Seed()
            select (Pattern: pattern, Companion: companion, Seed: seed);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // The Regex is built outside the assertion on purpose: only JustDummies' refusal is under test,
                        // never the .NET constructor's. The generated alphabets hold no whitespace and no '#', so this
                        // option cannot change whether the pattern compiles — only how JustDummies must answer.
                        Regex source = new(testCase.Pattern, RegexOptions.IgnorePatternWhitespace | testCase.Companion);

                        return Expect.Throws<ArgumentException>(() => Any.StringMatching(source))
                               && Expect.Throws<ArgumentException>(() => Any.WithSeed(testCase.Seed).StringMatching(source));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A null pattern is an argument error, on both overloads and on a seeded context.")]
    public void ANullPatternIsAnArgumentError() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => Expect.Throws<ArgumentNullException>(() => Any.StringMatching((string)null!))
                            && Expect.Throws<ArgumentNullException>(() => Any.StringMatching((Regex)null!))
                            && Expect.Throws<ArgumentNullException>(() => Any.WithSeed(seed).StringMatching((string)null!))
                            && Expect.Throws<ArgumentNullException>(() => Any.WithSeed(seed).StringMatching((Regex)null!)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A well-formed but unsupported construct is refused eagerly, alone and after any supported prefix.")]
    public void UnsupportedConstructsAreRefusedAsUnsupported() {
        Gen<(string Prefix, string Construct)> cases =
            from prefix in SupportedPattern()
            from construct in Gen.Elements(UnsupportedConstructs)
            select (Prefix: prefix, Construct: construct);

        Prop.ForAll(cases.ToArbitrary(),
                    // Each construct opens on '(', '[' or '\', none of which a preceding supported pattern can absorb,
                    // so appending one to an arbitrary prefix reaches the same refusal from a different parser state:
                    // the verdict must not depend on the construct sitting at position zero.
                    testCase => Expect.Throws<UnsupportedRegexException>(() => Any.StringMatching(testCase.Construct))
                                && Expect.Throws<UnsupportedRegexException>(() => Any.StringMatching(testCase.Prefix + testCase.Construct)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A malformed pattern is an argument error, never an unsupported-construct refusal.")]
    public void MalformedPatternsAreArgumentErrors() {
        Prop.ForAll(Gen.Elements(MalformedPatterns).ToArbitrary(),
                    // The real engine is asked first, so the property states the taxonomy rather than restating the
                    // list: a pattern .NET itself cannot compile is broken, and JustDummies must say so as an argument
                    // error. An UnsupportedRegexException here would be the wrong answer, and ThrowsMalformed rejects it.
                    pattern => !CompilesInTheRealEngine(pattern) && ThrowsMalformed(pattern))
            .QuickCheckThrowOnFailure();
    }

}
