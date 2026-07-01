#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(ErrorCode))]
public sealed class ErrorCodeConversionTests {

    [Fact(DisplayName = "Implicitly converting a valid error code returns its code string.")]
    public void ImplicitlyConvertingAValidErrorCodeReturnsItsCodeString() {
        // Exercise
        string s = ErrorCode.Unspecified;

        // Verify
        Check.That(s).IsEqualTo("#UNSPECIFIED");
    }

    [Fact(DisplayName = "Implicitly converting a null error code throws.")]
    public void ImplicitlyConvertingANullErrorCodeThrows() {
        // Exercise & verify
        Check.ThatCode(() => {
                 ErrorCode code = null!;
                 string    s    = code;
                 _ = s;
             })
             .Throws<ArgumentNullException>();
    }

}
