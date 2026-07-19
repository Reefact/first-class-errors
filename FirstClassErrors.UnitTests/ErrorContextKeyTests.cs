#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(ErrorContextKey))]
public class ErrorContextKeyTests : IDisposable {

    #region Constructors & Destructor

    public ErrorContextKeyTests() {
        ErrorContextKey.ResetForTests();
    }

    #endregion

    [Fact(DisplayName = "A registered key matches its declaration.")]
    public void RegisteredKeyMatchesItsDeclaration() {
        // Setup
        const string name = "DealId";

        // Exercise
        ErrorContextKey<string> key = ErrorContextKey.Create<string>(name);

        // Verify
        Check.That(key).IsNotNull();
        Check.That(key.Name).IsEqualTo(name);
        Check.That(key.ValueType).IsEqualTo(typeof(string));
    }

    [Fact(DisplayName = "Registering a key with a description stores the description.")]
    public void RegisteringKeyWithDescriptionStoresTheDescription() {
        // Setup
        const string name        = "UserId";
        const string description = "Identifier of the user.";

        // Exercise
        ErrorContextKey<int> key = ErrorContextKey.Create<int>(name, description);

        // Verify
        Check.That(key.Description).IsEqualTo(description);
    }

    [Fact(DisplayName = "A key's description provider is resolved on each read (not cached).")]
    public void AKeyDescriptionProviderIsResolvedOnEachRead() {
        // Setup: a provider whose result changes, proving the description follows the current state (e.g. the current
        // UI culture) rather than being frozen at creation.
        string                  current = "first";
        ErrorContextKey<string> key     = ErrorContextKey.Create<string>("LazyDesc", () => current);

        // Verify
        Check.That(key.Description).IsEqualTo("first");

        current = "second";
        Check.That(key.Description).IsEqualTo("second");
    }

    [Fact(DisplayName = "A key cannot be created with a null description provider.")]
    public void AKeyCannotBeCreatedWithANullDescriptionProvider() {
        // Exercise & verify
        Check.ThatCode(() => ErrorContextKey.Create<string>("K", (Func<string?>)null!))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "Registering a key with a null or blank name is rejected.")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RegisteringKeyWithBlankNameIsRejected(string? name) {
        // Exercise
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ErrorContextKey.Create<string>(name!));

