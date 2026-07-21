#region Usings declarations

using System.Text.RegularExpressions;

using NFluent;

#endregion

namespace Dummies.UnitTests;

public sealed class AnyPatternTests {

    #region Statics members declarations

    private const int SampleCount = 200;

    // The oracle: a generated value is correct iff the REAL .NET regex engine fully matches it. Anchoring with
    // ^(?:...)$ turns the partial-match IsMatch into a whole-string test, so it catches both under-generation
    // (too few characters) and over-generation (trailing junk), and handles top-level alternation correctly.
    private static void AssertMatches(string value, string pattern, RegexOptions options = RegexOptions.None) {
        Assert.True(Regex.IsMatch(value, "^(?:" + pattern + ")$", options),
                    $"generated value {Display(value)} is not matched by /{pattern}/");
    }

    private static string Display(string value) {
        return "\"" + value.Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
    }

    // The oracle again, as a predicate: does the real .NET engine compile this pattern at all? Used to assert the
    // rejection taxonomy — a pattern the engine accepts must never be reported as malformed (ArgumentException).
    private static bool IsCompiledByTheRealEngine(string pattern) {
        try {
            _ = new Regex(pattern);

            return true;
        } catch (ArgumentException) {
            return false;
        }
    }

    #endregion

    [Theory(DisplayName = "Every generated value is fully matched by the real .NET regex engine.")]
    [InlineData(@"\d{8}")]
    [InlineData(@"^ORD-\d{8}$")]
    [InlineData(@"[A-Z]{3}")]
    [InlineData(@"[a-z]{2,5}")]
    [InlineData(@"(EUR|USD|GBP)")]
    [InlineData(@"[A-Za-z0-9_]+")]
    [InlineData(@"\w{4}\d{2}")]
    [InlineData(@"[^0-9]{3}")]
    [InlineData(@"colou?r")]
    [InlineData(@"a{2,4}b*c+")]
    [InlineData(@"(ab|cd){2,3}")]
    [InlineData(@"\d+\.\d{2}")]
    [InlineData(@"[A-F0-9]{6}")]
    [InlineData(@"(?:foo|bar)-\d+")]
    [InlineData(@"(?<year>\d{4})-(?<month>\d{2})")]
    [InlineData(@"(?'tag'\d{2})")]
    [InlineData(@"(?<1>x)")]          // explicitly-numbered group: a valid capture NUMBER, not an invalid name
    [InlineData(@"(?'2'y)")]          // ...same, quote form
    [InlineData(@"(?<10>ab)")]        // a multi-digit group number stays valid
    [InlineData(@"(?<a1>xy)")]        // a named group whose name merely contains digits stays valid
    [InlineData(@"^a$|^b$")]
    [InlineData(@"^^abc")]            // a run of boundary anchors is a no-op, exactly as in the real engine
    [InlineData(@"abc$$")]
    [InlineData(@"^*abc")]            // a quantifier on a zero-width anchor is a no-op too
    [InlineData(@"abc$*")]
    [InlineData(@"^{2}xy")]
    [InlineData(@"^?abc$?")]
    [InlineData(@"[-[x]]")]           // a leading '-' is an ordinary hyphen, not a subtraction operator
    [InlineData(@"[-[abc]]")]
    [InlineData(@"[\d]{3}")]
    [InlineData(@"[-a-z]{2}")]
    [InlineData(@"[a-z-]{2}")]
    [InlineData(@"[a-b-z]{4}")]
    [InlineData(@"[]a]{3}")]
    [InlineData(@"[^]]{3}")]
    [InlineData(@"[\b]")]
    [InlineData(@"[\1]")]
    [InlineData(@"[\x30-\x39]{3}")]
    [InlineData(@"(a|aa|aaa)")]
    [InlineData(@"a+?b*?")]
    [InlineData(@"\x41\x2DB")]
    [InlineData(@"\a\t")]
    [InlineData(@"\e")]
    [InlineData(@"\cM")]
    [InlineData(@"\0")]
    [InlineData(@"\07")]
    [InlineData(@"a{x}")]
    [InlineData(@"{abc}")]
    [InlineData(@"a{2,")]
    [InlineData(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")]
    [InlineData(@"[A-Z]{2}\d{2}[A-Z0-9]{10}")]
    [InlineData(@"([01]\d|2[0-3]):[0-5]\d")]
    [InlineData(@"\d+\.\d+\.\d+(-[a-z]+(\.\d+)?)?")]
    [InlineData(@"\s")]
    [InlineData(@".")]
    [InlineData(@"")]
    public void GeneratedValuesMatchTheRealEngine(string pattern) {
        AnyContext context   = Any.WithSeed(20260718);
        AnyPattern generator = context.StringMatching(pattern);

        for (int i = 0; i < SampleCount; i++) {
            AssertMatches(generator.Generate(), pattern);
        }
    }

    [Fact(DisplayName = "Generated values vary from draw to draw whenever the pattern leaves room.")]
    public void GeneratedValuesVary() {
        foreach (string pattern in new[] { @"\d{8}", @"[A-Z]{3}", @"(EUR|USD|GBP)", @"[A-Za-z0-9_]+", @"a{2,4}b*c+" }) {
            AnyPattern      generator = Any.WithSeed(4242).StringMatching(pattern);
            HashSet<string> seen      = new();
            for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }
            Check.That(seen.Count).IsStrictlyGreaterThan(1);
        }
    }

    [Fact(DisplayName = "A fixed-shape pattern yields exactly that shape.")]
    public void FixedShape() {
        for (int i = 0; i < SampleCount; i++) {
            string reference = Any.StringMatching(@"^ORD-\d{8}$").Generate();
            Check.That(reference.Length).IsEqualTo(12);
            Check.That(reference).StartsWith("ORD-");
            Check.That(reference.Substring(4)).Matches("^[0-9]{8}$");
        }
    }

    [Fact(DisplayName = "Alternation draws each branch and only declared branches.")]
    public void Alternation() {
        HashSet<string> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            string value = Any.StringMatching("(EUR|USD|GBP)").Generate();
            Check.That(value == "EUR" || value == "USD" || value == "GBP").IsTrue();
            seen.Add(value);
        }

        Check.That(seen).Contains("EUR", "USD", "GBP");
    }

