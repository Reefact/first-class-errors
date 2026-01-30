namespace DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where examples of the error can be specified.
/// </summary>
public interface IErrorExamplesStage {

    /// <summary>
    ///     Specifies examples for the error being documented.
    /// </summary>
    /// <typeparam name="TException">
    ///     The type of exception for which the documentation is being created. This type must inherit from
    ///     <see cref="DiagnosableException" />.
    /// </typeparam>
    /// <param name="exampleFactories">
    ///     An array of factory methods that generate instances of <typeparamref name="TException" />. Each instance represents
    ///     a specific example of the error.
    /// </param>
    /// <returns>
    ///     The complete <see cref="ErrorDocumentation" /> that contains all the error details.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="exampleFactories" /> is <c>null</c>.
    /// </exception>
    ErrorDocumentation WithExamples<TException>(params Func<TException>[] exampleFactories)
        where TException : DiagnosableException;

}