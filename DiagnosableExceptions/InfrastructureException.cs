namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents an application exception that originates from infrastructure concerns, such as databases, filesystems,
///     network calls, or external APIs.
/// </summary>
public class InfrastructureException : DiagnosableException {

    #region Constructors declarations

    protected InfrastructureException(string        errorCode,
                                      string        message,
                                      FlowDirection flowDirection,
                                      bool?         isTransient  = null,
                                      string?       remoteSystem = null,
                                      ErrorContext? context      = null)
        : base(errorCode, message, context) {
        FlowDirection = flowDirection;
        IsTransient   = isTransient;
        RemoteSystem  = remoteSystem;
    }

    protected InfrastructureException(string        errorCode,
                                      string        message,
                                      Exception     cause,
                                      FlowDirection flowDirection,
                                      bool?         isTransient  = null,
                                      string?       remoteSystem = null,
                                      ErrorContext? context      = null)
        : base(errorCode, message, cause, context) {
        FlowDirection = flowDirection;
        IsTransient   = isTransient;
        RemoteSystem  = remoteSystem;
    }

    protected InfrastructureException(string                 errorCode,
                                      string                 message,
                                      IEnumerable<Exception> causes,
                                      FlowDirection          flowDirection,
                                      bool?                  isTransient  = null,
                                      string?                remoteSystem = null,
                                      ErrorContext?          context      = null)
        : base(errorCode, message, causes, context) {
        FlowDirection = flowDirection;
        IsTransient   = isTransient;
        RemoteSystem  = remoteSystem;
    }

    #endregion

    /// <summary>
    ///     Indicates whether this infrastructure error is considered transient.
    ///     <list type="bullet">
    ///         <item><c>true</c>: retry may succeed without intervention</item>
    ///         <item><c>false</c>: retry will not help</item>
    ///         <item><c>null</c>: unknown / not specified</item>
    ///     </list>
    /// </summary>
    public bool? IsTransient { get; }

    /// <summary>
    ///     Indicates whether the error occurred while processing input (Inbound)
    ///     or when interacting with an external system (Outbound).
    /// </summary>
    public FlowDirection FlowDirection { get; }

    /// <summary>
    ///     Optional logical name of the external system involved in the error.
    ///     Example: "SAP", "PSP-X", "ReportingDb".
    /// </summary>
    public string? RemoteSystem { get; }

}