    [Fact(DisplayName = "Character classes, ranges and negation stay within their set.")]
    public void CharacterClasses() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.StringMatching("[A-Z]{3}").Generate()).Matches("^[A-Z]{3}$");
            Check.That(Any.StringMatching("[^0-9]{4}").Generate()).Matches("^[^0-9]{4}$");
            Check.That(Any.StringMatching(@"[\d]{5}").Generate()).Matches("^[0-9]{5}$");
        }
    }

    [Fact(DisplayName = "Bounded quantifiers stay within their bounds; unbounded ones draw the minimum plus 0 to 8.")]
    public void QuantifierBounds() {
        HashSet<int> starLengths = new();
        HashSet<int> plusLengths = new();
        HashSet<int> openLengths = new();

        for (int i = 0; i < SampleCount; i++) {
            int bounded = (Any.StringMatching("a{2,4}").Generate()).Length;
            Check.That(bounded is >= 2 and <= 4).IsTrue();

            starLengths.Add((Any.StringMatching("a*").Generate()).Length);
            plusLengths.Add((Any.StringMatching("a+").Generate()).Length);
            openLengths.Add((Any.StringMatching("a{2,}").Generate()).Length);
        }

        Check.That(starLengths.Min()).IsEqualTo(0);
        Check.That(starLengths.Max()).IsEqualTo(8);   // 0 + 0..8
        Check.That(plusLengths.Min()).IsEqualTo(1);
        Check.That(plusLengths.Max()).IsEqualTo(9);   // 1 + 0..8
        Check.That(openLengths.Min()).IsEqualTo(2);
        Check.That(openLengths.Max()).IsEqualTo(10);  // 2 + 0..8
    }

    [Fact(DisplayName = "Anchors are no-ops: the whole generated string is the match.")]
    public void AnchorsAreNoOps() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.StringMatching("^abc$").Generate()).IsEqualTo("abc");
        }
    }

    [Fact(DisplayName = "Repeated and quantified boundary anchors are no-ops, consistently for '^' and '$'.")]
    public void RepeatedAndQuantifiedAnchorsAreNoOps() {
        // '^^abc' was already accepted while the symmetric 'abc$$' was refused — an avoidable asymmetry, now closed.
        // Repeating or quantifying a zero-width boundary assertion never changes which strings match, so all of
        // these are no-ops, exactly as the real engine treats them.
        Check.That(Any.StringMatching(@"^^abc$$").Generate()).IsEqualTo("abc");
        Check.That(Any.StringMatching(@"^*abc$*").Generate()).IsEqualTo("abc");
        Check.That(Any.StringMatching(@"^{2}abc").Generate()).IsEqualTo("abc");
        Check.That(Any.StringMatching(@"^?abc$?").Generate()).IsEqualTo("abc");
    }

    [Fact(DisplayName = "A pattern that overruns the generation ceiling fails with an honest message, naming no false cause.")]
    public void OverLimitPatternFailsWithoutAFalseCause() {
        // '(a{1000}){1000}' deterministically asks for 1,000,000 characters — every quantifier is bounded, there is
        // no unbounded quantifier at all. The pattern parses fine (a resource ceiling is not a satisfiability
        // conflict); generation is what overruns the limit, and the message must not blame a quantifier that is absent.
        AnyPattern generator = Any.StringMatching(@"(a{1000}){1000}");

        AnyGenerationException caught = Assert.Throws<AnyGenerationException>(() => generator.Generate());
        Check.That(caught.Message).Contains("generation limit");
        Check.That(caught.Message).Contains("bounded quantifiers");             // the real cause is offered
        Check.That(caught.Message).Not.Contains("is expanding without bound");  // the old, false assertion is gone
    }

    [Fact(DisplayName = "A negated class that excludes the whole printable universe is refused as unsupported, not malformed.")]
    public void NegatedClassExcludingTheUniverseIsUnsupported() {
        // Well-formed and regular — the real engine accepts both — but no printable-ASCII character survives the
        // negation, so Dummies cannot draw a value. That is a universe limit (unsupported), not a caller mistake
        // (malformed): the pattern is not broken, Dummies simply does not reach outside printable ASCII.
        Check.ThatCode(() => Any.StringMatching(@"[^\x20-\x7E]")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"[^\s\S]")).Throws<UnsupportedRegexException>();

        UnsupportedRegexException caught = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"[^\x20-\x7E]"));
        Check.That(caught.Message).Contains("printable ASCII");
    }

    [Theory(DisplayName = "A pattern the real .NET engine accepts is never rejected as malformed — it generates, or is refused as unsupported.")]
    [InlineData(@"^^abc")]
    [InlineData(@"abc$$")]
    [InlineData(@"^*abc")]
    [InlineData(@"abc$*")]
    [InlineData(@"^{2}xy")]
    [InlineData(@"^?a$?")]
    [InlineData(@"[-[x]]")]
    [InlineData(@"[a-[x]]")]         // subtraction (member precedes): .NET-valid, refused as UNSUPPORTED, not malformed
    [InlineData(@"[ab-[b]]")]
    [InlineData(@"[^\x20-\x7E]")]    // negated-empty: .NET-valid, refused as UNSUPPORTED
    [InlineData(@"[^\s\S]")]
    [InlineData(@"(a{1000}){1000}")] // over-limit: .NET-valid, fails at generation time, never as malformed
    public void PatternsAcceptedByTheRealEngineAreNeverMalformed(string pattern) {
        // The advertised taxonomy (see RegexParser's summary): ArgumentException == "the real engine rejects this
        // pattern as malformed". So a pattern the real engine COMPILES must never surface here as ArgumentException —
        // Dummies must either generate a matching value, or refuse it as UnsupportedRegexException, or fail the
        // generation itself. This guards the whole channel, not just the individual edges #210 corrected.
        Assert.True(IsCompiledByTheRealEngine(pattern), $"test precondition: /{pattern}/ must be accepted by .NET");

        try {
            Any.WithSeed(1).StringMatching(pattern).Generate();
        } catch (ArgumentException e) {
            Assert.Fail($"/{pattern}/ is accepted by the real engine but Dummies rejected it as malformed: {e.Message}");
        } catch (UnsupportedRegexException) {
            // acceptable: refused as unsupported (a construct or universe Dummies declines), not as malformed
        } catch (AnyGenerationException) {
            // acceptable: a resource-limit overrun, not a verdict that the pattern is malformed
        }
    }

    [Fact(DisplayName = "A Regex with IgnoreCase generates either case.")]
    public void IgnoreCaseHonoured() {
        Regex           pattern = new("^[a-z]{5}$", RegexOptions.IgnoreCase);
        bool            sawUpper = false;
        AnyContext      context  = Any.WithSeed(99);
        AnyPattern      generator = context.StringMatching(pattern);

        for (int i = 0; i < SampleCount; i++) {
            string value = generator.Generate();
            AssertMatches(value, "[a-z]{5}", RegexOptions.IgnoreCase);
            if (value.Any(char.IsUpper)) { sawUpper = true; }
        }

        Check.That(sawUpper).IsTrue();
    }

    [Fact(DisplayName = "A matching generator composes into a value object through As.")]
    public void ComposesThroughAs() {
        IAny<OrderReference> generator = Any.StringMatching(@"^ORD-\d{8}$").As(OrderReference.Create);

        for (int i = 0; i < 50; i++) {
            OrderReference reference = generator.Generate();
            Check.That(reference.Value).StartsWith("ORD-");
            Check.That(reference.Value.Length).IsEqualTo(12);
        }
    }

    [Fact(DisplayName = "Matching is reproducible under a seed.")]
    public void ReproducibleUnderASeed() {
        string first  = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).StringMatching(@"[A-Z]{3}-\d{4}").Generate()));
        string second = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).StringMatching(@"[A-Z]{3}-\d{4}").Generate()));

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Non-regular constructs are refused eagerly with UnsupportedRegexException.")]
    public void UnsupportedConstructsAreRefused() {
        Check.ThatCode(() => Any.StringMatching(@"foo(?=bar)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"foo(?!bar)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?<=x)y")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"\bword\b")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(\w+)\s\1")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"\p{L}+")).Throws<UnsupportedRegexException>();

        UnsupportedRegexException caught = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"a(?=b)"));
        Check.That(caught.Message).Contains("lookahead");
    }

    [Fact(DisplayName = "Constructs whose language a plain walk cannot honour are refused, never mis-generated.")]
    public void NotGeneratableConstructsAreRefused() {
        // An atomic group commits to its first matching branch: (?>ab|a)b matches only "abb", so lowering it to
        // a plain alternation could emit "ab" — refused instead.
        Check.ThatCode(() => Any.StringMatching(@"(?>ab|a)b")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?>a)")).Throws<UnsupportedRegexException>();

        // A misplaced anchor makes the pattern unmatchable by any whole string.
        Check.ThatCode(() => Any.StringMatching(@"a^")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"$a")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"x(^a)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(a$)x")).Throws<UnsupportedRegexException>();

        // .NET class subtraction removes a nested class; parsing '-[' as members would generate outside the set. It
        // is subtraction only when a base member precedes the '-[' — after a range ('[a-z-[aeiou]]') or a single
        // member ('[a-[x]]', '[ab-[b]]'). A leading '-[' is an ordinary hyphen and IS accepted (see the oracle theory).
        Check.ThatCode(() => Any.StringMatching(@"[a-z-[aeiou]]")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"[a-[x]]")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"[ab-[b]]")).Throws<UnsupportedRegexException>();

        // IgnorePatternWhitespace changes how the pattern text itself is read: "^A B$" matches "AB", not "A B".
        Check.ThatCode(() => Any.StringMatching(new Regex("^A B$", RegexOptions.IgnorePatternWhitespace))).Throws<ArgumentException>();
        Check.ThatCode(() => Any.WithSeed(1).StringMatching(new Regex("^A B$", RegexOptions.IgnorePatternWhitespace))).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A balancing group is refused as unsupported — both syntaxes, target defined or not.")]
    public void BalancingGroupsAreRefused() {
        // A balancing group '(?<-name>…)' / '(?<name1-name2>…)' pops the capture stack — the backreference family,
        // which is non-regular. Its language is not that of a plain named group: '(?<a>y)?(?<-a>x)' matches only
        // "yx" (the '-a' pop forces the optional 'a' group to have fired), yet lowering '(?<-a>x)' to an ordinary
        // named group would emit "x". It is refused instead of mis-generated. .NET accepts these two target-defined
        // patterns, so the refusal is a genuine "we decline what a plain walk cannot honour", not an echo of .NET.
        Check.ThatCode(() => Any.StringMatching(@"(?<a>y)?(?<-a>x)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?<a>y)?(?'-a'x)")).Throws<UnsupportedRegexException>(); // quote form

        // The '-' is refused even when the target group is undefined — where the real engine instead reports a
        // malformed pattern. Distinguishing the two would need a table of captured groups the generator does not
        // keep; the divergence is only in the error kind (both reject, neither mis-generates) and is accepted.
        Check.ThatCode(() => Any.StringMatching(@"(?<-a>x)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?'-a'x)")).Throws<UnsupportedRegexException>();          // quote form
        Check.ThatCode(() => Any.StringMatching(@"(?<x-y>z)")).Throws<UnsupportedRegexException>();         // name1-name2 form

        UnsupportedRegexException caught = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"(?<a>y)?(?<-a>x)"));
        Check.That(caught.Message).Contains("balancing group");
    }

    [Fact(DisplayName = "Malformed patterns raise ArgumentException; a null pattern raises ArgumentNullException.")]
    public void MalformedPatternsAreRejected() {
        Check.ThatCode(() => Any.StringMatching(@"[a-")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"(abc")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"a{3,1}")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"*abc")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"a\")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching((string)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.StringMatching((Regex)null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Patterns the real engine rejects are rejected here too, never silently re-interpreted.")]
    public void RealEngineRejectionsAreMirrored() {
        // Each of these is refused by System.Text.RegularExpressions; accepting them would make the generator
        // produce values for patterns no production code could ever carry.
        Check.ThatCode(() => Any.StringMatching(@"a*+")).Throws<ArgumentException>();      // possessive: not a .NET construct
        Check.ThatCode(() => Any.StringMatching(@"a**")).Throws<ArgumentException>();      // nested quantifier
        Check.ThatCode(() => Any.StringMatching(@"a*??")).Throws<ArgumentException>();     // nested quantifier
        Check.ThatCode(() => Any.StringMatching(@"[]")).Throws<ArgumentException>();       // unterminated class
        Check.ThatCode(() => Any.StringMatching(@"\q")).Throws<ArgumentException>();       // unrecognized escape
        Check.ThatCode(() => Any.StringMatching(@"\x4")).Throws<ArgumentException>();      // \x expects 2 hex digits
        Check.ThatCode(() => Any.StringMatching(@"\c1")).Throws<ArgumentException>();      // \c expects a letter
        Check.ThatCode(() => Any.StringMatching(@"{2}")).Throws<ArgumentException>();      // quantifier following nothing
        Check.ThatCode(() => Any.StringMatching(@"(?<>a)")).Throws<ArgumentException>();   // empty group name
    }

    [Fact(DisplayName = "An invalid group name is rejected as malformed, matching the real engine — both syntaxes.")]
    public void InvalidGroupNamesAreRejected() {
        // A name opening with a digit is an explicit capture NUMBER, which the real engine accepts only as a positive
        // integer with no leading zero. '0' (reserved for the whole match), a leading zero, and a digit-then-letter
        // name are all rejected — here as they are there.
        Check.ThatCode(() => Any.StringMatching(@"(?<1a>x)")).Throws<ArgumentException>();  // digit then letter
        Check.ThatCode(() => Any.StringMatching(@"(?<0>x)")).Throws<ArgumentException>();   // group 0 is reserved
        Check.ThatCode(() => Any.StringMatching(@"(?<01>x)")).Throws<ArgumentException>();  // leading zero
        Check.ThatCode(() => Any.StringMatching(@"(?'0'x)")).Throws<ArgumentException>();   // quote form, reserved

        // A non-numeric name must be word characters (letter, digit or underscore); a space or a dot is malformed.
        Check.ThatCode(() => Any.StringMatching(@"(?<a b>x)")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"(?'a b'x)")).Throws<ArgumentException>(); // quote form
        Check.ThatCode(() => Any.StringMatching(@"(?<a.b>x)")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Escape sequences generate the real characters, not their letter.")]
    public void EscapesGenerateTheRealCharacters() {
        Check.That(Any.StringMatching(@"\a").Generate()).IsEqualTo("\a");
        Check.That(Any.StringMatching(@"\e").Generate()).IsEqualTo("\u001B");
        Check.That(Any.StringMatching(@"\x41").Generate()).IsEqualTo("A");
        Check.That(Any.StringMatching(@"\u0042").Generate()).IsEqualTo("B");
        Check.That(Any.StringMatching(@"\cA").Generate()).IsEqualTo("\u0001");
        Check.That(Any.StringMatching(@"\07").Generate()).IsEqualTo("\a");
        Check.That(Any.StringMatching(@"\0").Generate()).IsEqualTo("\0");
    }

    [Fact(DisplayName = "A brace that is not a well-formed quantifier is a literal, exactly as in the real engine.")]
    public void BraceLiteralsGenerate() {
        Check.That(Any.StringMatching(@"a{x}").Generate()).IsEqualTo("a{x}");
        Check.That(Any.StringMatching(@"{abc}").Generate()).IsEqualTo("{abc}");
        Check.That(Any.StringMatching(@"a{2,").Generate()).IsEqualTo("a{2,");
    }

    [Fact(DisplayName = "Nesting groups beyond the parser's depth ceiling fails cleanly instead of overflowing the stack.")]
    public void DeepNestingFailsCleanly() {
        string deep = new string('(', 300) + "a" + new string(')', 300);

        ArgumentException caught = Assert.Throws<ArgumentException>(() => Any.StringMatching(deep));
        Check.That(caught.Message).Contains("nested");
    }

    [Theory(DisplayName = "A class range ending at U+FFFF terminates promptly and yields a member, instead of hanging.")]
    [InlineData(@"[\u0020-\uFFFF]")]  // \uFFFF escape: drives the range's upper bound to the top of the char space...
    [InlineData("[ -\uFFFF]")]        // ...and a literal U+FFFF member does the same; both once wrapped the 16-bit loop.
    public async Task ClassRangeEndingAtMaxCharTerminates(string pattern) {
        // Generate off-thread and race a deadline: a loop that wraps a 16-bit char past U+FFFF loses the race and
        // fails the test instead of hanging the whole suite (mirrors the AnyGuid carry-wraparound guard).
        Task<string> run   = Task.Run(() => Any.StringMatching(pattern).Generate());
        Task         first = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Check.That(first == run).IsTrue();

        // The generated value is a genuine member of the class — the real .NET engine is the oracle.
        AssertMatches(await run, pattern);
    }

}
