#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(DiagnosableException))]
public sealed class DiagnosableExceptionTests : IDisposable {

    #region Constructors & Destructor

    public DiagnosableExceptionTests() {
        ErrorContextKey.ResetForTests();
        ErrorCode.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
        ErrorCode.ResetForTests();
    }

    [Fact(DisplayName = "A diagnosable exception has a unique instance identifier.")]
    public void ADiagnosableExceptionHasAUniqueInstanceIdentifier() {
        // Setup
        ErrorCode                anyErrorCode    = ErrorCodeFactory.CreateAny();
        string                   anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        TestDiagnosableException first           = new(anyErrorCode, anyErrorMessage);
        TestDiagnosableException second          = new(anyErrorCode, anyErrorMessage);

        // Exercise
        Guid firstId  = first.InstanceId;
        Guid secondId = second.InstanceId;

        // Verify
        Check.That(firstId).IsNotEqualTo(Guid.Empty);
        Check.That(secondId).IsNotEqualTo(Guid.Empty);
        Check.That(firstId).IsNotEqualTo(secondId);
    }

    [Fact(DisplayName = "A diagnosable exception captures its occurrence time in UTC.")]
    public void ADiagnosableExceptionCapturesItsOccurrenceTimeInUtc() {
        // Setup
        ErrorCode      anyErrorCode    = ErrorCodeFactory.CreateAny();
        string         anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        DateTimeOffset before          = DateTimeOffset.UtcNow;

        // Exercise
        TestDiagnosableException exception = new(anyErrorCode, anyErrorMessage);

        // Verify
        DateTimeOffset after = DateTimeOffset.UtcNow;

        // NOTE: We use >= and <= instead of strict > and < because DateTimeOffset.UtcNow
        // does not guarantee sub-millisecond precision. If the constructor execution
        // occurs within the same clock tick as the 'before' or 'after' capture,
        // the values may be equal. The invariant we test is that OccurredAt was
        // captured during construction, not that it is strictly greater.
        Check.That(exception.OccurredAt >= before).IsTrue();
        Check.That(exception.OccurredAt <= after).IsTrue();
    }

    [Fact(DisplayName = "A diagnosable exception preserves the provided error code.")]
    public void ADiagnosableExceptionPreservesTheProvidedErrorCode() {
        // Setup
        string    anyErrorMessage              = ExceptionMessageFactory.CreateAnyMessage();
        ErrorCode temperatureBelowAbsoluteZero = ErrorCode.Create("TEMPERATURE_BELOW_ABSOLUTE_ZERO");

        // Exercise
        TestDiagnosableException exception = new(temperatureBelowAbsoluteZero, anyErrorMessage);

        // Verify
        Check.That(exception.ErrorCode).IsEqualTo(temperatureBelowAbsoluteZero);
    }

    [Fact(DisplayName = "A diagnosable exception preserves the provided short message.")]
    public void ADiagnosableExceptionPreservesTheProvidedShortMessage() {
        // Exercise
        string                   anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        ErrorCode                anyErrorCode    = ErrorCodeFactory.CreateAny();
        TestDiagnosableException exception       = new(anyErrorCode, anyErrorMessage, "short");

        // Verify
        Check.That(exception.ShortMessage).IsEqualTo("short");
    }

    [Fact(DisplayName = "A diagnosable exception has an empty context when no context is provided.")]
    public void ADiagnosableExceptionHasAnEmptyContextWhenNoContextIsProvided() {
        // Exercise
        string                   anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        ErrorCode                anyErrorCode    = ErrorCodeFactory.CreateAny();
        TestDiagnosableException exception       = new(anyErrorCode, anyErrorMessage);

        // Verify
        Check.That(exception.Context).IsNotNull();
        Check.That(exception.Context.IsEmpty).IsTrue();
        Check.That(exception.Context.Values).CountIs(0);
    }

    [Fact(DisplayName = "A diagnosable exception includes the provided context entries.")]
    public void ADiagnosableExceptionIncludesTheProvidedContextEntries() {
        // Setup
        string                  anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        ErrorCode               anyErrorCode    = ErrorCodeFactory.CreateAny();
        ErrorContextKey<string> userIdKey       = ErrorContextKey.Create<string>("UserId");

        // Exercise
        TestDiagnosableException exception = new(anyErrorCode, anyErrorMessage,
                                                 configureContext: ctx => ctx.Add(userIdKey, "u-123"));

        // Verify
        Check.That(exception.Context.IsEmpty).IsFalse();

        bool found = exception.Context.TryGet(userIdKey, out string? value);
        Check.That(found).IsTrue();
        Check.That(value).IsEqualTo("u-123");
    }

