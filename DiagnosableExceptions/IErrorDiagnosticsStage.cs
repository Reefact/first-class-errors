namespace DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where diagnostics can be specified.
/// </summary>
public interface IErrorDiagnosticsStage {

    /// <summary>
    ///     Adds one or more documented diagnostic scenarios associated with this error.
    /// </summary>
    /// <param name="diagnostics">
    ///     A set of diagnostic entries describing plausible causes and analysis directions.
    /// </param>
    /// <remarks>
    ///     Diagnostics help readers reason about why the error may have occurred and where investigation should begin.
    /// </remarks>
    IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics);

    /// <summary>
    ///     Adds a documented diagnostic scenario describing a plausible cause of the error and a corresponding direction for
    ///     investigation.
    /// </summary>
    /// <param name="cause">
    ///     A concise description of a plausible state of the domain or system that could explain the error. This should
    ///     represent a meaningful situation (e.g., invalid input, inconsistent data, unexpected system behavior), not a
    ///     technical symptom or exception detail.
    /// </param>
    /// <param name="type">
    ///     A classification indicating whether the problem most likely originates from input data, internal system logic, or
    ///     both. This helps orient investigation and does not imply blame.
    /// </param>
    /// <param name="analysisLead">
    ///     A direction to explore when investigating this diagnostic scenario. This must guide <b>where to look</b>, not
    ///     <b>what to fix</b>. Prefer verbs such as "Verify", "Check", "Examine", or "Inspect". Avoid corrective instructions
    ///     (e.g., "Fix", "Correct", "Ensure") and references to specific tools or support processes.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         Diagnostics capture <b>diagnostic knowledge</b> about the error, helping readers reason about why it may have
    ///         occurred and where investigation should begin.
    ///     </para>
    ///     <para>
    ///         Each diagnostic represents one possible explanation. Multiple calls to this method can be used to document
    ///         alternative causes.
    ///     </para>
    /// </remarks>
    IErrorExamplesOrDiagnosticsStage WithDiagnostic(string cause, ErrorCauseType type, string analysisLead);

    /// <summary>
    ///     Explicitly indicates that no diagnostic scenarios are provided for this error.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this method when diagnostic scenarios would be unreliable, too speculative, or not useful for
    ///         investigation. It prevents authors from adding low-quality diagnostics just to fill the documentation.
    ///     </para>
    ///     <para>
    ///         Prefer <see cref="WithDiagnostics(ErrorDiagnostic[])" /> when you can express plausible causes and analysis
    ///         directions. Use <see cref="WithNoDiagnostic" /> only when the absence of diagnostics is intentional.
    ///     </para>
    /// </remarks>
    IErrorExamplesStage WithNoDiagnostic();

}