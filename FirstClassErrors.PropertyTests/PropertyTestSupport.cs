#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

#endregion

namespace FirstClassErrors.PropertyTests;

/// <summary>
///     Custom FsCheck generators shared by the property-based tests. They deliberately feed awkward inputs
///     (empty, whitespace-only, and whitespace-padded strings, alongside the full range of characters produced
///     by FsCheck's default string generator) into the library's validating factories, in the spirit of fuzzing.
/// </summary>
internal static class Generators {

    #region Statics members declarations

    /// <summary>
    ///     Generates a non-blank string: never <c>null</c>, always containing at least one non-whitespace
    ///     character, and frequently carrying leading or trailing whitespace so that trimming and normalization
    ///     paths are exercised.
    /// </summary>
    public static Gen<string> NonBlank() {
        return ArbMap.Default.GeneratorFor<string>().Select(candidate => string.IsNullOrWhiteSpace(candidate) ? "x" + candidate : candidate);
    }

    /// <summary>
    ///     Generates a blank string: either empty or composed solely of whitespace characters. Never <c>null</c>.
    /// </summary>
    public static Gen<string> Blank() {
        return Gen.OneOf(Gen.Constant(string.Empty),
                         Gen.Choose(1, 8).Select(length => new string(' ', length)),
                         Gen.Constant("\t"),
                         Gen.Constant("\n"),
                         Gen.Constant("\r\n"),
                         Gen.Constant("  \t \r\n "));
    }

    #endregion

}

/// <summary>
///     Small assertion helpers usable from inside an FsCheck property, where a boolean result (rather than a
///     thrown assertion) signals whether the property held for the generated input.
/// </summary>
internal static class Expect {

    #region Statics members declarations

    /// <summary>
    ///     Returns <c>true</c> when invoking <paramref name="action" /> throws an exception assignable to
    ///     <typeparamref name="TException" />; otherwise <c>false</c>.
    /// </summary>
    public static bool Throws<TException>(Action action)
        where TException : Exception {
        try {
            action();

            return false;
        } catch (TException) {
            return true;
        }
    }

    #endregion

}
