#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(ErrorContext))]
public sealed class ErrorContextTests : IDisposable {

    #region Constructors & Destructor

    public ErrorContextTests() {
        ErrorContextKey.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

    [Fact(DisplayName = "An error context cannot be created from null values.")]
    public void AnErrorContextCannotBeCreatedFromNullValues() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorContext(null!))
             .ThrowsAny();
    }

    [Fact(DisplayName = "An empty error context contains no entries.")]
    public void EmptyErrorContextContainsNoEntries() {
        // Exercise
        ErrorContext context = ErrorContext.Empty;

        // Verify
        Check.That(context.IsEmpty).IsTrue();
        Check.That(context.Values).IsNotNull();
        Check.That(context.Values).CountIs(0);
    }

    [Fact(DisplayName = "An error context is not affected by later changes to the source values.")]
    public void ErrorContextIsNotAffectedByLaterChangesToTheSourceValues() {
        // Setup
        ErrorContextKey<string> userIdKey = ErrorContextKey.Create<string>("UserId");
        Dictionary<ErrorContextKey, object?> values = new() {
            { userIdKey, "u-123" }
        };
        ErrorContext context = new(values);

        // Exercise
        values[userIdKey] = "u-999";
        values.Add(ErrorContextKey.Create<int>("OtherKey"), 42);

        // Verify
        bool found = context.TryGet(userIdKey, out string? value);

        Check.That(context.Values).CountIs(1);
        Check.That(found).IsTrue();
        Check.That(value).IsEqualTo("u-123");
    }

    [Fact(DisplayName = "A stored value can be retrieved when the key exists and the type matches.")]
    public void StoredValueCanBeRetrievedWhenTheKeyExistsAndTheTypeMatches() {
        // Setup
        ErrorContextKey<int> dealIdKey = ErrorContextKey.Create<int>("DealId");
        Dictionary<ErrorContextKey, object?> values = new() {
            { dealIdKey, 123 }
        };
        ErrorContext context = new(values);

        // Exercise
        bool found = context.TryGet(dealIdKey, out int value);

        // Verify
        Check.That(found).IsTrue();
        Check.That(value).IsEqualTo(123);
    }

    [Fact(DisplayName = "A missing key cannot be retrieved from an error context.")]
    public void MissingKeyCannotBeRetrievedFromAnErrorContext() {
        // Setup
        ErrorContextKey<string> correlationIdKey = ErrorContextKey.Create<string>("CorrelationId");
        ErrorContext            context          = new(new Dictionary<ErrorContextKey, object?>());

        // Exercise
        bool found = context.TryGet(correlationIdKey, out string? value);

        // Verify
        Check.That(found).IsFalse();
        Check.That(value).IsNull();
    }

    [Fact(DisplayName = "A value cannot be retrieved with an incompatible expected type.")]
    public void ValueCannotBeRetrievedWithAnIncompatibleExpectedType() {
        // Setup
        ErrorContextKey<string> key = ErrorContextKey.Create<string>("SomeKey");
        Dictionary<ErrorContextKey, object?> values = new() {
            { key, 123 }
        };
        ErrorContext context = new(values);

        // Exercise
        bool found = context.TryGet(key, out string? value);

        // Verify
        Check.That(found).IsFalse();
        Check.That(value).IsNull();
    }

    [Fact(DisplayName = "A null stored value cannot be retrieved as a typed value.")]
    public void NullStoredValueCannotBeRetrievedAsATypedValue() {
        // Setup
        ErrorContextKey<string> key = ErrorContextKey.Create<string>("OptionalInfo");
        Dictionary<ErrorContextKey, object?> values = new() {
            { key, null }
        };
        ErrorContext context = new(values);

        // Exercise
        bool found = context.TryGet(key, out string? value);

        // Verify
        Check.That(found).IsFalse();
        Check.That(value).IsNull();
    }

    [Fact(DisplayName = "An error context can be converted to a name-based dictionary.")]
    public void AnErrorContextCanBeConvertedToANameBasedDictionary() {
        // Setup
        ErrorContextKey<int>    dealIdKey = ErrorContextKey.Create<int>("DealId");
        ErrorContextKey<string> userIdKey = ErrorContextKey.Create<string>("UserId");
        Dictionary<ErrorContextKey, object?> values = new() {
            { dealIdKey, 123 },
            { userIdKey, "u-123" }
        };
        ErrorContext context = new(values);

        // Exercise
        IReadOnlyDictionary<string, object?> dict = context.ToNameDictionary();

        // Verify
        string[] expectedKeys = new[] { "DealId", "UserId" }.OrderBy(x => x).ToArray();

        Check.That(dict.Keys.OrderBy(x => x)).ContainsExactly(expectedKeys);
        Check.That(dict["DealId"]).IsEqualTo(123);
        Check.That(dict["UserId"]).IsEqualTo("u-123");
    }

    [Fact(DisplayName = "Converting an error context to a name-based dictionary returns a new dictionary each time.")]
    public void ConvertingAnErrorContextToANameBasedDictionaryReturnsANewDictionaryEachTime() {
        // Setup
        ErrorContextKey<int> dealIdKey = ErrorContextKey.Create<int>("DealId");
        Dictionary<ErrorContextKey, object?> values = new() {
            { dealIdKey, 123 }
        };
        ErrorContext context = new(values);

        // Exercise
        IReadOnlyDictionary<string, object?> first  = context.ToNameDictionary();
        IReadOnlyDictionary<string, object?> second = context.ToNameDictionary();

        // Verify
        Check.That(ReferenceEquals(first, second)).IsFalse();
        Check.That(first["DealId"]).IsEqualTo(123);
        Check.That(second["DealId"]).IsEqualTo(123);
    }

}