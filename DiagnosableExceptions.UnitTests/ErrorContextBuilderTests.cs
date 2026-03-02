#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[TestSubject(typeof(ErrorContextBuilder))]
public sealed class ErrorContextBuilderTests : IDisposable {

    #region Constructors & Destructor

    public ErrorContextBuilderTests() {
        ErrorContextKey.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

    [Fact(DisplayName = "An error context builder cannot accept a null key.")]
    public void AnErrorContextBuilderCannotAcceptANullKey() {
        // Setup
        ErrorContextBuilder builder = new();

        // Exercise & verify
        Check.ThatCode(() => builder.Add<string>(null!, "x"))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "An error context builder allows method chaining.")]
    public void AnErrorContextBuilderAllowsMethodChaining() {
        // Setup
        ErrorContextBuilder     builder = new();
        ErrorContextKey<string> key     = ErrorContextKey.Create<string>("UserId");

        // Exercise
        ErrorContextBuilder returned = builder.Add(key, "u-123");

        // Verify
        Check.That(returned).IsSameReferenceAs(builder);
    }

    [Fact(DisplayName = "An error context builder stores the latest value for a key.")]
    public void AnErrorContextBuilderStoresTheLatestValueForAKey() {
        // Setup
        ErrorContextBuilder     builder = new();
        ErrorContextKey<string> key     = ErrorContextKey.Create<string>("UserId");
        builder.Add(key, "u-123");

        // Exercise
        builder.Add(key, "u-999");

        // Verify
        ErrorContext context = builder.Build();

        Check.That(context.IsEmpty).IsFalse();

        bool found = context.TryGet(key, out string? value);
        Check.That(found).IsTrue();
        Check.That(value).IsEqualTo("u-999");
    }

    [Fact(DisplayName = "An error context builder can store a null value.")]
    public void AnErrorContextBuilderCanStoreANullValue() {
        // Setup
        ErrorContextBuilder     builder = new();
        ErrorContextKey<string> key     = ErrorContextKey.Create<string>("OptionalInfo");

        // Exercise
        builder.Add(key, null);
        ErrorContext context = builder.Build();

        // Verify
        Check.That(context.IsEmpty).IsFalse();

        bool found = context.TryGet(key, out string? value);
        Check.That(found).IsFalse();
        Check.That(value).IsNull();
    }

    [Fact(DisplayName = "Building an error context creates a snapshot of the builder values.")]
    public void BuildingAnErrorContextCreatesASnapshotOfTheBuilderValues() {
        // Setup
        ErrorContextBuilder     builder   = new();
        ErrorContextKey<string> userIdKey = ErrorContextKey.Create<string>("UserId");
        ErrorContextKey<int>    otherKey  = ErrorContextKey.Create<int>("OtherKey");

        builder.Add(userIdKey, "u-123");
        ErrorContext first = builder.Build();

        // Exercise
        builder.Add(userIdKey, "u-999");
        builder.Add(otherKey, 42);

        // Verify
        bool foundInFirst = first.TryGet(userIdKey, out string? firstUserId);
        Check.That(foundInFirst).IsTrue();
        Check.That(firstUserId).IsEqualTo("u-123");

        bool foundOtherInFirst = first.TryGet(otherKey, out int _);
        Check.That(foundOtherInFirst).IsFalse();
    }

}