        // Verify the contract, not the framework-specific parameter-suffix formatting of
        // ArgumentException.Message (same net472-floor rationale as ErrorCodeTests).
        Check.That(exception.Message).Contains("Value cannot be null or whitespace.");
        Check.That(exception.ParamName).IsEqualTo("name");
    }

    [Fact(DisplayName = "Re-declaring a key with the same name and type returns the registered instance.")]
    public void RedeclaringKeyWithSameNameAndTypeReturnsTheRegisteredInstance() {
        // Setup
        const string            name  = "Duplicate";
        ErrorContextKey<string> first = ErrorContextKey.Create<string>(name);

        // Exercise
        ErrorContextKey<string> second = ErrorContextKey.Create<string>(name);

        // Verify
        Check.That(second).IsSameReferenceAs(first);
        Check.That(ErrorContextKey.GetRegisteredKeys().Count).IsEqualTo(1);
    }

    [Fact(DisplayName = "Re-declaring a key with a different description keeps the first registered description.")]
    public void RedeclaringKeyWithDifferentDescriptionKeepsTheFirstRegisteredDescription() {
        // Setup
        const string name = "DescribedTwice";
        ErrorContextKey.Create<string>(name, "first");

        // Exercise
        ErrorContextKey<string> second = ErrorContextKey.Create<string>(name, "second");

        // Verify
        Check.That(second.Description).IsEqualTo("first");
    }

    [Fact(DisplayName = "Registering a key with the same name but a different type is rejected.")]
    public void RegisteringKeyWithSameNameButDifferentTypeIsRejected() {
        // Setup
        const string name = "SameName";
        ErrorContextKey.Create<string>(name);

        // Exercise & verify
        Check.ThatCode(() => ErrorContextKey.Create<Guid>(name))
             .Throws<InvalidOperationException>()
             .WithMessage("An error context key 'SameName' is already registered with value type 'System.String'; it cannot be re-registered with value type 'System.Guid'.");
    }

    [Fact(DisplayName = "The registry returns all registered keys.")]
    public void RegistryReturnsAllRegisteredKeys() {
        // Setup
        ErrorContextKey<string> key1 = ErrorContextKey.Create<string>("K1");
        ErrorContextKey<int>    key2 = ErrorContextKey.Create<int>("K2");

        // Exercise
        IReadOnlyCollection<ErrorContextKey> registered = ErrorContextKey.GetRegisteredKeys();

        // Verify
        Check.That(registered).Contains(key1);
        Check.That(registered).Contains(key2);
        Check.That(registered.Count).IsEqualTo(2);
    }

    [Fact(DisplayName = "Keys with the same name are considered equal.")]
    public void KeysWithSameNameAreEqual() {
        // Setup
        const string            name = "EqualKey";
        ErrorContextKey<string> key1 = ErrorContextKey.Create<string>(name);
        ErrorContextKey.ResetForTests();
        ErrorContextKey<int> key2 = ErrorContextKey.Create<int>(name);

        // Exercise & verify
        Check.That(key1).IsEqualTo(key2);
    }

    [Fact(DisplayName = "Keys with different names are not equal.")]
    public void KeysWithDifferentNamesAreNotEqual() {
        // Setup
        ErrorContextKey<string> key1 = ErrorContextKey.Create<string>("K1");
        ErrorContextKey.ResetForTests();
        ErrorContextKey<string> key2 = ErrorContextKey.Create<string>("K2");

        // Exercise & verify
        Check.That(key1).IsNotEqualTo(key2);
    }

    [Fact(DisplayName = "A key is not equal to null.")]
    public void KeyComparedToNullIsNotEqual() {
        // Setup
        ErrorContextKey<string>? key = ErrorContextKey.Create<string>("K");

        // Exercise
        bool result = key.Equals(null);

        // Verify
        Check.That(result).IsFalse();
    }

    [Fact(DisplayName = "A key is not equal to an object of another type.")]
    public void KeyComparedToObjectOfDifferentTypeIsNotEqual() {
        // Setup
        ErrorContextKey<string> key = ErrorContextKey.Create<string>("K");

        // Exercise
        // ReSharper disable once SuspiciousTypeConversion.Global
        bool result = key.Equals("K");

        // Verify
        Check.That(result).IsFalse();
    }

    [Fact(DisplayName = "Equality operator returns true for keys with the same name.")]
    public void EqualityOperatorReturnsTrueForKeysWithSameName() {
        // Setup
        const string            name = "OpEqual";
        ErrorContextKey<string> key1 = ErrorContextKey.Create<string>(name);
        ErrorContextKey.ResetForTests();
        ErrorContextKey<int> key2 = ErrorContextKey.Create<int>(name);

        // Exercise
        bool result = key1 == key2;

        // Verify
        Check.That(result).IsTrue();
    }

    [Fact(DisplayName = "Inequality operator returns true for keys with different names.")]
    public void InequalityOperatorReturnsTrueForKeysWithDifferentNames() {
        // Setup
        ErrorContextKey<string> key1 = ErrorContextKey.Create<string>("A");
        ErrorContextKey.ResetForTests();
        ErrorContextKey<string> key2 = ErrorContextKey.Create<string>("B");

        // Exercise
        bool result = key1 != key2;

        // Verify
        Check.That(result).IsTrue();
    }

    [Fact(DisplayName = "Keys with the same name produce the same hash code.")]
    public void KeysWithSameNameProduceTheSameHashCode() {
        // Setup
        const string            name = "Hash";
        ErrorContextKey<string> key1 = ErrorContextKey.Create<string>(name);
        ErrorContextKey.ResetForTests();
        ErrorContextKey<int> key2 = ErrorContextKey.Create<int>(name);

        // Exercise
        int hash1 = key1.GetHashCode();
        int hash2 = key2.GetHashCode();

        // Verify
        Check.That(hash1).IsEqualTo(hash2);
    }

    [Fact(DisplayName = "Keys with different names may produce different hash codes.")]
    public void KeysWithDifferentNamesMayProduceDifferentHashCodes() {
        // Setup
        ErrorContextKey<string> key1 = ErrorContextKey.Create<string>("A");
        ErrorContextKey.ResetForTests();
        ErrorContextKey<string> key2 = ErrorContextKey.Create<string>("B");

        // Exercise
        int hash1 = key1.GetHashCode();
        int hash2 = key2.GetHashCode();

        // Verify
        Check.That(hash1).IsNotEqualTo(hash2);
    }

    [Fact(DisplayName = "The string representation of a key is its name.")]
    public void KeyToStringReturnsItsName() {
        // Setup
        const string            name = "ToStringKey";
        ErrorContextKey<string> key  = ErrorContextKey.Create<string>(name);

        // Exercise
        string result = key.ToString();

        // Verify
        Check.That(result).IsEqualTo(name);
    }

    [Fact(DisplayName = "Concurrent registration of the same key name results in a single registration.")]
    public void ConcurrentRegistrationOfSameKeyNameResultsInSingleRegistration() {
        // Setup
        const string name = "Concurrent";

        ErrorContextKey<string>[] created = new ErrorContextKey<string>[20];

        // Exercise
        Parallel.For(0, 20, index => created[index] = ErrorContextKey.Create<string>(name));

        // Verify
        Check.That(created.All(key => ReferenceEquals(key, created[0]))).IsTrue();
        Check.That(ErrorContextKey.GetRegisteredKeys().Count).IsEqualTo(1);
    }

    [SuppressMessage("Usage", "CA1816",
                     Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

}