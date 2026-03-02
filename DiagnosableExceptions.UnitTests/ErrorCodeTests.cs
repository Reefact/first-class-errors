#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[TestSubject(typeof(ErrorCode))]
public class ErrorCodeTests : IDisposable {

    #region Constructors & Destructor

    public ErrorCodeTests() {
        ErrorCode.ResetForTests();
    }

    #endregion

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

    [Fact(DisplayName = "Creating an error code with a duplicate code is rejected.")]
    public void CreatingErrorCodeWithDuplicateCodeIsRejected() {
        // Setup
        const string code = "DUPLICATE_ERROR";
        ErrorCode.Create(code);

        // Exercise & verify
        Check.ThatCode(() => ErrorCode.Create(code))
             .Throws<InvalidOperationException>()
             .WithMessage("Error code 'DUPLICATE_ERROR' has already been registered.");
    }

    [Fact(DisplayName = "Error codes with the same code are equal.")]
    public void ErrorCodesWithSameCodeAreEqual() {
        // Setup
        const string code       = "EQUALS_TEST";
        ErrorCode    errorCode1 = ErrorCode.Create(code);
        ErrorCode.ResetForTests();
        ErrorCode errorCode2 = ErrorCode.Create(code);

        // Exercise & verify
        Check.That(errorCode1).IsEqualTo(errorCode2);
    }

    [Fact(DisplayName = "Error codes with different codes are not equal.")]
    public void ErrorCodesWithDifferentCodesAreNotEqual() {
        // Setup
        ErrorCode errorCode1 = ErrorCode.Create("ERROR_1");
        ErrorCode.ResetForTests();
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
        ErrorCode.ResetForTests();
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
        ErrorCode.ResetForTests();
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
        ErrorCode.ResetForTests();
        ErrorCode errorCode2 = ErrorCode.Create(code);

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
        ErrorCode.ResetForTests();
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
        bool result = errorCode.Equals("TEST");

        // Verify
        Check.That(result).IsFalse();
    }

    [Fact(DisplayName = "Concurrent creation of the same error code results in a single registration.")]
    public void ConcurrentCreationOfSameErrorCodeResultsInSingleRegistration() {
        const string code = "CONCURRENT";

        int successes = 0;
        int failures  = 0;

        Parallel.For(0, 20, _ => {
            try {
                ErrorCode.Create(code);
                Interlocked.Increment(ref successes);
            } catch (InvalidOperationException) {
                Interlocked.Increment(ref failures);
            }
        });

        Check.That(successes).IsEqualTo(1);
        Check.That(failures).IsEqualTo(19);
    }

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorCode.ResetForTests();
    }

}