namespace DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where the error title can be specified.
/// </summary>
/// <remarks>
///     This interface is part of a fluent API for constructing error documentation.
/// </remarks>
public interface IErrorTitleStage {

    /// <summary>
    ///     Specifies the title of the error being documented.
    /// </summary>
    /// <param name="title">The title of the error. Must not be <c>null</c>, empty, or consist only of whitespace.</param>
    /// <returns>The next stage in the error documentation building process, where the error explanation can be specified.</returns>
    /// <exception cref="System.ArgumentException">
    ///     Thrown when <paramref name="title" /> is <c>null</c>, empty, or consists only of whitespace.
    /// </exception>
    IErrorDescriptionStage WithTitle(string title);

}