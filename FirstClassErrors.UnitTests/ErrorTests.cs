#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(Error))]
public sealed class ErrorTests : IDisposable {

    #region Constructors declarations

    public ErrorTests() {
        ErrorContextKey.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

    [Fact(DisplayName = "An error has a unique instance identifier.")]
    public void ADiagnosableExceptionHasAUniqueInstanceIdentifier() {
        // Setup
        ErrorCode anyErrorCode    = ErrorCodeFactory.CreateAny();
        string    anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();

        // Exercise
        DomainError firstError  = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);
        DomainError secondError = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(firstError.InstanceId).IsNotEqualTo(Guid.Empty);
        Check.That(secondError.InstanceId).IsNotEqualTo(Guid.Empty);
        Check.That(firstError.InstanceId).IsNotEqualTo(secondError.InstanceId);
    }

    [Fact(DisplayName = "An error captures its occurrence time in UTC.")]
    public void ADiagnosableExceptionCapturesItsOccurrenceTimeInUtc() {
        // Setup
        ErrorCode      anyErrorCode    = ErrorCodeFactory.CreateAny();
        string         anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        DateTimeOffset before          = DateTimeOffset.UtcNow;

        // Exercise
        DomainError error = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);

        // Verify
        DateTimeOffset after = DateTimeOffset.UtcNow;

