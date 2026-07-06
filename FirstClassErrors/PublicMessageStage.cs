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
    ///     The mandatory short public summary of the error, safe to surface to an end user or an API client. Following the
    ///     library's "manufacturing an error never throws" doctrine, a <c>null</c> or whitespace value is not rejected: it is
    ///     replaced by <see cref="Error.MissingShortMessage" /> and the omission is recorded in the error context.
    /// </param>
    /// <param name="detailedMessage">
    ///     An optional, controlled public detail. It may be exposed to a caller, but only when the application explicitly
    ///     chooses to. Defaults to <c>null</c>.
    /// </param>
    /// <returns>The finalized <typeparamref name="TError" /> instance.</returns>
    public TError WithPublicMessage(string shortMessage, string? detailedMessage = null) {
        // The base Error constructor is the single place that normalizes messages (coalesces missing mandatory ones to a
        // documented sentinel and trims the rest), so the raw values are forwarded as-is here.
        return _finalize(shortMessage, detailedMessage);
    }

}
