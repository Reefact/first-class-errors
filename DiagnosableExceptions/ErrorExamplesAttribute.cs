namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Specifies example values used to generate error messages through exception factory methods.
/// </summary>
/// <remarks>
///     This attribute is applied to factory methods for exceptions to document example inputs that could lead to specific
///     errors.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ErrorExamplesAttribute : Attribute {

    #region Constructors & Destructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorExamplesAttribute" /> class.
    /// </summary>
    /// <param name="exampleType">
    ///     The type that contains the example values used to illustrate error scenarios.
    /// </param>
    /// <param name="exampleMemberName">
    ///     The name of the member within the specified type that provides the example values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="exampleType" /> or <paramref name="exampleMemberName" /> is <c>null</c>.
    /// </exception>
    public ErrorExamplesAttribute(Type exampleType, string exampleMemberName) {
        if (exampleType is null) { throw new ArgumentNullException(nameof(exampleType)); }
        if (exampleMemberName is null) { throw new ArgumentNullException(nameof(exampleMemberName)); }

        ExampleType       = exampleType;
        ExampleMemberName = exampleMemberName;
    }

    #endregion

    /// <summary>
    ///     Gets the type that contains the example values used to illustrate error scenarios.
    /// </summary>
    /// <value>
    ///     A <see cref="Type" /> representing the class or structure that provides the example values.
    /// </value>
    /// <remarks>
    ///     This property is used to identify the type that holds the example data, which is referenced
    ///     to document potential error cases in exception factory methods.
    /// </remarks>
    public Type ExampleType { get; }
    /// <summary>
    ///     Gets the name of the member within the specified type that provides the example values.
    /// </summary>
    /// <value>
    ///     A <see cref="string" /> representing the name of the member that contains the example values used to illustrate
    ///     error scenarios.
    /// </value>
    /// <remarks>
    ///     This property is used to identify the specific member in the <see cref="ExampleType" /> that holds the example
    ///     data.
    /// </remarks>
    public string ExampleMemberName { get; }

}