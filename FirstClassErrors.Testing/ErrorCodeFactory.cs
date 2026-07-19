#region Usings declarations

using Dummies;

#endregion

namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies an arbitrary, valid <see cref="ErrorCode" /> for the parts of a test that need <i>some</i> code but
///     never assert on it — an explicit <c>ErrorCodeFactory.Any()</c> call reads as "this code is arbitrary" where a hand-picked
///     literal reads as "this matters". The value is shaped like <c>ANY_CODE_7F3A9C</c> so it is recognizable as
///     arbitrary in a failure message, and it is drawn from Dummies' ambient random context: wrap the test in
///     <c>Dummies.Any.Reproducibly(...)</c> to make the chosen code reproducible and reported on failure.
/// </summary>
public static class ErrorCodeFactory {

    /// <summary>
    ///     Returns an arbitrary, non-blank <see cref="ErrorCode" />, recognizable as arbitrary (for example
    ///     <c>ANY_CODE_7F3A9C</c>).
    /// </summary>
    /// <returns>An arbitrary error code.</returns>
    public static ErrorCode Any() {
        return Dummies.Any.StringMatching("ANY_CODE_[A-Z0-9]{6}").As(ErrorCode.Create).Generate();
    }

}
