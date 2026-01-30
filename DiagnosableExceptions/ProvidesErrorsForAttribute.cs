namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Specifies the type that an exception class is associated with.
/// </summary>
/// <remarks>
///     This attribute is used to establish a relationship between an exception class and a specific type,
///     typically to indicate that the exception is related to operations or rules concerning that type.
/// </remarks>
/// <example>
///     The following example demonstrates how to use the <see cref="ProvidesErrorsForAttribute" />:
///     <code>
/// [ExceptionOf(typeof(Temperature))]
/// public class InvalidTemperatureException : DomainException {
///     // Exception implementation
/// }
/// </code>
///     In this example, the <c>InvalidTemperatureException</c> is associated with the <c>Temperature</c> type.
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProvidesErrorsForAttribute : Attribute {

    #region Constructors & Destructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProvidesErrorsForAttribute" /> class with the specified owner type.
    /// </summary>
    /// <param name="ownerType">
    ///     The type that the exception class is associated with. This parameter cannot be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="ownerType" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This constructor establishes the relationship between an exception class and a specific type,
    ///     typically to indicate that the exception is related to operations or rules concerning that type.
    /// </remarks>
    public ProvidesErrorsForAttribute(Type ownerType) {
        if (ownerType is null) { throw new ArgumentNullException(nameof(ownerType)); }

        OwnerType = ownerType;
    }

    #endregion

    /// <summary>
    ///     Gets the type that the exception class is associated with.
    /// </summary>
    /// <value>
    ///     The type that the exception class is related to. This property is set during the initialization
    ///     of the <see cref="ProvidesErrorsForAttribute" /> and cannot be <c>null</c>.
    /// </value>
    /// <remarks>
    ///     This property provides access to the type that is associated with the exception class,
    ///     typically used to indicate that the exception is related to operations or rules concerning that type.
    /// </remarks>
    public Type OwnerType { get; }

}