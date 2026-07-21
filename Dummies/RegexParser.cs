#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A recursive-descent parser turning the <b>regular</b> subset of a regex pattern into a <see cref="RegexNode" />
///     tree the generator can walk. Supported: literals; the escapes <c>\t \n \r \f \v \a \e</c>, hexadecimal
///     <c>\xHH</c> and <c>\uHHHH</c>, control <c>\cX</c>, octal <c>\0nn</c> and escaped punctuation; the shorthands
///     <c>\d \D \w \W \s \S</c>; character classes with ranges and negation; the quantifiers
///     <c>? * + {n} {n,} {n,m}</c> (a lazy <c>?</c> marker is accepted and ignored — it changes matching order, never
///     which strings match; possessive quantifiers do not exist in .NET and are rejected); alternation; grouping
///     (capturing, non-capturing and named — the name is validated as the real engine would, then ignored); the dot;
///     and the anchors <c>^ $</c> at the start and end of the pattern or of a top-level alternation branch — including
///     a run of them (<c>^^</c>, <c>$$</c>) or a quantified one (<c>^*</c>, <c>$?</c>), all no-ops there since a whole
///     matching string is generated; anywhere else they are refused, because the pattern could never be matched by a
///     whole generated string. A brace that does not form a well-formed quantifier is a literal, as in the real engine,
///     and groups may nest at most 256 levels deep. A well-formed but non-regular or out-of-scope construct — a
///     lookaround, a backreference, a balancing group (it pops the capture stack, the backreference family), a Unicode
///     category, a word boundary, an atomic group (its first-branch commit is not language-equivalent to plain
///     alternation), a class subtraction, or a negated class that excludes the whole printable-ASCII universe Dummies
///     draws from — is refused with an
///     <see cref="UnsupportedRegexException" /> rather than silently mis-generated; a malformed pattern (including an
///     escape the real engine rejects) raises an <see cref="ArgumentException" />.
/// </summary>
internal sealed class RegexParser {

    #region Statics members declarations

    private const int MaxGroupDepth = 256;

    internal static RegexNode Parse(string pattern, bool ignoreCase) {
        RegexParser parser = new(pattern, ignoreCase);
        RegexNode   root   = parser.ParseAlternation();
        if (!parser.AtEnd) {
            // The only character ParseSequence stops on without consuming is a ')' with no opener.
            throw parser.Malformed(parser.Peek() == ')' ? "unbalanced closing parenthesis ')'" : $"unexpected character '{parser.Peek()}'");
        }

        return root;
    }

    private static bool IsClassShorthand(char character) {
        return character is 'd' or 'D' or 'w' or 'W' or 's' or 'S';
    }

    private static void AddClassShorthand(HashSet<char> set, char shorthand) {
        char[] members = shorthand switch {
            'd' => RegexAlphabet.Digit,
            'D' => RegexAlphabet.NonDigit,
            'w' => RegexAlphabet.Word,
            'W' => RegexAlphabet.NonWord,
            's' => RegexAlphabet.Whitespace,
            _   => RegexAlphabet.NonWhitespace
        };
        foreach (char member in members) { set.Add(member); }
    }

    private static HashSet<char> ExpandCase(HashSet<char> set) {
        HashSet<char> expanded = new();
        foreach (char character in set) {
            foreach (char variant in RegexAlphabet.WithBothCases(character)) { expanded.Add(variant); }
        }

        return expanded;
    }

    private static bool IsHexDigit(char character) {
        return character is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
    }

    private static int HexValue(char character) {
        return character <= '9' ? character - '0' : char.ToUpperInvariant(character) - 'A' + 10;
    }

    #endregion

    #region Fields declarations

    private readonly bool   _ignoreCase;
    private readonly string _pattern;
    private          int    _depth;
    private          int    _index;

    #endregion

    private RegexParser(string pattern, bool ignoreCase) {
        _pattern    = pattern;
        _ignoreCase = ignoreCase;
    }

