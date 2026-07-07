#region Usings declarations

using System.Diagnostics;

#endregion

namespace FirstClassErrors;

/// <summary>
///     Represents a unique, named key used to attach structured diagnostic data to a <see cref="DiagnosableException" />.
/// </summary>
/// <remarks>
///     <para>
///         An <see cref="ErrorContextKey" /> defines the identity of a piece of contextual information associated with an
///         error (for example: <c>DealId</c>, <c>UserId</c>, or <c>CorrelationId</c>). Each key is globally unique by its
///         <see cref="Name" /> and is registered once: re-declaring a key with the same name and value type returns the
///         registered instance, while a same-named key with a different value type is rejected.
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
///     Attaching the key to an error's context, then throwing it as a <see cref="DiagnosableException" />:
///     <code>
///     throw DomainError.Create(ErrorCode.Create("DEAL_NOT_FOUND"), "Deal not found", ctx => ctx.Add(DealId, dealId))
///                      .WithPublicMessage("The requested deal could not be found.")
///                      .ToException();
///     </code>
/// </example>
[DebuggerDisplay("{Name}")]
public abstract class ErrorContextKey : IEquatable<ErrorContextKey> {

    #region Statics members declarations

    private static readonly Dictionary<string, ErrorContextKey> Registered = new(StringComparer.Ordinal);
    private static readonly object                              Lock       = new();

    /// <summary>
    ///     Represents the key used to attach the exception responsible for the failure to construct the error context
    ///     to a <see cref="DiagnosableException" />.
    /// </summary>
    /// <remarks>
    ///     This key is used to provide additional diagnostic information when an error occurs during the construction
    ///     of an error context. The associated value is expected to be of type <see cref="Exception" />.
    /// </remarks>
    internal static readonly ErrorContextKey CannotBuildErrorContext = Create<Exception>("#CANNOT_BUILD_ERROR_CONTEXT", "The exception responsible for the failure to construct the error context.");

    /// <summary>
    ///     Represents the key that lists the mandatory messages (by parameter name) that were missing at error construction
    ///     and were replaced by a fallback sentinel.
    /// </summary>
    /// <remarks>
    ///     This key materializes, as queryable diagnostic data, a violation of the invariant that every error carries a
    ///     diagnostic and a short message. It is added during <see cref="Error" /> construction under the library's
    ///     "manufacturing an error never throws" doctrine. The associated value is an
    ///     <see cref="IReadOnlyList{T}" /> of <see cref="string" />.
    /// </remarks>
    internal static readonly ErrorContextKey MissingRequiredMessages = Create<IReadOnlyList<string>>("#MISSING_REQUIRED_MESSAGE", "The mandatory messages (by parameter name) that were missing and replaced by a fallback sentinel.");

    /// <summary>
    ///     Gets or creates the <see cref="ErrorContextKey{T}" /> with the specified name and optional description.
    /// </summary>
    /// <remarks>
    ///     Creation is idempotent: re-declaring a key with the same <paramref name="name" /> and the same
    ///     <typeparamref name="T" /> returns the already-registered instance (whose description wins), so a declaration
    ///     that is executed again — a reloaded plugin, a re-run test fixture — never fails. A same-named key with a
    ///     different value type is rejected: keys compare by name inside error contexts, so the two keys would silently
    ///     collide and make the stored value unreadable through the typed API.
    /// </remarks>
    /// <typeparam name="T">The type associated with the error context key.</typeparam>
    /// <param name="name">The unique name of the error context key. Must not be <c>null</c>, empty, or whitespace.</param>
    /// <param name="description">An optional description providing additional context for the error context key.</param>
    /// <returns>The registered <see cref="ErrorContextKey{T}" /> instance for <paramref name="name" />.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name" /> is <c>null</c>, empty, or consists only of
    ///     whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a key with the specified <paramref name="name" /> is
    ///     already registered with a different value type.
    /// </exception>
    public static ErrorContextKey<T> Create<T>(string name, string? description = null) {
        Func<string?>? descriptionProvider = null;
        if (description is not null) { descriptionProvider = () => description; }

        return Register<T>(name, descriptionProvider);
    }

