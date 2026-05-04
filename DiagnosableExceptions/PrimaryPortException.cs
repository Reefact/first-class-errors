namespace DiagnosableExceptions;

/// <summary>
///     Represents an exception that occurs due to a <see cref="PrimaryPortError" /> in the infrastructure layer.
/// </summary>
/// <remarks>
///     This exception is specifically designed to handle errors related to primary ports and extends the
///     <see cref="InfrastructureException" /> class.
/// </remarks>
public class PrimaryPortException : InfrastructureException {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="PrimaryPortException" /> class with the specified
    ///     <see cref="PrimaryPortError" />.
    /// </summary>
    /// <param name="error">The <see cref="PrimaryPortError" /> that describes the error.</param>
    public PrimaryPortException(PrimaryPortError error) : base(error) { }

    #endregion

}