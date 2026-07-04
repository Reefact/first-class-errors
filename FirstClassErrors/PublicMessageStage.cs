namespace FirstClassErrors;

/// <summary>
///     Intermediate stage of the error staged-builder returned by every concrete error's <c>Create(...)</c> factory.
/// </summary>
/// <typeparam name="TError">The concrete <see cref="Error" /> type being built.</typeparam>
/// <remarks>
///     <para>
///         A <see cref="PublicMessageStage{TError}" /> already holds the mandatory internal information of the error (its
///         <see cref="Error.Code" /> and <see cref="Error.DiagnosticMessage" />, plus any structural information such as
///         transience or inner errors). It is deliberately <b>not</b> an <see cref="Error" />: it cannot be used where an
///         <see cref="Error" /> (or any specialization) is expected, so an error can never be left without its public
///         messages.
///     </para>
///     <para>
///         Call <see cref="WithPublicMessage(string, string?)" /> to supply the public-facing messages and obtain the final
///         <typeparamref name="TError" /> instance. There is no separate <c>Build()</c> step.
///     </para>
/// </remarks>
public sealed class PublicMessageStage<TError>
    where TError : Error {

    #region Fields declarations

    private readonly Func<string, string?, TError> _finalize;

    #endregion

    #region Constructors declarations

    internal PublicMessageStage(Func<string, string?, TError> finalize) {
        _finalize = finalize;
    }

    #endregion

    /// <summary>
    ///     Supplies the public-facing messages and produces the final error instance.
    /// </summary>
    /// <param name="shortMessage">
    ///     The mandatory short public summary of the error, safe to surface to an end user or an API client. This value
    ///     cannot be null or whitespace.
    /// </param>
    /// <param name="detailedMessage">
    ///     An optional, controlled public detail. It may be exposed to a caller, but only when the application explicitly
    ///     chooses to. Defaults to <c>null</c>.
    /// </param>
    /// <returns>The finalized <typeparamref name="TError" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shortMessage" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="shortMessage" /> is empty or whitespace.</exception>
    public TError WithPublicMessage(string shortMessage, string? detailedMessage = null) {
        string  safeShortMessage    = Error.RequireMessage(shortMessage, nameof(shortMessage));
        string? safeDetailedMessage = Error.NormalizeOptionalMessage(detailedMessage);

        return _finalize(safeShortMessage, safeDetailedMessage);
    }

}
