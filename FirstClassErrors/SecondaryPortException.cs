namespace FirstClassErrors;

/// <summary>
///     Represents an exception that occurs due to a <see cref="SecondaryPortError" /> in the infrastructure layer.
/// </summary>
/// <remarks>
///     This exception is specifically designed to handle errors related to secondary ports and extends the
///     <see cref="InfrastructureException" /> class.
/// </remarks>
public class SecondaryPortException : InfrastructureException {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="SecondaryPortException" /> class with the specified
    ///     <see cref="SecondaryPortError" />.
    /// </summary>
    /// <param name="error">The <see cref="SecondaryPortError" /> that describes the error.</param>
    public SecondaryPortException(SecondaryPortError error) : base(error) { }

    #endregion

}