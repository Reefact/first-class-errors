namespace Dummies;

/// <summary>
///     The bounded, readable character universe the regex generator draws from. Every terminal — a literal, a class,
///     a shorthand (<c>\d \w \s</c> and their negations), the dot — resolves to a set of <b>printable ASCII</b>
///     characters (0x20–0x7E). Restricting the universe keeps generated dummies legible instead of scattering
///     arbitrary Unicode, and it keeps every generated character a genuine member of the class it stands for, so the
///     output always matches the source pattern.
/// </summary>
internal static class RegexAlphabet {

    #region Statics members declarations

    internal const char MinPrintable = ' ';  // 0x20
    internal const char MaxPrintable = '~';  // 0x7E

    /// <summary>Every printable ASCII character — the universe negated classes and the dot draw from.</summary>
    internal static readonly char[] Printable = Range(MinPrintable, MaxPrintable);

    /// <summary><c>\d</c>.</summary>
    internal static readonly char[] Digit = Range('0', '9');

    /// <summary><c>\D</c> — printable non-digits.</summary>
    internal static readonly char[] NonDigit = Where(character => !IsDigit(character));

    /// <summary><c>\w</c>.</summary>
    internal static readonly char[] Word = Where(IsWord);

    /// <summary><c>\W</c> — printable non-word characters.</summary>
    internal static readonly char[] NonWord = Where(character => !IsWord(character));

    /// <summary><c>\s</c> — a readable pair; both are genuine whitespace, so either matches the source pattern.</summary>
    internal static readonly char[] Whitespace = { ' ', '\t' };

    /// <summary><c>\S</c> — printable non-whitespace (space is the only printable whitespace, so this is 0x21–0x7E).</summary>
    internal static readonly char[] NonWhitespace = Where(character => character != ' ');

    /// <summary><c>.</c> — any character except a newline; every printable ASCII character qualifies.</summary>
    internal static readonly char[] Dot = Printable;

    private static bool IsDigit(char character) {
        return character is >= '0' and <= '9';
    }

    private static bool IsWord(char character) {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_';
    }

    /// <summary>The printable characters none of <paramref name="excluded" /> covers — the universe of a negated class <c>[^…]</c>.</summary>
    internal static char[] Negate(ISet<char> excluded) {
        return Where(character => !excluded.Contains(character));
    }

    /// <summary>
    ///     <paramref name="character" /> together with its opposite-case twin when it is an ASCII letter — the
    ///     expansion applied under <see cref="System.Text.RegularExpressions.RegexOptions.IgnoreCase" /> so a literal
    ///     or class member matches either case.
    /// </summary>
    internal static IEnumerable<char> WithBothCases(char character) {
        if (character is >= 'A' and <= 'Z') { return new[] { character, (char)(character + 32) }; }
        if (character is >= 'a' and <= 'z') { return new[] { character, (char)(character - 32) }; }

        return new[] { character };
    }

    private static char[] Range(char low, char high) {
        List<char> characters = new(high - low + 1);
        // Iterate an int, not a char: a high of U+FFFF would wrap a 16-bit char back to 0x0000 and loop forever.
        // Every current caller passes a bounded high, so this is defense in depth against a future wide range.
        for (int code = low; code <= high; code++) { characters.Add((char)code); }

        return characters.ToArray();
    }

    private static char[] Where(Func<char, bool> keep) {
        List<char> characters = new(Printable.Length);
        foreach (char character in Printable) {
            if (keep(character)) { characters.Add(character); }
        }

        return characters.ToArray();
    }

    #endregion

}
