namespace Dummies;

/// <summary>The character families a string or char generator can be restricted to.</summary>
internal enum CharacterSet {

    Alpha,
    Numeric,
    AlphaNumeric

}

/// <summary>The casing a string or char generator can impose on alphabetic characters.</summary>
internal enum LetterCasing {

    Lower,
    Upper

}

/// <summary>
///     The ASCII pools and classification helpers shared by <see cref="AnyString" />'s filler and
///     <see cref="AnyChar" /> — one definition of "letters and digits", so the two generators can never drift
///     apart on what their default characters are.
/// </summary>
internal static class CharacterPools {

    internal const string UpperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    internal const string LowerLetters = "abcdefghijklmnopqrstuvwxyz";
    internal const string Digits       = "0123456789";

    internal static bool IsAsciiLetter(char character) {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    internal static bool IsAsciiDigit(char character) {
        return character is >= '0' and <= '9';
    }

}
