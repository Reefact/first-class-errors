#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace FirstClassErrors.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="ErrorCode" />. They assert the invariants that must hold for every
///     valid code rather than for a handful of hand-picked examples: an error code is a verbatim, order-sensitive
///     value that round-trips through its textual representations.
/// </summary>
[TestSubject(typeof(ErrorCode))]
public sealed class ErrorCodePropertyTests {

    [Fact(DisplayName = "ErrorCode.Create preserves the code verbatim through ToString (no normalization).")]
    public void CreatePreservesCodeThroughToString() {
        Prop.ForAll(Generators.NonBlank().ToArbitrary(), code => ErrorCode.Create(code).ToString() == code)
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "ErrorCode.Create preserves the code verbatim through the implicit string conversion.")]
    public void CreatePreservesCodeThroughImplicitString() {
        Prop.ForAll(Generators.NonBlank().ToArbitrary(), code => (string)ErrorCode.Create(code) == code)
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Two ErrorCodes are equal if and only if their codes are ordinally equal.")]
    public void EqualityIsOrdinal() {
        Gen<string> code = Generators.NonBlank();
        var pairs = (from left in code
                     from right in code
                     select (left, right)).ToArbitrary();

        Prop.ForAll(pairs,
                    pair => (ErrorCode.Create(pair.left) == ErrorCode.Create(pair.right))
                            == string.Equals(pair.left, pair.right, StringComparison.Ordinal))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Codes built from the same string share the same hash code.")]
    public void EqualCodesShareHashCode() {
        Prop.ForAll(Generators.NonBlank().ToArbitrary(),
                    code => ErrorCode.Create(code).GetHashCode() == ErrorCode.Create(code).GetHashCode())
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "ErrorCode.Create rejects any blank code with an ArgumentException.")]
    public void CreateRejectsBlankCodes() {
        Prop.ForAll(Generators.Blank().ToArbitrary(), blank => Expect.Throws<ArgumentException>(() => ErrorCode.Create(blank)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "ErrorCode.Create rejects a null code with an ArgumentException.")]
    public void CreateRejectsNullCode() {
        Assert.Throws<ArgumentException>(() => ErrorCode.Create(null!));
    }

}