    private bool AtEnd => _index >= _pattern.Length;

    private char Peek() {
        return _pattern[_index];
    }

    private char Next() {
        return _pattern[_index++];
    }

    private char PeekAt(int offset) {
        int at = _index + offset;

        return at < _pattern.Length ? _pattern[at] : '\0';
    }

    private bool Eat(char character) {
        if (!AtEnd && _pattern[_index] == character) {
            _index++;

            return true;
        }

        return false;
    }

    private RegexNode ParseAlternation() {
        List<RegexNode> branches = new() { ParseSequence() };
        while (Eat('|')) { branches.Add(ParseSequence()); }

        return branches.Count == 1 ? branches[0] : new RegexAlternation(branches.ToArray());
    }

    private RegexNode ParseSequence() {
        List<RegexNode> parts = new();
        while (!AtEnd && Peek() != '|' && Peek() != ')') {
            // Anchors are no-ops for a whole-string generator, but only where they are guaranteed to match:
            // '^' at the start and '$' at the end of the pattern or of a top-level alternation branch. A run of
            // them ('^^', '$$') and a quantified one ('^*', '$?', '^{2}') are equally no-ops there — the real
            // engine accepts and matches all of these — so they are consumed and ignored. Anywhere else ('a^',
            // '$a', inside a group) the pattern can never be matched by a whole generated string, so it is
            // refused instead of silently mis-generated.
            if (Peek() == '^') {
                if (_depth > 0 || parts.Count > 0) { throw Unsupported("an anchor '^' away from the start of the pattern or of a top-level alternation branch", _index); }
                _index++;
                SkipAnchorQuantifier();

                continue;
            }

            if (Peek() == '$') {
                int position = _index;
                _index++;
                SkipAnchorQuantifier();
                // A '$' is a no-op only at the very end: what follows must be end-of-pattern, a branch bar, or
                // another end-anchor '$'. Inside a group, or before anything else, it can never match a whole string.
                if (_depth > 0 || (!AtEnd && Peek() != '|' && Peek() != '$')) { throw Unsupported("an anchor '$' away from the end of the pattern or of a top-level alternation branch", position); }

                continue;
            }

            parts.Add(ParseQuantified());
        }

        return parts.Count == 1 ? parts[0] : new RegexSequence(parts.ToArray());
    }

    /// <summary>
    ///     Consumes a quantifier applied to a boundary anchor (<c>^*</c>, <c>$?</c>, <c>^{2,3}</c>, a lazy marker
    ///     included) when one is present. Repeating a zero-width no-op leaves it a no-op — the real engine accepts and
    ///     matches these — so the quantifier is read and discarded, never turned into a repeat node.
    /// </summary>
    private void SkipAnchorQuantifier() {
        if (AtEnd) { return; }                     // an anchor at the very end carries no quantifier
        if (TryReadQuantifier() is null) { return; }
        if (!AtEnd && Peek() == '?') { _index++; } // a lazy marker on a no-op is immaterial
    }

    private RegexNode ParseQuantified() {
        RegexNode atom = ParseAtom();
        if (AtEnd) { return atom; }

        (int Min, int? Max)? quantifier = TryReadQuantifier();
        if (quantifier is null) { return atom; }

        // A trailing '?' makes the quantifier lazy — it changes which match is preferred, never which strings
        // match, so it is accepted and ignored. Possessive quantifiers (a*+) do not exist in .NET; the '+' falls
        // through to the nothing-to-repeat error below, mirroring the real engine's rejection.
        if (!AtEnd && Peek() == '?') { _index++; }

        return new RegexRepeat(atom, quantifier.Value.Min, quantifier.Value.Max);
    }

    private (int Min, int? Max)? TryReadQuantifier() {
        switch (Peek()) {
            case '*': _index++; return (0, null);
            case '+': _index++; return (1, null);
            case '?': _index++; return (0, 1);
            case '{': return ReadBraceQuantifier();
            default:  return null;
        }
    }

