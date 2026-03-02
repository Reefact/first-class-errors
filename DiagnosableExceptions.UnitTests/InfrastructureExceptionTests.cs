#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(InfrastructureException))]
public sealed class InfrastructureExceptionTests : IDisposable {

    #region Constructors & Destructor

    public InfrastructureExceptionTests() {
        ErrorContextKey.ResetForTests();
        ErrorCode.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
        ErrorCode.ResetForTests();
    }

    [Theory(DisplayName = "An infrastructure exception preserves its transient classification.")]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void AnInfrastructureExceptionPreservesItsTransientClassification(bool? isTransient) {
        // Setup
        ErrorCode anyErrorCode = ErrorCodeFactory.CreateAny();
        string    anyMessage   = ExceptionMessageFactory.CreateAnyMessage();

        // Exercise
        TestInfrastructureException exception = new(anyErrorCode, anyMessage, isTransient: isTransient);

        // Verify
        Check.That(exception.IsTransient).IsEqualTo(isTransient);
    }

    [Theory(DisplayName = "An infrastructure exception with an inner exception preserves its transient classification.")]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void AnInfrastructureExceptionWithAnInnerExceptionPreservesItsTransientClassification(bool? isTransient) {
        // Setup
        ErrorCode anyErrorCode = ErrorCodeFactory.CreateAny();
        string    anyMessage   = ExceptionMessageFactory.CreateAnyMessage();
        Exception inner        = new InvalidOperationException("inner");

        // Exercise
        TestInfrastructureException exception = new(anyErrorCode, anyMessage, inner, isTransient: isTransient);

        // Verify
        Check.That(exception.IsTransient).IsEqualTo(isTransient);
    }

    [Theory(DisplayName = "An infrastructure exception with multiple inner exceptions preserves its transient classification.")]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void AnInfrastructureExceptionWithMultipleInnerExceptionsPreservesItsTransientClassification(bool? isTransient) {
        // Setup
        ErrorCode anyErrorCode = ErrorCodeFactory.CreateAny();
        string    anyMessage   = ExceptionMessageFactory.CreateAnyMessage();

        Exception first  = new InvalidOperationException("first");
        Exception second = new ArgumentException("second");

        IEnumerable<Exception> innerExceptions = new[] { first, second };

        // Exercise
        TestInfrastructureException exception = new(anyErrorCode, anyMessage, innerExceptions, isTransient: isTransient);

        // Verify
        Check.That(exception.IsTransient).IsEqualTo(isTransient);
    }

    #region Nested types

    private sealed class TestInfrastructureException : InfrastructureException {

        #region Constructors & Destructor

        public TestInfrastructureException(ErrorCode                    errorCode,
                                           string                       errorMessage,
                                           string?                      shortMessage     = null,
                                           bool?                        isTransient      = null,
                                           Action<ErrorContextBuilder>? configureContext = null)
            : base(errorCode, errorMessage, shortMessage, isTransient, configureContext) { }

        public TestInfrastructureException(ErrorCode                    errorCode,
                                           string                       errorMessage,
                                           Exception                    innerException,
                                           string?                      shortMessage     = null,
                                           bool?                        isTransient      = null,
                                           Action<ErrorContextBuilder>? configureContext = null)
            : base(errorCode, errorMessage, innerException, shortMessage, isTransient, configureContext) { }

        public TestInfrastructureException(ErrorCode                    errorCode,
                                           string                       errorMessage,
                                           IEnumerable<Exception>       innerExceptions,
                                           string?                      shortMessage     = null,
                                           bool?                        isTransient      = null,
                                           Action<ErrorContextBuilder>? configureContext = null)
            : base(errorCode, errorMessage, innerExceptions, shortMessage, isTransient, configureContext) { }

        #endregion

    }

    private static class ExceptionMessageFactory {

        #region Static members

        public static string CreateAnyMessage() {
            return "boom";
        }

        #endregion

    }

    #endregion

}