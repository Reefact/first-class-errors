namespace DiagnosableExceptions;

/// <summary>
///     Represents an infrastructure exception originating from a primary adapter, that is, a component through which the
///     outside world invokes the system.
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="PrimaryAdapterException" /> models technical failures occurring at the system boundary where
///         external actors interact with the application. These adapters typically expose the system to users or upstream
///         systems through interfaces such as HTTP APIs, message consumers, CLI tools, or UI layers.
///     </para>
///     <para>
///         This exception type is used to signal that an incoming request or interaction could not be processed due to a
///         technical issue in the entry-side infrastructure, rather than a domain rule violation.
///     </para>
///     <para>
///         It belongs to the infrastructure layer but specifically reflects failures on the <b>input side</b> of the
///         system (outside → inside).
///     </para>
///     <para>
///         <b>Typical scenarios include:</b>
///     </para>
///     <list type="bullet">
///         <item>Request parsing or deserialization failures</item>
///         <item>Protocol or format errors</item>
///         <item>Adapter-specific configuration issues</item>
///         <item>Inbound transport or gateway failures</item>
///     </list>
/// </remarks>
public abstract class PrimaryAdapterException : InfrastructureException {

    #region Constructors declarations

    /// <inheritdoc />
    protected PrimaryAdapterException(string  errorCode,
                                      string  errorMessage,
                                      string? shortMessage = null,
                                      bool?   isTransient  = null)
        : base(errorCode, errorMessage, shortMessage, isTransient) { }

    /// <inheritdoc />
    protected PrimaryAdapterException(string    errorCode,
                                      string    errorMessage,
                                      Exception innerException,
                                      string?   shortMessage = null,
                                      bool?     isTransient  = null)
        : base(errorCode, errorMessage, innerException, shortMessage, isTransient) { }

    /// <inheritdoc />
    protected PrimaryAdapterException(string                 errorCode,
                                      string                 errorMessage,
                                      IEnumerable<Exception> innerExceptions,
                                      string?                shortMessage = null,
                                      bool?                  isTransient  = null)
        : base(errorCode, errorMessage, innerExceptions, shortMessage, isTransient) { }

    #endregion

}