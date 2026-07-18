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
    [InlineData(@"^a$|^b$")]
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
            string reference = Any.StringMatching(@"^ORD-\d{8}$");
            Check.That(reference.Length).IsEqualTo(12);
            Check.That(reference).StartsWith("ORD-");
            Check.That(reference.Substring(4)).Matches("^[0-9]{8}$");
        }
    }

    [Fact(DisplayName = "Alternation draws each branch and only declared branches.")]
    public void Alternation() {
        HashSet<string> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            string value = Any.StringMatching("(EUR|USD|GBP)");
            Check.That(value == "EUR" || value == "USD" || value == "GBP").IsTrue();
            seen.Add(value);
        }

        Check.That(seen).Contains("EUR", "USD", "GBP");
    }

    [Fact(DisplayName = "Character classes, ranges and negation stay within their set.")]
    public void CharacterClasses() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That((string)Any.StringMatching("[A-Z]{3}")).Matches("^[A-Z]{3}$");
            Check.That((string)Any.StringMatching("[^0-9]{4}")).Matches("^[^0-9]{4}$");
            Check.That((string)Any.StringMatching(@"[\d]{5}")).Matches("^[0-9]{5}$");
        }
    }

    [Fact(DisplayName = "Bounded quantifiers stay within their bounds; unbounded ones draw the minimum plus 0 to 8.")]
    public void QuantifierBounds() {
        HashSet<int> starLengths = new();
        HashSet<int> plusLengths = new();
        HashSet<int> openLengths = new();

        for (int i = 0; i < SampleCount; i++) {
            int bounded = ((string)Any.StringMatching("a{2,4}")).Length;
            Check.That(bounded is >= 2 and <= 4).IsTrue();

            starLengths.Add(((string)Any.StringMatching("a*")).Length);
            plusLengths.Add(((string)Any.StringMatching("a+")).Length);
            openLengths.Add(((string)Any.StringMatching("a{2,}")).Length);
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
            Check.That((string)Any.StringMatching("^abc$")).IsEqualTo("abc");
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

        // .NET class subtraction removes a nested class; parsing '-[' as members would generate outside the set.
        Check.ThatCode(() => Any.StringMatching(@"[a-z-[aeiou]]")).Throws<UnsupportedRegexException>();

        // IgnorePatternWhitespace changes how the pattern text itself is read: "^A B$" matches "AB", not "A B".
        Check.ThatCode(() => Any.StringMatching(new Regex("^A B$", RegexOptions.IgnorePatternWhitespace))).Throws<ArgumentException>();
        Check.ThatCode(() => Any.WithSeed(1).StringMatching(new Regex("^A B$", RegexOptions.IgnorePatternWhitespace))).Throws<ArgumentException>();
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

    [Fact(DisplayName = "Escape sequences generate the real characters, not their letter.")]
    public void EscapesGenerateTheRealCharacters() {
        Check.That((string)Any.StringMatching(@"\a")).IsEqualTo("\a");
        Check.That((string)Any.StringMatching(@"\e")).IsEqualTo("\u001B");
        Check.That((string)Any.StringMatching(@"\x41")).IsEqualTo("A");
        Check.That((string)Any.StringMatching(@"\u0042")).IsEqualTo("B");
        Check.That((string)Any.StringMatching(@"\cA")).IsEqualTo("\u0001");
        Check.That((string)Any.StringMatching(@"\07")).IsEqualTo("\a");
        Check.That((string)Any.StringMatching(@"\0")).IsEqualTo("\0");
    }

    [Fact(DisplayName = "A brace that is not a well-formed quantifier is a literal, exactly as in the real engine.")]
    public void BraceLiteralsGenerate() {
        Check.That((string)Any.StringMatching(@"a{x}")).IsEqualTo("a{x}");
        Check.That((string)Any.StringMatching(@"{abc}")).IsEqualTo("{abc}");
        Check.That((string)Any.StringMatching(@"a{2,")).IsEqualTo("a{2,");
    }

    [Fact(DisplayName = "Nesting groups beyond the parser's depth ceiling fails cleanly instead of overflowing the stack.")]
    public void DeepNestingFailsCleanly() {
        string deep = new string('(', 300) + "a" + new string(')', 300);

        ArgumentException caught = Assert.Throws<ArgumentException>(() => Any.StringMatching(deep));
        Check.That(caught.Message).Contains("nested");
    }

}
