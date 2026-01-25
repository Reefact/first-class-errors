namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Specifies example values used to generate error messages through exception factory methods.
/// </summary>
/// <remarks>
///     This attribute is applied to factory methods for exceptions to document example inputs that could lead to specific
///     errors.
///     It supports multiple usages on the same method to provide examples for different scenarios.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ErrorExampleAttribute : Attribute {

    #region Constructors & Destructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorExampleAttribute" /> class with the specified example values.
    /// </summary>
    /// <param name="values">
    ///     The example values that demonstrate scenarios leading to specific errors.
    ///     Each object in the array corresponds to the value (in order) of the arguments in the exception factory method.
    ///     These values are used for documentation purposes in exception factory methods.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="values" /> is <c>null</c>.
    /// </exception>
    public ErrorExampleAttribute(params object[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        Values = values;
    }

    #endregion

    /// <summary>
    ///     Gets the example values that demonstrate scenarios leading to specific errors.
    /// </summary>
    /// <remarks>
    ///     Each object in the array corresponds to the value (in order) of the arguments in the exception factory method.
    ///     These values are used for documentation purposes to illustrate potential error scenarios.
    /// </remarks>
    public object[] Values { get; }

}