    /// <summary>
    ///     Reads a <c>{n}</c>, <c>{n,}</c> or <c>{n,m}</c> quantifier. A brace whose content is not one of those
    ///     forms is a <b>literal</b> in the real engine, so the position is restored and <c>null</c> returned — the
    ///     caller then reads the '{' as an ordinary character. An out-of-order <c>{3,1}</c> is rejected, as the real
    ///     engine rejects it.
    /// </summary>
    private (int Min, int? Max)? ReadBraceQuantifier() {
        int start = _index;
        _index++; // consume '{'
        if (AtEnd || !char.IsDigit(Peek())) {
            _index = start;

            return null;
        }

        int  min = ReadInteger();
        int? max;
        if (Eat('}')) {
            max = min;
        } else if (Eat(',')) {
            if (Eat('}')) {
                max = null;
            } else if (!AtEnd && char.IsDigit(Peek())) {
                max = ReadInteger();
                if (!Eat('}')) {
                    _index = start;

                    return null;
                }
            } else {
                _index = start;

                return null;
            }
        } else {
            _index = start;

            return null;
        }

        if (max is int upper && upper < min) { throw Malformed($"quantifier {{{min},{upper}}} is out of order (the maximum is below the minimum)"); }

        return (min, max);
    }

    private int ReadInteger() {
        int start = _index;
        while (!AtEnd && char.IsDigit(Peek())) { _index++; }
        string digits = _pattern.Substring(start, _index - start);
        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int value)) {
            throw Malformed($"quantifier bound '{digits}' is too large");
        }

        return value;
    }

    private RegexNode ParseAtom() {
        char character = Peek();
        switch (character) {
            case '(': return ParseGroup();
            case '[': return ParseClass();
            case '\\': return ParseEscape();
            case '.': _index++; return new RegexCharacters(RegexAlphabet.Dot);
            case '*':
            case '+':
            case '?': throw Malformed($"quantifier '{character}' has nothing to repeat");
            case '{': {
                // A well-formed brace quantifier with no atom before it is an error, exactly as in the real
                // engine; any other brace is a literal.
                if (ReadBraceQuantifier() is not null) { throw Malformed("quantifier '{' has nothing to repeat"); }
                _index++;

                return Literal('{');
            }
            default: _index++; return Literal(character);
        }
    }

    private RegexNode ParseGroup() {
        int position = _index;
        _index++; // consume '('
        if (!AtEnd && Peek() == '?') {
            _index++; // consume '?'
            if (AtEnd) { throw Malformed("unterminated group '(?'"); }

            switch (Peek()) {
                case ':': _index++; break; // non-capturing group
                // An atomic group commits to the first branch that matches, so its language is NOT that of the
                // plain alternation ('(?>ab|a)b' matches only "abb"); generating from it as if it were would
                // yield non-matching values, so it is refused.
                case '>': throw Unsupported("an atomic group '(?>…)'", position);
                case '=': throw Unsupported("a lookahead '(?=…)'", position);
                case '!': throw Unsupported("a negative lookahead '(?!…)'", position);
                case '(': throw Unsupported("a conditional group '(?(…)…)'", position);
                case '#': throw Unsupported("an inline comment '(?#…)'", position);
                case '<':
                    if (PeekAt(1) is '=' or '!') { throw Unsupported("a lookbehind '(?<=…)' or '(?<!…)'", position); }
                    _index++;                     // consume '<'
                    SkipGroupName('>', position);  // named group: the name never shapes generation, but it is validated
                    break;
                case '\'':
                    _index++; // consume '\''
                    SkipGroupName('\'', position);
                    break;
                default: throw Unsupported($"a group option '(?{Peek()}…)'", position);
            }
        }

        _depth++;
        if (_depth > MaxGroupDepth) { throw Malformed($"groups are nested deeper than {MaxGroupDepth} levels"); }
        RegexNode inner = ParseAlternation();
        _depth--;
        if (!Eat(')')) { throw Malformed("unbalanced opening parenthesis '('"); }

        return inner;
    }

    /// <summary>
    ///     Consumes a named-group name up to its <paramref name="terminator" /> (<c>&gt;</c> for
    ///     <c>(?&lt;name&gt;…)</c>, a quote for <c>(?'name'…)</c>) and validates it as the real engine would, so
    ///     "accepted by Dummies" stays aligned with "accepted by .NET". The name never shapes generation, but two
    ///     well-formed .NET constructs must not slip through as ordinary named groups:
    ///     <list type="bullet">
    ///         <item>
    ///             a <b>balancing group</b> <c>(?&lt;name1-name2&gt;…)</c> / <c>(?&lt;-name2&gt;…)</c> — the
    ///             <c>-</c> pops the capture stack (the same family as a backreference), so it is non-regular and
    ///             refused with an <see cref="UnsupportedRegexException" />. This fires <b>even when the target group
    ///             is undefined</b> — where the real engine instead reports a malformed pattern — because telling the
    ///             two apart would need a table of captured groups, which this generator deliberately does not keep.
    ///             The divergence is only in the error <i>kind</i>: both reject the pattern, neither mis-generates.
    ///         </item>
    ///         <item>
    ///             an <b>invalid name</b> — a name opening with a digit is an explicit capture <i>number</i>, valid
    ///             only as a positive integer with no leading zero (<c>1</c>, <c>10</c>; not <c>0</c>, <c>01</c> or
    ///             <c>1a</c>); any other name must be word characters. Anything else raises an
    ///             <see cref="ArgumentException" />, mirroring the real engine.
    ///         </item>
    ///     </list>
    /// </summary>
    private void SkipGroupName(char terminator, int position) {
        int start = _index;
        while (!AtEnd && Peek() != terminator) { _index++; }
        if (_index == start) { throw Malformed("a group name must not be empty"); }
        string name = _pattern.Substring(start, _index - start);
        if (!Eat(terminator)) { throw Malformed($"unterminated group name (expected '{terminator}')"); }
        ValidateGroupName(name, position);
    }

    private void ValidateGroupName(string name, int position) {
        // A '-' marks a balancing group; it manipulates the capture stack (the backreference family), so it is
        // non-regular and refused here even when its target is undefined (see SkipGroupName for why the divergence
        // from the real engine's malformed-pattern verdict on that case is accepted).
        if (name.IndexOf('-') >= 0) { throw Unsupported("a balancing group '(?<name1-name2>…)'", position); }

        // A name opening with a digit is an explicit capture number: the real engine accepts it only as a positive
        // integer with no leading zero, so '0', '01' and '1a' are refused while '1' and '10' pass.
        if (name[0] is >= '0' and <= '9') {
            bool validNumber = name[0] != '0';
            foreach (char character in name) {
                if (character is < '0' or > '9') { validNumber = false; }
            }

            if (!validNumber) { throw Malformed($"a group name starting with a digit must be a group number with no leading zero, but '{name}' is not"); }

            return;
        }

        // Any other name must be word characters (letter, digit or underscore), exactly as the real engine requires.
        foreach (char character in name) {
            if (!char.IsLetterOrDigit(character) && character != '_') { throw Malformed($"the group name '{name}' contains '{character}', which is not a letter, digit or underscore"); }
        }
    }

    private RegexNode ParseEscape() {
        int position = _index;
        _index++; // consume '\'
        if (AtEnd) { throw Malformed("a trailing '\\' escapes nothing"); }

        char escaped = Next();
        switch (escaped) {
            case 'd': return new RegexCharacters(RegexAlphabet.Digit);
            case 'D': return new RegexCharacters(RegexAlphabet.NonDigit);
            case 'w': return new RegexCharacters(RegexAlphabet.Word);
            case 'W': return new RegexCharacters(RegexAlphabet.NonWord);
            case 's': return new RegexCharacters(RegexAlphabet.Whitespace);
            case 'S': return new RegexCharacters(RegexAlphabet.NonWhitespace);
            case 't': return Literal('\t');
            case 'n': return Literal('\n');
            case 'r': return Literal('\r');
            case 'f': return Literal('\f');
            case 'v': return Literal('\v');
            case 'a': return Literal('\a');
            case 'e': return Literal('\u001B');
            case 'x': return Literal(ReadHexEscape(2));
            case 'u': return Literal(ReadHexEscape(4));
            case 'c': return Literal(ReadControlEscape());
            case '0': return Literal(ReadOctalTail(0));
            case 'b': throw Unsupported("a word-boundary '\\b'", position);
            case 'B': throw Unsupported("a non-word-boundary '\\B'", position);
            case 'A': throw Unsupported("a start-of-string anchor '\\A'", position);
            case 'G': throw Unsupported("a contiguous-match anchor '\\G'", position);
            case 'Z':
            case 'z': throw Unsupported("an end-of-string anchor '\\" + escaped + "'", position);
            case 'p':
            case 'P': throw Unsupported("a Unicode category '\\" + escaped + "{…}'", position);
            case 'k': throw Unsupported("a named backreference '\\k<…>'", position);
            default:
                if (escaped is >= '1' and <= '9') { throw Unsupported($"a backreference '\\{escaped}'", position); }
                // The real engine rejects unknown word-character escapes rather than reading them as literals;
                // mirroring it keeps "accepted by Dummies" aligned with "accepted by .NET".
                if (char.IsLetterOrDigit(escaped)) { throw Malformed($"unrecognized escape sequence '\\{escaped}'"); }

                return Literal(escaped); // an escaped metacharacter (\. \* \( \\ …) or escaped punctuation
        }
    }

    private char ReadHexEscape(int digits) {
        int value = 0;
        for (int i = 0; i < digits; i++) {
            if (AtEnd || !IsHexDigit(Peek())) { throw Malformed($"a '\\{(digits == 2 ? 'x' : 'u')}' escape expects exactly {digits} hexadecimal digits"); }
            value = value * 16 + HexValue(Next());
        }

        return (char)value;
    }

    private char ReadControlEscape() {
        if (AtEnd || Peek() is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))) { throw Malformed("a '\\c' escape expects a letter (\\cA through \\cZ)"); }

        return (char)(char.ToUpperInvariant(Next()) - 'A' + 1);
    }

    private char ReadOctalTail(int firstDigit) {
        int value = firstDigit;
        for (int i = 0; i < 2 && !AtEnd && Peek() is >= '0' and <= '7'; i++) { value = value * 8 + (Next() - '0'); }

        return (char)value;
    }

    private RegexNode ParseClass() {
        int position = _index;
        _index++; // consume '['
        bool          negated = Eat('^');
        HashSet<char> set     = new();
        bool          first   = true;

        while (true) {
            if (AtEnd) { throw Malformed("unterminated character class '['"); }
            if (Peek() == ']' && !first) {
                _index++;

                break;
            }

            first = false;
            // .NET's class subtraction ([a-z-[aeiou]]) removes a nested class; parsing the '-[' as members would
            // close the class early and generate values outside it, so the construct is refused. But '-[' is
            // subtraction only when a base member precedes it: the real engine reads a leading '-' (as in '[-[x]]')
            // as an ordinary hyphen, so it is refused here only once the set already holds a member.
            if (Peek() == '-' && PeekAt(1) == '[' && set.Count > 0) { throw Unsupported("a character-class subtraction '[…-[…]]'", _index); }
            if (Peek() == '\\' && IsClassShorthand(PeekAt(1))) {
                _index++; // consume '\'
                AddClassShorthand(set, Next());

                continue;
            }

            char low = ReadClassChar();
            // A '-[' immediately after a member is subtraction as well ('[a-[x]]'): the real engine never reads it
            // as a range whose upper bound is '[', so intercepting it here can never turn away a valid range.
            if (!AtEnd && Peek() == '-' && PeekAt(1) == '[') { throw Unsupported("a character-class subtraction '[…-[…]]'", _index); }
            if (!AtEnd && Peek() == '-' && PeekAt(1) != ']' && PeekAt(1) != '\0') {
                _index++; // consume '-'
                char high = ReadClassChar();
                if (high < low) { throw Malformed($"character class range '{low}-{high}' is out of order"); }
                // Iterate an int, not a char: a class range may legitimately end at U+FFFF (reachable via a
                // literal or the \uFFFF escape), and incrementing a 16-bit char past it wraps to 0x0000 and never ends.
                for (int code = low; code <= high; code++) { set.Add((char)code); }
            } else {
                set.Add(low);
            }
        }

        if (_ignoreCase) { set = ExpandCase(set); }
        char[] choices = negated ? RegexAlphabet.Negate(set) : set.ToArray();
        // Only a negated class can end up empty — a plain class always holds the member it just read. Such a class
        // is well-formed and regular (the real engine accepts it), but it excludes every character Dummies draws
        // from, so it is refused as unsupported (a universe limit), not as malformed. Routing it through Malformed
        // would claim the caller wrote a broken pattern for one the real engine compiles.
        if (choices.Length == 0) {
            throw new UnsupportedRegexException($"The regular expression pattern \"{_pattern}\" uses a negated character class that excludes every character Dummies can generate (printable ASCII U+0020 to U+007E) at position {position}, which Dummies cannot generate from. It draws values from printable ASCII; express the requirement with characters inside that range, or generate the value another way.");
        }

        return new RegexCharacters(choices);
    }

    private char ReadClassChar() {
        if (AtEnd) { throw Malformed("unterminated character class '['"); }

        char character = Next();
        if (character != '\\') { return character; }
        if (AtEnd) { throw Malformed("a trailing '\\' escapes nothing"); }

        char escaped = Next();
        switch (escaped) {
            case 't': return '\t';
            case 'n': return '\n';
            case 'r': return '\r';
            case 'f': return '\f';
            case 'v': return '\v';
            case 'a': return '\a';
            case 'e': return '\u001B';
            case 'b': return '\b'; // inside a class, \b is the backspace character, never a word boundary
            case 'x': return ReadHexEscape(2);
            case 'u': return ReadHexEscape(4);
            case 'c': return ReadControlEscape();
            case '0': return ReadOctalTail(0);
            default:
                if (IsClassShorthand(escaped)) { throw Malformed($"a shorthand '\\{escaped}' cannot be an endpoint of a character range"); }
                // Inside a class a backslash-digit is an octal escape — backreferences cannot occur here.
                if (escaped is >= '1' and <= '7') { return ReadOctalTail(escaped - '0'); }
                if (escaped is 'p' or 'P') { throw Unsupported($"a Unicode category '\\{escaped}{{…}}'", _index - 2); }
                if (char.IsLetterOrDigit(escaped)) { throw Malformed($"unrecognized escape sequence '\\{escaped}' in a character class"); }

                return escaped; // an escaped metacharacter or escaped punctuation
        }
    }

    private RegexNode Literal(char character) {
        if (!_ignoreCase) { return new RegexCharacters(new[] { character }); }

        return new RegexCharacters(RegexAlphabet.WithBothCases(character).Distinct().ToArray());
    }

    private ArgumentException Malformed(string reason) {
        return new ArgumentException($"The regular expression pattern \"{_pattern}\" is invalid: {reason} (at position {_index}).", "pattern");
    }

    private UnsupportedRegexException Unsupported(string construct, int position) {
        return new UnsupportedRegexException($"The regular expression pattern \"{_pattern}\" uses {construct} at position {position}, which Dummies cannot generate from. It builds values from the regular subset of the pattern language; lookarounds, backreferences, word boundaries and Unicode categories are outside it. Express the requirement with the supported subset, or generate the value another way.");
    }

}