    [Fact(DisplayName = "A diagnosable exception has no inner exceptions by default.")]
    public void ADiagnosableExceptionHasNoInnerExceptionsByDefault() {
        // Exercise
        string                   anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        ErrorCode                anyErrorCode    = ErrorCodeFactory.CreateAny();
        TestDiagnosableException exception       = new(anyErrorCode, anyErrorMessage);

        // Verify
        Check.That(exception.InnerExceptions).IsNotNull();
        Check.That(exception.InnerExceptions).CountIs(0);
        Check.That(exception.HasInnerExceptions()).IsFalse();
        Check.That(exception.InnerException).IsNull();
    }

    [Fact(DisplayName = "A diagnosable exception preserves a single inner exception.")]
    public void ADiagnosableExceptionPreservesASingleInnerException() {
        // Setup
        string    anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        ErrorCode anyErrorCode    = ErrorCodeFactory.CreateAny();
        Exception inner           = new InvalidOperationException("inner");

        // Exercise
        TestDiagnosableException exception = new(anyErrorCode, anyErrorMessage, inner);

        // Verify
        Check.That(exception.InnerException).IsSameReferenceAs(inner);
        Check.That(exception.InnerExceptions).CountIs(1);
        Check.That(exception.InnerExceptions[0]).IsSameReferenceAs(inner);
        Check.That(exception.HasInnerExceptions()).IsTrue();
    }

    [Fact(DisplayName = "A diagnosable exception preserves multiple inner exceptions.")]
    public void ADiagnosableExceptionPreservesMultipleInnerExceptions() {
        // Setup
        string    anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        ErrorCode anyErrorCode    = ErrorCodeFactory.CreateAny();
        Exception first           = new InvalidOperationException("first");
        Exception second          = new ArgumentException("second");

        IReadOnlyList<Exception> provided = [first, second];

        // Exercise
        TestDiagnosableException exception = new(anyErrorCode, anyErrorMessage, provided);

        // Verify
        Check.That(exception.InnerException).IsNull();
        Check.That(exception.InnerExceptions).CountIs(2);
        Check.That(exception.InnerExceptions[0]).IsSameReferenceAs(first);
        Check.That(exception.InnerExceptions[1]).IsSameReferenceAs(second);
        Check.That(exception.HasInnerExceptions()).IsTrue();
    }

    [Fact(DisplayName = "A diagnosable exception can be created without inner exceptions even when a null collection is provided.")]
    public void ADiagnosableExceptionCanBeCreatedWithoutInnerExceptionsEvenWhenANullCollectionIsProvided() {
        // Exercise
        ErrorCode                anyErrorCode    = ErrorCodeFactory.CreateAny();
        string                   anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        TestDiagnosableException exception       = new(anyErrorCode, anyErrorMessage, innerExceptions: null!);

        // Verify
        Check.That(exception.InnerExceptions).CountIs(0);
        Check.That(exception.HasInnerExceptions()).IsFalse();
    }

    [Fact(DisplayName = "A diagnosable exception created with a null inner exception has no inner exceptions.")]
    public void ADiagnosableExceptionCreatedWithANullInnerExceptionHasNoInnerExceptions() {
        // Exercise
        ErrorCode                anyErrorCode    = ErrorCodeFactory.CreateAny();
        string                   anyErrorMessage = ExceptionMessageFactory.CreateAnyMessage();
        TestDiagnosableException exception       = new(anyErrorCode, anyErrorMessage, innerException: null!);

        // Verify
        Check.That(exception.InnerException).IsNull();
        Check.That(exception.InnerExceptions).IsNotNull();
        Check.That(exception.InnerExceptions).CountIs(0);
        Check.That(exception.HasInnerExceptions()).IsFalse();
    }

    #region Nested types

    private sealed class TestDiagnosableException : DiagnosableException {

        #region Constructors & Destructor

        public TestDiagnosableException(ErrorCode                    errorCode,
                                        string                       errorMessage,
                                        string?                      shortMessage     = null,
                                        Action<ErrorContextBuilder>? configureContext = null)
            : base(errorCode, errorMessage, shortMessage, configureContext) { }

        public TestDiagnosableException(ErrorCode                    errorCode,
                                        string                       errorMessage,
                                        Exception                    innerException,
                                        string?                      shortMessage     = null,
                                        Action<ErrorContextBuilder>? configureContext = null)
            : base(errorCode, errorMessage, innerException, shortMessage, configureContext) { }

        public TestDiagnosableException(ErrorCode                    errorCode,
                                        string                       errorMessage,
                                        IEnumerable<Exception>       innerExceptions,
                                        string?                      shortMessage     = null,
                                        Action<ErrorContextBuilder>? configureContext = null)
            : base(errorCode, errorMessage, innerExceptions, shortMessage, configureContext) { }

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