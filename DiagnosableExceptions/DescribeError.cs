namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Provides a fluent API for defining and documenting errors in a structured and detailed manner.
/// </summary>
public static class DescribeError {

    #region Static members

    /// <summary>
    ///     Specifies the title of the error being documented.
    /// </summary>
    /// <param name="title">The title of the error. Must not be <c>null</c>, empty, or consist only of whitespace.</param>
    /// <returns>The next stage in the error documentation building process, where the error explanation can be specified.</returns>
    /// <exception cref="System.ArgumentException">
    ///     Thrown when <paramref name="title" /> is <c>null</c>, empty, or consists only of whitespace.
    /// </exception>
    public static IErrorExplanationStage WithTitle(string title) {
        return new ErrorDocumentationBuilder().WithTitle(title);
    }

    #endregion

}