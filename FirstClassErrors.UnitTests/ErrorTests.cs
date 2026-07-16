#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

using NSubstitute;

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
        ErrorCode anyErrorCode    = Any.ErrorCode();
        string    anyErrorMessage = Any.DiagnosticMessage();

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
        ErrorCode      anyErrorCode    = Any.ErrorCode();
        string         anyErrorMessage = Any.DiagnosticMessage();
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

    [Fact(DisplayName = "An error captures its occurrence time from the ambient clock.")]
    public void AnErrorCapturesItsOccurrenceTimeFromTheAmbientClock() {
        // Setup
        ErrorCode      anyErrorCode    = Any.ErrorCode();
        string         anyErrorMessage = Any.DiagnosticMessage();
        DateTimeOffset instant         = new(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);
        IClock         clock           = Substitute.For<IClock>();
        clock.UtcNow.Returns(instant);

        // Exercise
        using (Clock.Use(clock)) {
            DomainError error = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);

            // Verify: the occurrence time is exactly the mocked instant, no time-window juggling required.
            Check.That(error.OccurredAt).IsEqualTo(instant);
        }

        // Verify: the override is restored once the scope ends; a new error no longer sees the mocked instant.
        DomainError afterScope = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);
        Check.That(afterScope.OccurredAt).IsNotEqualTo(instant);
    }

    [Fact(DisplayName = "An error captures its instance id from the ambient source.")]
    public void AnErrorCapturesItsInstanceIdFromTheAmbientSource() {
        // Setup
        ErrorCode anyErrorCode    = Any.ErrorCode();
        string    anyErrorMessage = Any.DiagnosticMessage();
        Guid      fixedId         = new("11111111-1111-1111-1111-111111111111");

        // Exercise
        using (InstanceIds.UseFixed(fixedId)) {
            DomainError error = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);

            // Verify
            Check.That(error.InstanceId).IsEqualTo(fixedId);
        }

        // Verify: the override is restored once the scope ends.
        DomainError afterScope = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);
        Check.That(afterScope.InstanceId).IsNotEqualTo(fixedId);
    }

    [Fact(DisplayName = "A custom id source assigns distinct identifiers within the scope.")]
    public void ACustomIdSourceAssignsDistinctIdentifiersWithinTheScope() {
        // Setup
        ErrorCode anyErrorCode    = Any.ErrorCode();
        string    anyErrorMessage = Any.DiagnosticMessage();
        int       counter         = 0;

        // Exercise: callers who want a sequence roll their own through Use(Func<Guid>).
        using (InstanceIds.Use(() => new Guid(++counter, 0, 0, new byte[8]))) {
            DomainError first  = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);
            DomainError second = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);

            // Verify
            Check.That(first.InstanceId).IsEqualTo(new Guid("00000001-0000-0000-0000-000000000000"));
            Check.That(second.InstanceId).IsEqualTo(new Guid("00000002-0000-0000-0000-000000000000"));
        }
    }

    [Fact(DisplayName = "UseSequential assigns readable, monotonically increasing identifiers within the scope.")]
    public void UseSequentialAssignsMonotonicIdentifiersWithinTheScope() {
        // Setup
        ErrorCode anyErrorCode    = Any.ErrorCode();
        string    anyErrorMessage = Any.DiagnosticMessage();

        // Exercise
        using (InstanceIds.UseSequential()) {
            DomainError first  = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);
            DomainError second = DomainError.Create(anyErrorCode, anyErrorMessage).WithPublicMessage(anyErrorMessage);

            // Verify
            Check.That(first.InstanceId).IsEqualTo(new Guid("00000001-0000-0000-0000-000000000000"));
            Check.That(second.InstanceId).IsEqualTo(new Guid("00000002-0000-0000-0000-000000000000"));
        }
    }

    [Fact(DisplayName = "An error preserves the provided error code.")]
    public void ADiagnosableExceptionPreservesTheProvidedErrorCode() {
        // Setup
        string    anyErrorMessage              = Any.DiagnosticMessage();
        ErrorCode temperatureBelowAbsoluteZero = ErrorCode.Create("TEMPERATURE_BELOW_ABSOLUTE_ZERO");

        // Exercise
        DomainError error = DomainError.Create(temperatureBelowAbsoluteZero, anyErrorMessage).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.Code).IsEqualTo(temperatureBelowAbsoluteZero);
    }

    [Fact(DisplayName = "An error preserves the provided short message.")]
    public void ADiagnosableExceptionPreservesTheProvidedShortMessage() {
        // Exercise
        string              anyErrorMessage = Any.DiagnosticMessage();
        ErrorCode           anyErrorCode    = Any.ErrorCode();
        InfrastructureError error           = InfrastructureError.Create(anyErrorCode, anyErrorMessage, Any.InteractionDirection(), Any.Transience())
                                                                  .WithPublicMessage("short");

        // Verify
        Check.That(error.ShortMessage).IsEqualTo("short");
    }

    [Fact(DisplayName = "An error preserves the provided diagnostic message.")]
    public void ADiagnosableExceptionPreservesTheProvidedDiagnosticMessage() {
        // Exercise
        ErrorCode   anyErrorCode = Any.ErrorCode();
        DomainError error        = DomainError.Create(anyErrorCode, "diagnostic").WithPublicMessage("short", "detailed");

        // Verify
        Check.That(error.DiagnosticMessage).IsEqualTo("diagnostic");
        Check.That(error.ShortMessage).IsEqualTo("short");
        Check.That(error.DetailedMessage).IsEqualTo("detailed");
    }

    [Fact(DisplayName = "An error has an empty context when no context is provided.")]
    public void ADiagnosableExceptionHasAnEmptyContextWhenNoContextIsProvided() {
        // Exercise
        string              anyErrorMessage = Any.DiagnosticMessage();
        ErrorCode           anyErrorCode    = Any.ErrorCode();
        InfrastructureError error           = InfrastructureError.Create(anyErrorCode, anyErrorMessage, Any.InteractionDirection(), Any.Transience())
                                                                  .WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.Context).IsNotNull();
        Check.That(error.Context.IsEmpty).IsTrue();
        Check.That(error.Context.Values).CountIs(0);
    }

    [Fact(DisplayName = "An error includes the provided context entries.")]
    public void ADiagnosableExceptionIncludesTheProvidedContextEntries() {
        // Setup
        string                  anyErrorMessage = Any.DiagnosticMessage();
        ErrorCode               anyErrorCode    = Any.ErrorCode();
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
        string              anyErrorMessage = Any.DiagnosticMessage();
        ErrorCode           anyErrorCode    = Any.ErrorCode();
        InfrastructureError error           = InfrastructureError.Create(anyErrorCode, anyErrorMessage, Any.InteractionDirection(), Transience.Unknown)
                                                                  .WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.InnerErrors).IsNotNull();
        Check.That(error.InnerErrors).CountIs(0);
    }

    [Fact(DisplayName = "An error preserves a single inner error.")]
    public void ADiagnosableExceptionPreservesASingleInnerException() {
        // Setup
        string      anyErrorMessage = Any.DiagnosticMessage();
        ErrorCode   anyErrorCode    = Any.ErrorCode();
        DomainError innerError      = DomainError.Create(anyErrorCode, Any.DiagnosticMessage()).WithPublicMessage(Any.ShortMessage());

        // Exercise
        DomainError rootError = DomainError.Create(anyErrorCode, anyErrorMessage, innerError).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(rootError.InnerErrors).CountIs(1);
        Check.That(rootError.InnerErrors[0]).IsSameReferenceAs(innerError);
    }

    [Fact(DisplayName = "An infrastructure error preserves a single inner error.")]
    public void AnInfrastructureErrorPreservesASingleInnerError() {
        // Setup
        string      anyErrorMessage = Any.DiagnosticMessage();
        ErrorCode   anyErrorCode    = Any.ErrorCode();
        DomainError innerError      = DomainError.Create(anyErrorCode, Any.DiagnosticMessage()).WithPublicMessage(Any.ShortMessage());

        // Exercise
        InfrastructureError rootError = InfrastructureError.Create(anyErrorCode, anyErrorMessage, Any.InteractionDirection(), Any.Transience(), innerError)
                                                           .WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(rootError.InnerErrors).CountIs(1);
        Check.That(rootError.InnerErrors[0]).IsSameReferenceAs(innerError);
    }

    [Fact(DisplayName = "An error preserves multiple inner errors.")]
    public void ADiagnosableExceptionPreservesMultipleInnerExceptions() {
        // Setup
        string           anyErrorMessage  = Any.DiagnosticMessage();
        ErrorCode        anyErrorCode     = Any.ErrorCode();
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
        ErrorCode   anyErrorCode    = Any.ErrorCode();
        string      anyErrorMessage = Any.DiagnosticMessage();
        DomainError error           = DomainError.Create(anyErrorCode, anyErrorMessage, innerErrors: null!).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(error.InnerErrors).CountIs(0);
    }

    [Fact(DisplayName = "An error created with a null inner error has no inner errors.")]
    public void ADiagnosableExceptionCreatedWithANullInnerExceptionHasNoInnerExceptions() {
        // Exercise
        ErrorCode   anyErrorCode    = Any.ErrorCode();
        string      anyErrorMessage = Any.DiagnosticMessage();
        DomainError exception       = DomainError.Create(anyErrorCode, anyErrorMessage, innerError: null!).WithPublicMessage(anyErrorMessage);

        // Verify
        Check.That(exception.InnerErrors).CountIs(0);
    }

}
