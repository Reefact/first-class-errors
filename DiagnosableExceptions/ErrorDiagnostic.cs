namespace DiagnosableExceptions;

/// <summary>
///     Represents a documented diagnostic scenario associated with an error.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ErrorDiagnostic" /> does not describe the technical failure itself. Instead, it documents a
///         <b>plausible cause</b> behind the observed error and provides a corresponding <b>analysis lead</b> to guide
///         investigation. It exists to bridge the gap between a technical error and human understanding, helping support
///         teams, developers, and operators reason about what may have happened and where to start looking.
///     </para>
///     <para>
///         <b>An <see cref="ErrorDiagnostic" /> must never:</b>
///     </para>
///     <list type="bullet">
///         <item>Describe the exception mechanics (stack traces, null references, etc.)</item>
///         <item>Provide a technical fix or correction procedure</item>
///         <item>Depend on specific support processes, tools, or organizational workflows</item>
///     </list>
///     <para>
///         Instead, each diagnostic expresses:
///     </para>
///     <list type="bullet">
///         <item>A <see cref="Cause" /> — a plausible state of the world or system that could explain the error</item>
///         <item>A <see cref="Type" /> — a classification of the origin of the problem</item>
///         <item>An <see cref="AnalysisLead" /> — a direction to explore during investigation</item>
///     </list>
///     <para>
///         This structure ensures that errors remain understandable and actionable
///         even outside the original development context.
///     </para>
/// </remarks>
public sealed class ErrorDiagnostic {

    #region Constructors & Destructor

    /// <summary>
    ///     Initializes a new fully defined instance of the <see cref="ErrorDiagnostic" /> class.
    /// </summary>
    /// <param name="cause">
    ///     Describes a plausible state of the domain or system that could explain the error. This must represent a meaningful
    ///     situation (e.g., invalid input, inconsistent data, unexpected system behavior), not a technical symptom or
    ///     exception detail.
    /// </param>
    /// <param name="type">
    ///     Classifies where the problem most likely originates (input data, internal system logic, or both). This helps orient
    ///     investigation and should reflect the most probable source, not responsibility or blame.
    /// </param>
    /// <param name="analysisLead">
    ///     Indicates a direction to explore during investigation. This must guide <b>where to look</b>, not <b>what to fix</b>
    ///     . Use verbs such as "Verify", "Check", "Examine", or "Inspect". Avoid corrective instructions (e.g., "Fix",
    ///     "Correct", "Ensure", "Convert") and avoid references to specific tools or support procedures.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor enforces that an <see cref="ErrorDiagnostic" /> is always created as a
    ///         <b>complete diagnostic unit</b>. A diagnostic without a cause, origin classification, or analysis direction
    ///         would not provide meaningful guidance and is therefore not valid.
    ///     </para>
    ///     <para>
    ///         The three parameters represent complementary aspects of diagnostic knowledge:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><b>Cause</b> — what may have happened</item>
    ///         <item><b>Type</b> — where the issue most likely originates</item>
    ///         <item><b>Analysis lead</b> — where to look first during investigation</item>
    ///     </list>
    ///     <para>
    ///         <b>Authoring guidance:</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             The <paramref name="cause" /> should describe a plausible situation in the domain or system, not a
    ///             technical symptom.
    ///         </item>
    ///         <item>
    ///             The <paramref name="analysisLead" /> must indicate a direction of analysis (e.g., "Verify", "Check",
    ///             "Examine") and must not describe a fix or corrective action.
    ///         </item>
    ///         <item>Text should remain understandable without knowledge of the internal codebase.</item>
    ///     </list>
    ///     <para>
    ///         Keep in mind that diagnostics are meant to help humans reason about errors beyond the raw exception
    ///         information.
    ///     </para>
    /// </remarks>
    public ErrorDiagnostic(string cause, ErrorCauseType type, string analysisLead) {
        if (cause is null) { throw new ArgumentNullException(nameof(cause)); }
        if (analysisLead is null) { throw new ArgumentNullException(nameof(analysisLead)); }
        if (string.IsNullOrWhiteSpace(cause)) { throw new ArgumentException("Value cannot be empty or whitespace.", nameof(cause)); }
        if (string.IsNullOrWhiteSpace(analysisLead)) { throw new ArgumentException("Value cannot be empty or whitespace.", nameof(analysisLead)); }

        Cause        = cause.Trim();
        Type         = type;
        AnalysisLead = analysisLead.Trim();
    }

    #endregion

    /// <summary>
    ///     Describes a plausible cause that could have led to the error.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The cause should be expressed as a <b>state of the system or domain</b>,
    ///         not as a technical symptom.
    ///     </para>
    ///     <para>
    ///         <b>Good examples:</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>"The value received from an external system is invalid."</item>
    ///         <item>"The statement period does not match the transaction coverage."</item>
    ///     </list>
    ///     <para>
    ///         <b>Avoid:</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Stack trace descriptions</item>
    ///         <item>Low-level implementation details</item>
    ///         <item>Blame-oriented language</item>
    ///     </list>
    ///     <para>
    ///         A cause should remain meaningful to someone who does not know the internal codebase.
    ///     </para>
    /// </remarks>
    public string Cause { get; }

    /// <summary>
    ///     Classifies where the problem most likely originates.
    /// </summary>
    /// <remarks>
    ///     This helps guide initial investigation by indicating whether the issue is more likely related to external data,
    ///     internal logic, or both.
    /// </remarks>
    public ErrorCauseType Type { get; }

    /// <summary>
    ///     Provides a direction to explore when investigating this cause.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         An analysis lead indicates <b>where to look</b>, not <b>what to fix</b>. It should guide understanding without
    ///         prescribing corrective action.
    ///     </para>
    ///     <para>
    ///         <b>Use verbs such as:</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Verify</item>
    ///         <item>Check</item>
    ///         <item>Examine</item>
    ///         <item>Inspect</item>
    ///     </list>
    ///     <para>
    ///         <b>Avoid:</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>"Fix", "Correct", "Ensure", "Convert"</item>
    ///         <item>Step-by-step procedures</item>
    ///         <item>References to support tools or workflows</item>
    ///     </list>
    ///     <para>
    ///         The analysis lead should remain valid even if organizational processes change.
    ///     </para>
    /// </remarks>
    public string AnalysisLead { get; }

}