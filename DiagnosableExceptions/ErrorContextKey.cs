#region Usings declarations

using System.Diagnostics;

#endregion

namespace DiagnosableExceptions;

/// <summary>
///     Represents a unique, named key used to attach structured diagnostic data to a <see cref="DiagnosableException" />.
/// </summary>
/// <remarks>
///     <para>
///         An <see cref="ErrorContextKey" /> defines the identity of a piece of contextual information associated with an
///         error (for example: <c>DealId</c>, <c>UserId</c>, or <c>CorrelationId</c>). Each key is globally unique by its
///         <see cref="Name" /> and is registered once at application startup.
///     </para>
///     <para>
///         Keys are strongly typed through the generic subclass <see cref="ErrorContextKey{T}" />, which specifies the
///         expected value type associated with the key.
///     </para>
///     <para>
///         Instances are immutable and compared by their <see cref="Name" />, ensuring consistent behavior across modules,
///         services, and serialization boundaries.
///     </para>
///     <para>
///         Keys are typically declared as <c>static readonly</c> fields to form a shared vocabulary of diagnostic context
///         within a system.
///     </para>
/// </remarks>
/// <example>
///     Declaring a key:
///     <code>public static readonly ErrorContextKey&lt;string&gt; DealId = ErrorContextKey.Create&lt;string&gt;("DealId", "Business identifier of the deal.");</code>
///     Using the key when throwing an exception:
///     <code>throw new DiagnosableException("Deal not found", ctx => ctx.Add(ErrorContextKeys.DealId, dealId));</code>
/// </example>
[DebuggerDisplay("{Name}")]
public abstract class ErrorContextKey : IEquatable<ErrorContextKey> {

    #region Static members

    private static readonly Dictionary<string, ErrorContextKey> Registered = new(StringComparer.Ordinal);
    private static readonly object                              Lock       = new();

    public static ErrorContextKey<T> Create<T>(string name, string? description = null) {
        if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)); }

        lock (Lock) {
            if (Registered.ContainsKey(name)) { throw new InvalidOperationException($"An error context key '{name}' has already been registered."); }

            ErrorContextKey<T> instance = new(name, description);
            Registered.Add(name, instance);

            return instance;
        }
    }

    public static IReadOnlyCollection<ErrorContextKey> GetRegisteredKeys() {
        lock (Lock) {
            return Registered.Values.ToArray();
        }
    }

    /// <summary>
    ///     Resets the internal state of registered <see cref="ErrorContextKey" /> instances.
    /// </summary>
    /// <remarks>
    ///     This method is intended for use in testing scenarios only. It clears all registered keys,
    ///     allowing a clean slate for subsequent tests that rely on <see cref="ErrorContextKey" /> registration.
    /// </remarks>
    internal static void ResetForTests() {
        lock (Lock) {
            Registered.Clear();
        }
    }

    #endregion

    public static bool operator ==(ErrorContextKey? left, ErrorContextKey? right) {
        return Equals(left, right);
    }

    public static bool operator !=(ErrorContextKey? left, ErrorContextKey? right) {
        return !Equals(left, right);
    }

    #region Constructors & Destructor

    private protected ErrorContextKey(string name, string? description, Type valueType) {
        Name        = name;
        Description = description;
        ValueType   = valueType;
    }

    #endregion

    /// <summary>
    ///     Gets the unique name that identifies this context key.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The name defines the identity of the key and must be unique within the application. Two keys with the same name
    ///         cannot be registered, regardless of their value type.
    ///     </para>
    ///     <para>
    ///         The name is used for equality comparison, serialization, logging, and documentation. It should be stable and
    ///         represent a meaningful domain concept (for example: <c>DealId</c>, <c>UserId</c>, or <c>CorrelationId</c>).
    ///     </para>
    /// </remarks>
    public string Name { get; }
    /// <summary>
    ///     Gets an optional human-readable description of the context key.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The description provides additional semantic information about the purpose and expected meaning of the key. It
    ///         is intended for documentation, debugging, and observability tools.
    ///     </para>
    ///     <para>
    ///         This value does not affect the identity or behavior of the key and may be null if the name is considered
    ///         self-explanatory.
    ///     </para>
    /// </remarks>
    public string? Description { get; }
    /// <summary>
    ///     Gets the type of value that is expected to be associated with this context key.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property reflects the generic type parameter of <see cref="ErrorContextKey{T}" />  and is used for
    ///         diagnostics, validation, and documentation purposes.
    ///     </para>
    ///     <para>
    ///         The value type does not participate in key identity: two keys with the same <see cref="Name" /> cannot coexist
    ///         even if their value types differ.
    ///     </para>
    /// </remarks>
    public Type ValueType { get; }

    public bool Equals(ErrorContextKey? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return Name == other.Name;
    }

    public override bool Equals(object? obj) {
        return obj is ErrorContextKey other && Equals(other);
    }

    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(Name);
    }

    public override string ToString() {
        return Name;
    }

}

/// <summary>
///     Represents a strongly typed diagnostic context key associated with values of type <typeparamref name="T" />.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ErrorContextKey{T}" /> extends <see cref="ErrorContextKey" /> by defining the expected value type
///         that can be associated with the key in an error context. This enables compile-time safety when adding
///         contextual information to a <see cref="DiagnosableException" />.
///     </para>
///     <para>
///         The generic type parameter <typeparamref name="T" /> does not affect the identity of the key, which is
///         determined solely by its name. Two keys with the same name cannot be registered, regardless of their type
///         parameter.
///     </para>
///     <para>
///         Instances are created via <see cref="ErrorContextKey.Create{T}(string, string?)" /> and are immutable once
///         registered.
///     </para>
/// </remarks>
/// <typeparam name="T">
///     The type of value that can be associated with this key in an error context.
/// </typeparam>
/// <example>
///     Declaring a typed key:
///     <code>public static readonly ErrorContextKey&lt;Guid&gt; CorrelationId = ErrorContextKey.Create&lt;Guid&gt;("CorrelationId", "Identifier used to correlate operations across services.");</code>
///     Adding a value to an exception context:
///     <code>ctx.Add(ErrorContextKeys.CorrelationId, correlationId);</code>
/// </example>
public sealed class ErrorContextKey<T> : ErrorContextKey {

    #region Constructors & Destructor

    internal ErrorContextKey(string name, string? description) : base(name, description, typeof(T)) { }

    #endregion

}