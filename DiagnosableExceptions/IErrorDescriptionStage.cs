namespace DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where the explanation of the error can be
///     specified.
/// </summary>
/// <remarks>
///     This interface is part of a fluent API for constructing error documentation.
/// </remarks>
public interface IErrorDescriptionStage {

    /// <summary>
    ///     Adds a human-readable description explaining what this error means.
    /// </summary>
    /// <param name="description">
    ///     A short narrative explaining the situation in which the error occurs.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This text should explain the <b>meaning</b> of the error, not its implementation details. It is meant to be
    ///         understood by someone who does not know the internal codebase.
    ///     </para>
    ///     <para>
    ///         For consistency across errors, you may start the description with:
    ///         <c>This error occurs when trying to (...)</c>. This is a recommended convention (not a requirement) that helps
    ///         keep descriptions uniform and easy to scan.
    ///     </para>
    ///     <para>
    ///         <b>Writing guidance:</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Prefer present tense and clear domain language.</item>
    ///         <item>Avoid stack traces, exception types, or low-level technical symptoms.</item>
    ///         <item>Focus on what the system was trying to do and why that situation is invalid.</item>
    ///     </list>
    /// </remarks>
    IErrorRuleStage WithDescription(string description);

}