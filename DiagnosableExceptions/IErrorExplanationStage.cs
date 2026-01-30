namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where the explanation of the error can be
///     specified.
/// </summary>
/// <remarks>
///     This interface is part of a fluent API for constructing error documentation.
/// </remarks>
public interface IErrorExplanationStage {

    /// <summary>
    ///     Specifies a detailed explanation for the error being documented.
    /// </summary>
    /// <param name="explanation">
    ///     A string providing additional context or clarification about the error.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="IErrorBusinessRuleStage" /> to continue the fluent API for constructing error
    ///     documentation.
    /// </returns>
    IErrorBusinessRuleStage WithExplanation(string explanation);

}