namespace DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where a business rule can be specified.
/// </summary>
/// <remarks>
///     This interface is part of a fluent API for constructing error documentation.
/// </remarks>
public interface IErrorBusinessRuleStage {

    /// <summary>
    ///     Specifies the business rule associated with the error being documented.
    /// </summary>
    /// <param name="rule">
    ///     A string representing the business rule that provides additional context or clarification about the
    ///     error.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="IErrorDiagnosticsStage" /> that represents the next stage in the error documentation
    ///     building process.
    /// </returns>
    IErrorDiagnosticsStage WithBusinessRule(string rule);

    /// <summary>
    ///     Specifies that no business rule is associated with the error being documented.
    /// </summary>
    /// <returns>
    ///     An instance of <see cref="IErrorDiagnosticsStage" /> that represents the next stage in the error documentation
    ///     building process.
    /// </returns>
    IErrorDiagnosticsStage WithNoBusinessRule();

}