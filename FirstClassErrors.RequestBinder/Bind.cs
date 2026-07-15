namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The entry point of request binding: converts an incoming request DTO into a typed command or query of value
///     objects at the primary-adapter boundary, collecting <b>every</b> failure — instead of stopping at the first —
///     into a single coded <see cref="PrimaryPortError" /> tree.
/// </summary>
/// <example>
///     <code>
///     var bind = Bind.PropertiesOf(request).FailWith(InvalidBookingCommandError.Invalid);
///
///     var email = bind.SimpleProperty(r =&gt; r.GuestEmail).AsRequired(EmailAddress.Parse);
///     var stay  = bind.ComplexProperty(r =&gt; r.Stay).FailWith(InvalidStayError.Invalid).AsRequired(BindStay);
///
///     Outcome&lt;PlaceBookingCommand&gt; command =
///         bind.Build(() =&gt; new PlaceBookingCommand(email.Value, stay.Value));
///     </code>
/// </example>
public static class Bind {

    #region Statics members declarations

    /// <summary>
    ///     Starts binding the properties of a request DTO. Declare the failure envelope next, with
    ///     <see cref="RequestBinderEnvelopeStage{TRequest}.FailWith" />.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request DTO.</typeparam>
    /// <param name="request">The request DTO to bind.</param>
    /// <returns>The stage on which the failure envelope is declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is <c>null</c>.</exception>
    public static RequestBinderEnvelopeStage<TRequest> PropertiesOf<TRequest>(TRequest request) {
        if (request is null) { throw new ArgumentNullException(nameof(request)); }

        return new RequestBinderEnvelopeStage<TRequest>(request);
    }

    #endregion

}