    /// <summary>
    ///     Gets or creates the <see cref="ErrorContextKey{T}" /> whose description is resolved lazily, on each read of
    ///     <see cref="Description" />.
    /// </summary>
    /// <remarks>
    ///     Use this to supply a <b>localized</b> description — for example one read from a
    ///     <see cref="System.Resources.ResourceManager" /> under the current UI culture — so the same registered key
    ///     documents itself in whatever language is in effect when the documentation is extracted. A key is still
    ///     registered once by its <paramref name="name" />; only the description text is deferred. Re-declaring a key
    ///     with the same name and the same <typeparamref name="T" /> returns the already-registered instance (whose
    ///     description provider wins); a different value type is rejected.
    /// </remarks>
    /// <typeparam name="T">The type associated with the error context key.</typeparam>
    /// <param name="name">The unique name of the error context key. Must not be <c>null</c>, empty, or whitespace.</param>
    /// <param name="descriptionProvider">A function returning the description; invoked each time <see cref="Description" /> is read.</param>
    /// <returns>The registered <see cref="ErrorContextKey{T}" /> instance for <paramref name="name" />.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is <c>null</c>, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="descriptionProvider" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a key with the specified <paramref name="name" /> is already registered with a different value type.</exception>
    public static ErrorContextKey<T> Create<T>(string name, Func<string?> descriptionProvider) {
        if (descriptionProvider is null) { throw new ArgumentNullException(nameof(descriptionProvider)); }

        return Register<T>(name, descriptionProvider);
    }

    private static ErrorContextKey<T> Register<T>(string name, Func<string?>? descriptionProvider) {
        if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)); }

        lock (Lock) {
            if (Registered.TryGetValue(name, out ErrorContextKey? existing)) {
                if (existing.ValueType != typeof(T)) {
                    throw new InvalidOperationException(
                        $"An error context key '{name}' is already registered with value type '{existing.ValueType}'; it cannot be re-registered with value type '{typeof(T)}'.");
                }

                // Same name, same value type: the declaration is being re-executed (a reloaded plugin, a re-run test
                // fixture), not conflicting. The first registered instance is the canonical one — its description
                // (or provider) wins — and the cast is safe because ErrorContextKey<T> is the only subclass.
                return (ErrorContextKey<T>)existing;
            }

            ErrorContextKey<T> instance = new(name, descriptionProvider);
            Registered.Add(name, instance);

            return instance;
        }
    }

    /// <summary>
    ///     Retrieves a collection of all registered <see cref="ErrorContextKey" /> instances.
    /// </summary>
    /// <returns>
    ///     An <see cref="IReadOnlyCollection{T}" /> containing all registered <see cref="ErrorContextKey" /> objects.
    /// </returns>
    /// <remarks>
    ///     This method is thread-safe and ensures that the returned collection reflects the current state of the registry.
    /// </remarks>
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

    /// <summary>
    ///     Determines whether two <see cref="ErrorContextKey" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ErrorContextKey" /> to compare.</param>
    /// <param name="right">The second <see cref="ErrorContextKey" /> to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="ErrorContextKey" /> instances are equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(ErrorContextKey? left, ErrorContextKey? right) {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two specified <see cref="ErrorContextKey" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ErrorContextKey" /> to compare.</param>
    /// <param name="right">The second <see cref="ErrorContextKey" /> to compare.</param>
    /// <returns>
    ///     <c>true</c> if the two <see cref="ErrorContextKey" /> instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This operator uses the <see cref="object.Equals(object?)" /> method to determine inequality.
    /// </remarks>
    public static bool operator !=(ErrorContextKey? left, ErrorContextKey? right) {
        return !Equals(left, right);
    }

    #region Fields declarations

    private readonly Func<string?>? _descriptionProvider;

    #endregion

    #region Constructors declarations

    private protected ErrorContextKey(string name, Func<string?>? descriptionProvider, Type valueType) {
        Name                 = name;
        _descriptionProvider = descriptionProvider;
        ValueType            = valueType;
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
    ///         self-explanatory. When the key is created with a description provider (see
    ///         <see cref="Create{T}(string, Func{string})" />), it is resolved on each read, so a localized description
    ///         follows the current UI culture.
    ///     </para>
    /// </remarks>
    public string? Description => _descriptionProvider?.Invoke();
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

    /// <inheritdoc />
    public bool Equals(ErrorContextKey? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return Name == other.Name;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is ErrorContextKey other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(Name);
    }

    /// <inheritdoc />
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

    #region Constructors declarations

    internal ErrorContextKey(string name, Func<string?>? descriptionProvider) : base(name, descriptionProvider, typeof(T)) { }

    #endregion

}