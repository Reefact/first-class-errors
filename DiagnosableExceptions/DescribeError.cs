namespace DiagnosableExceptions;

/// <summary>
///     Entry point for describing a domain or system error using a fluent, declarative DSL.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="DescribeError" /> is not a factory for exceptions and does not perform runtime logic. It provides a
///         structured language for documenting the meaning, rules, and diagnostic knowledge associated with an error.
///     </para>
///     <para>
///         This DSL is intended to capture the <b>semantic understanding</b> of an error, not its technical mechanics. It
///         helps humans understand:
///     </para>
///     <list type="bullet">
///         <item>What the error represents</item>
///         <item>Which rule or expectation was violated</item>
///         <item>Which scenarios may have led to it</item>
///         <item>Concrete situations where it occurs</item>
///     </list>
///     <para>
///         Error descriptions created with this DSL become part of the system’s shared knowledge about failures, making
///         errors understandable beyond raw exception data.
///     </para>
///     <para>
///         Each step in the fluent chain adds a different aspect of meaning to the error. The resulting description should
///         be readable as a structured explanation, not as technical configuration.
///     </para>
/// </remarks>
public static class DescribeError {

    #region Statics members declarations

    /// <summary>
    ///     Begins the description of an error by defining its human-readable title.
    /// </summary>
    /// <param name="title">
    ///     A short, clear name identifying the error condition.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         The title should express <b>what the error is</b>, not how it was detected. It is intended for human readers
    ///         and should remain meaningful outside the original codebase.
    ///     </para>
    ///     <para>
    ///         <b>Good examples:</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>"Temperature below absolute zero"</item>
    ///         <item>"Transaction date outside statement period"</item>
    ///     </list>
    ///     <para>
    ///         Titles should be concise and domain-oriented.
    ///     </para>
    /// </remarks>
    public static IErrorDescriptionStage WithTitle(string title) {
        return new ErrorDocumentationBuilder().WithTitle(title);
    }

    #endregion

}