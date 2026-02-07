namespace DiagnosableExceptions;

/// <summary>
///     Represents the stage in the error documentation building process where a business rule can be specified.
/// </summary>
/// <remarks>
///     This interface is part of a fluent API for constructing error documentation.
/// </remarks>
public interface IErrorRuleStage {

    /// <summary>
    ///     Specifies the rule or expectation that was violated.
    /// </summary>
    /// <param name="rule">
    ///     A statement expressing the rule, constraint, or condition that must hold true.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This rule expresses the normative condition that the system expected to be satisfied. It may be a domain rule,
    ///         validation rule, regulatory requirement, or consistency constraint.
    ///     </para>
    /// </remarks>
    IErrorDiagnosticsStage WithRule(string rule);

    /// <summary>
    ///     Explicitly indicates that no single rule statement is applicable or meaningful for this error.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this method when providing a rule would be misleading, overly speculative.
    ///     </para>
    ///     <para>
    ///         Prefer <see cref="WithRule(string)" /> whenever you can express a clear rule, constraint, or expectation. Use
    ///         <see cref="WithNoRule" /> only when the absence of a rule is intentional and communicates something meaningful
    ///         to readers.
    ///     </para>
    /// </remarks>
    IErrorDiagnosticsStage WithNoRule();

}