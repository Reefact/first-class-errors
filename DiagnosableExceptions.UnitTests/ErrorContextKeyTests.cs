#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

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

    [Theory(DisplayName = "Registering a key with a null or blank name is rejected.")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RegisteringKeyWithBlankNameIsRejected(string? name) {
        // Exercise & verify
        Check.ThatCode(() => ErrorContextKey.Create<string>(name!))
             .Throws<ArgumentException>()
             .WithMessage("Value cannot be null or whitespace. (Parameter 'name')");
    }

    [Fact(DisplayName = "Registering a key with a duplicate name is rejected.")]
    public void RegisteringKeyWithDuplicateNameIsRejected() {
        // Setup
        const string name = "Duplicate";
        ErrorContextKey.Create<string>(name);

        // Exercise & verify
        Check.ThatCode(() => ErrorContextKey.Create<int>(name))
             .Throws<InvalidOperationException>()
             .WithMessage("An error context key 'Duplicate' has already been registered.");
    }

    [Fact(DisplayName = "Registering a key with the same name but a different type is rejected.")]
    public void RegisteringKeyWithSameNameButDifferentTypeIsRejected() {
        // Setup
        const string name = "SameName";
        ErrorContextKey.Create<string>(name);

        // Exercise & verify
        Check.ThatCode(() => ErrorContextKey.Create<Guid>(name))
             .Throws<InvalidOperationException>();
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

        int successes = 0;
        int failures  = 0;

        // Exercise
        Parallel.For(0, 20, _ => {
            try {
                ErrorContextKey.Create<string>(name);
                Interlocked.Increment(ref successes);
            } catch (InvalidOperationException) {
                Interlocked.Increment(ref failures);
            }
        });

        // Verify
        Check.That(successes).IsEqualTo(1);
        Check.That(failures).IsEqualTo(19);
    }

    [SuppressMessage("Usage", "CA1816",
                     Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

}