        // NOTE: We use >= and <= instead of strict > and < because DateTimeOffset.UtcNow
        // does not guarantee sub-millisecond precision. If the constructor execution
        // occurs within the same clock tick as the 'before' or 'after' capture,
        // the values may be equal. The invariant we test is that OccurredAt was
        // captured during construction, not that it is strictly greater.
        Check.That(error.OccurredAt >= before).IsTrue();
        Check.That(error.OccurredAt <= after).IsTrue();
    }

    [Fact(DisplayName = "An error preserves the provided error code.")]
    public void ADiagnosableExceptionPreservesTheProvidedErrorCode() {
        // Setup
        string    anyErrorMessage              = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode temperatureBelowAbsoluteZero = ErrorCode.Create("TEMPERATURE_BELOW_ABSOLUTE_ZERO");

        // Exercise
        DomainError error = DomainError.Create(temperatureBelowAbsoluteZero, anyErrorMessage).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.Code).IsEqualTo(temperatureBelowAbsoluteZero);
    }

    [Fact(DisplayName = "An error preserves the provided short message.")]
    public void ADiagnosableExceptionPreservesTheProvidedShortMessage() {
        // Exercise
        string              anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode           anyErrorCode    = ErrorCodeFactory.CreateAny();
        InfrastructureError error           = InfrastructureError.Create(anyErrorCode, anyErrorMessage, InteractionDirection.Incoming, Transience.NonTransient)
                                                                  .WithPublicMessage("short");

        // Verify
        Check.That(error.ShortMessage).IsEqualTo("short");
    }

    [Fact(DisplayName = "An error preserves the provided diagnostic message.")]
    public void ADiagnosableExceptionPreservesTheProvidedDiagnosticMessage() {
        // Exercise
        ErrorCode   anyErrorCode = ErrorCodeFactory.CreateAny();
        DomainError error        = DomainError.Create(anyErrorCode, "diagnostic").WithPublicMessage("short", "detailed");

        // Verify
        Check.That(error.DiagnosticMessage).IsEqualTo("diagnostic");
        Check.That(error.ShortMessage).IsEqualTo("short");
        Check.That(error.DetailedMessage).IsEqualTo("detailed");
    }

    [Fact(DisplayName = "An error has an empty context when no context is provided.")]
    public void ADiagnosableExceptionHasAnEmptyContextWhenNoContextIsProvided() {
        // Exercise
        string              anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode           anyErrorCode    = ErrorCodeFactory.CreateAny();
        InfrastructureError error           = InfrastructureError.Create(anyErrorCode, anyErrorMessage, InteractionDirection.Outgoing, Transience.Transient)
                                                                  .WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.Context).IsNotNull();
        Check.That(error.Context.IsEmpty).IsTrue();
        Check.That(error.Context.Values).CountIs(0);
    }

    [Fact(DisplayName = "An error includes the provided context entries.")]
    public void ADiagnosableExceptionIncludesTheProvidedContextEntries() {
        // Setup
        string                  anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode               anyErrorCode    = ErrorCodeFactory.CreateAny();
        ErrorContextKey<string> userIdKey       = ErrorContextKey.Create<string>("UserId");

        // Exercise
        InfrastructureError error = InfrastructureError.Create(anyErrorCode, anyErrorMessage, InteractionDirection.Unknown, Transience.Unknown,
                                                               ctx => ctx.Add(userIdKey, "u-123"))
                                                       .WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.Context.IsEmpty).IsFalse();

        bool found = error.Context.TryGet(userIdKey, out string? value);
        Check.That(found).IsTrue();
        Check.That(value).IsEqualTo("u-123");
    }

    [Fact(DisplayName = "An error has no inner errors by default.")]
    public void ADiagnosableExceptionHasNoInnerExceptionsByDefault() {
        // Exercise
        string              anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode           anyErrorCode    = ErrorCodeFactory.CreateAny();
        InfrastructureError error           = InfrastructureError.Create(anyErrorCode, anyErrorMessage, InteractionDirection.Outgoing, Transience.Unknown)
                                                                  .WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.InnerErrors).IsNotNull();
        Check.That(error.InnerErrors).CountIs(0);
    }

    [Fact(DisplayName = "An error preserves a single inner error.")]
    public void ADiagnosableExceptionPreservesASingleInnerException() {
        // Setup
        string      anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode   anyErrorCode    = ErrorCodeFactory.CreateAny();
        DomainError innerError      = DomainError.Create(anyErrorCode, "inner").WithPublicMessage("inner");

        // Exercise
        DomainError rootError = DomainError.Create(anyErrorCode, anyErrorMessage, innerError).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(rootError.InnerErrors).CountIs(1);
        Check.That(rootError.InnerErrors[0]).IsSameReferenceAs(innerError);
    }

    [Fact(DisplayName = "An infrastructure error preserves a single inner error.")]
    public void AnInfrastructureErrorPreservesASingleInnerError() {
        // Setup
        string      anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode   anyErrorCode    = ErrorCodeFactory.CreateAny();
        DomainError innerError      = DomainError.Create(anyErrorCode, "inner").WithPublicMessage("inner");

        // Exercise
        InfrastructureError rootError = InfrastructureError.Create(anyErrorCode, anyErrorMessage, InteractionDirection.Incoming, Transience.NonTransient, innerError)
                                                           .WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(rootError.InnerErrors).CountIs(1);
        Check.That(rootError.InnerErrors[0]).IsSameReferenceAs(innerError);
    }

    [Fact(DisplayName = "An error preserves multiple inner errors.")]
    public void ADiagnosableExceptionPreservesMultipleInnerExceptions() {
        // Setup
        string           anyErrorMessage  = ErrorMessageFactory.CreateAnyMessage();
        ErrorCode        anyErrorCode     = ErrorCodeFactory.CreateAny();
        DomainError      firstInnerError  = DomainError.Create(ErrorCode.Create("first"), "first").WithPublicMessage("first");
        PrimaryPortError secondInnerError = PrimaryPortError.Create(ErrorCode.Create("second"), "second", Transience.Unknown).WithPublicMessage("second");
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors()
                                            .Add(firstInnerError)
                                            .Add(secondInnerError);

        // Exercise
        PrimaryPortError rootError = PrimaryPortError.Create(anyErrorCode, anyErrorMessage, innerErrors).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(rootError.InnerErrors).CountIs(2);
        Check.That(rootError.InnerErrors[0]).IsSameReferenceAs(firstInnerError);
        Check.That(rootError.InnerErrors[1]).IsSameReferenceAs(secondInnerError);
    }

    [Fact(DisplayName = "An error can be created without inner errors even when a null collection is provided.")]
    public void ADiagnosableExceptionCanBeCreatedWithoutInnerExceptionsEvenWhenANullCollectionIsProvided() {
        // Exercise
        ErrorCode   anyErrorCode    = ErrorCodeFactory.CreateAny();
        string      anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        DomainError error           = DomainError.Create(anyErrorCode, anyErrorMessage, innerErrors: null!).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.InnerErrors).CountIs(0);
    }

    [Fact(DisplayName = "An error created with a null inner error has no inner errors.")]
    public void ADiagnosableExceptionCreatedWithANullInnerExceptionHasNoInnerExceptions() {
        // Exercise
        ErrorCode   anyErrorCode    = ErrorCodeFactory.CreateAny();
        string      anyErrorMessage = ErrorMessageFactory.CreateAnyMessage();
        DomainError exception       = DomainError.Create(anyErrorCode, anyErrorMessage, innerError: null!).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(exception.InnerErrors).CountIs(0);
    }

    #region Nested types declarations

    private static class ErrorMessageFactory {

        #region Statics members declarations

        public static string CreateAnyMessage() {
            return "boom";
        }

        #endregion

    }

    #endregion

}
