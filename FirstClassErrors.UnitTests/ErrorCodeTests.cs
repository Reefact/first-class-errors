#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(ErrorCode))]
public class ErrorCodeTests {

    [Theory(DisplayName = "Creating an error code with a valid code succeeds.")]
    [InlineData("ERROR_001")]
    [InlineData("ERROR_002")]
    [InlineData("ERROR_003")]
    public void CreatingErrorCodeWithValidCodeSucceeds(string code) {
        // Exercise
        ErrorCode errorCode = ErrorCode.Create(code);

        // Verify
        Check.That(errorCode).IsNotNull();
        Check.That(errorCode.ToString()).IsEqualTo(code);
    }

    [Theory(DisplayName = "Creating an error code with a null, empty or blank code is rejected.")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreatingErrorCodeWithBlankCodeIsRejected(string? code) {
        // Exercise & verify
        Check.ThatCode(() => ErrorCode.Create(code!))
             .Throws<ArgumentException>()
             .WithMessage("Error code cannot be null or whitespace. (Parameter 'code')");
    }

    [Fact(DisplayName = "Creating the same code twice is allowed and produces equal instances.")]
    public void CreatingTheSameCodeTwiceIsAllowedAndProducesEqualInstances() {
        // Setup
        const string code = "DUPLICATE_ERROR";

        // Exercise
        ErrorCode first  = ErrorCode.Create(code);
        ErrorCode second = ErrorCode.Create(code);

        // Verify
        Check.That(first).IsEqualTo(second);
    }

    [Fact(DisplayName = "Error codes with the same code are equal.")]
    public void ErrorCodesWithSameCodeAreEqual() {
        // Setup
        const string code       = "EQUALS_TEST";
        ErrorCode    errorCode1 = ErrorCode.Create(code);
        ErrorCode    errorCode2 = ErrorCode.Create(code);

        // Exercise & verify
        Check.That(errorCode1).IsEqualTo(errorCode2);
    }

    [Fact(DisplayName = "Error codes with different codes are not equal.")]
    public void ErrorCodesWithDifferentCodesAreNotEqual() {
        // Setup
        ErrorCode errorCode1 = ErrorCode.Create("ERROR_1");
        ErrorCode errorCode2 = ErrorCode.Create("ERROR_2");

        // Exercise & verify
        Check.That(errorCode1).IsNotEqualTo(errorCode2);
    }

    [Fact(DisplayName = "An error code compared to null is not equal.")]
    public void ErrorCodeComparedToNullIsNotEqual() {
        // Setup
        ErrorCode? errorCode = ErrorCode.Create("NULL_TEST");

        // Exercise
        bool result = errorCode.Equals(null);

        // Verify
        Check.That(result).IsFalse();
    }

    [Fact(DisplayName = "The equality operator returns true for error codes with the same code.")]
    public void EqualityOperatorReturnsTrueForErrorCodesWithSameCode() {
        // Setup
        string    code       = "OPERATOR_EQUALS";
        ErrorCode errorCode1 = ErrorCode.Create(code);
        ErrorCode errorCode2 = ErrorCode.Create(code);

        // Exercise
        bool result = errorCode1 == errorCode2;

        // Verify
        Check.That(result).IsTrue();
    }

    [Fact(DisplayName = "The inequality operator returns true for error codes with different codes")]
    public void InequalityOperatorReturnsTrueForErrorCodesWithDifferentCodes() {
        // Setup
        ErrorCode errorCode1 = ErrorCode.Create("ERROR_1");
        ErrorCode errorCode2 = ErrorCode.Create("ERROR_2");

        // Exercise
        bool result = errorCode1 != errorCode2;

        // Verify
        Check.That(result).IsTrue();
    }

    [Fact(DisplayName = "Implicitly converting an error code to string returns its code representation.")]
    public void ImplicitlyConvertingErrorCodeToStringReturnsItsCodeRepresentation() {
        // Setup
        const string code      = "IMPLICIT_TEST";
        ErrorCode    errorCode = ErrorCode.Create(code);

        // Exercise
        string result = errorCode;

        // Verify
        Check.That(result).IsEqualTo(code);
    }

    [Fact(DisplayName = "Error codes with the same code produce the same hash code.")]
    public void ErrorCodesWithSameCodeProduceTheSameHashCode() {
        // Setup
        const string code       = "HASH_TEST";
        ErrorCode    errorCode1 = ErrorCode.Create(code);
        ErrorCode    errorCode2 = ErrorCode.Create(code);

        // Exercise
        int hash1 = errorCode1.GetHashCode();
        int hash2 = errorCode2.GetHashCode();

        // Verify
        Check.That(hash1).IsEqualTo(hash2);
    }

    [Fact(DisplayName = "Error codes with different codes may produce different hash codes.")]
    public void ErrorCodesWithDifferentCodesMayProduceDifferentHashCodes() {
        // Setup
        ErrorCode errorCode1 = ErrorCode.Create("HASH_1");
        ErrorCode errorCode2 = ErrorCode.Create("HASH_2");

        // Exercise
        int hash1 = errorCode1.GetHashCode();
        int hash2 = errorCode2.GetHashCode();

        // Verify
        Check.That(hash1).IsNotEqualTo(hash2);
    }

    [Fact(DisplayName = "An error code compared to an object of another type is not equal.")]
    public void ErrorCodeComparedToObjectOfDifferentTypeIsNotEqual() {
        // Setup
        ErrorCode errorCode = ErrorCode.Create("TEST");

        // Exercise
        // ReSharper disable once SuspiciousTypeConversion.Global
        bool result = errorCode.Equals("TEST");

        // Verify
        Check.That(result).IsFalse();
    }

    [Fact(DisplayName = "Concurrent creation of the same code always succeeds and yields equal instances.")]
    public void ConcurrentCreationOfSameCodeAlwaysSucceedsAndYieldsEqualInstances() {
        // Setup
        const string code      = "CONCURRENT";
        ErrorCode    reference = ErrorCode.Create(code);

        ErrorCode[] created = new ErrorCode[20];

        // Exercise
        Parallel.For(0, 20, index => created[index] = ErrorCode.Create(code));

        // Verify
        Check.That(created.All(errorCode => errorCode == reference)).IsTrue();
    }

}
