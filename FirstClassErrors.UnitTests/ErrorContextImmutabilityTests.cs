#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(ErrorContext))]
public sealed class ErrorContextImmutabilityTests : IDisposable {

    #region Constructors declarations

    public ErrorContextImmutabilityTests() {
        ErrorContextKey.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "xUnit teardown hook.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

    [Fact(DisplayName = "The context values are read-only and cannot be mutated.")]
    public void TheContextValuesAreReadOnlyAndCannotBeMutated() {
        // Setup
        ErrorContextKey<string> key = ErrorContextKey.Create<string>("K");
        ErrorContext context = DomainError.Create(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(),
                                                  ctx => ctx.Add(key, Dummies.Any.String().NonEmpty().Generate())).WithPublicMessage(ShortMessageFactory.Any()).Context;

        // Exercise & verify
        Check.ThatCode(() => ((IDictionary<ErrorContextKey, object?>)context.Values).Clear())
             .Throws<NotSupportedException>();
    }

    [Fact(DisplayName = "An entry whose stored value is null is present but reported as not found.")]
    public void AnEntryWhoseStoredValueIsNullIsPresentButReportedAsNotFound() {
        // Setup
        ErrorContextKey<string> key = ErrorContextKey.Create<string>("K");
        ErrorContext context = DomainError.Create(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(),
                                                  ctx => ctx.Add(key, null)).WithPublicMessage(ShortMessageFactory.Any()).Context;

        // Exercise
        bool found = context.TryGet(key, out _);

        // Verify
        Check.That(context.IsEmpty).IsFalse();
        Check.That(context.Values).CountIs(1);
        Check.That(found).IsFalse();
    }

}
