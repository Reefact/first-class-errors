namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where diagnostics can be specified.
/// </summary>
public interface IErrorDiagnosticsStage {

    /// <summary>
    ///     Specifies diagnostics for the error being documented.
    /// </summary>
    /// <param name="diagnostics">
    ///     An array of <see cref="ErrorDiagnostic" /> objects representing the causes and corrective actions
    ///     associated with the error.
    /// </param>
    /// <returns>
    ///     The next stage in the error documentation building process, allowing examples to be specified.
    /// </returns>
    IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics);

    /// <summary>
    ///     Specifies that no diagnostics are associated with the error being documented.
    /// </summary>
    /// <returns>
    ///     The next stage in the error documentation building process, allowing examples to be specified.
    /// </returns>
    IErrorExamplesStage WithNoDiagnostic();

}