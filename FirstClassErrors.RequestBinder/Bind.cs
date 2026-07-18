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
///         bind.New(s =&gt; new PlaceBookingCommand(s.Get(email), s.Get(stay)));
///     </code>
/// </example>
public static class Bind {

    #region Statics members declarations

    /// <summary>
    ///     Starts binding the properties of a request DTO with the default options (argument names are the C#
    ///     property names). Declare the failure envelope next, with
    ///     <see cref="RequestBinderEnvelopeStage{TRequest}.FailWith" />.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request DTO.</typeparam>
    /// <param name="request">The request DTO to bind.</param>
    /// <returns>The stage on which the failure envelope is declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is <c>null</c>.</exception>
    public static RequestBinderEnvelopeStage<TRequest> PropertiesOf<TRequest>(TRequest request) {
        if (request is null) { throw new ArgumentNullException(nameof(request)); }

        return new RequestBinderEnvelopeStage<TRequest>(request, RequestBinderOptions.Default);
    }

    /// <summary>
    ///     Fixes the binding options — for example a serializer-aware <see cref="IArgumentNameProvider" /> — before
    ///     any property is bound, then starts binding with <see cref="ConfiguredBind.PropertiesOf{TRequest}" />. The
    ///     options are set once here, so a binder's naming policy can never change mid-binding. The returned entry
    ///     point holds no per-request state: create it once (for example at application setup) and reuse it for every
    ///     request.
    /// </summary>
    /// <param name="options">The options every binding started from the returned entry point (and its nested binders) binds with.</param>
    /// <returns>An options-configured entry point offering <see cref="ConfiguredBind.PropertiesOf{TRequest}" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <c>null</c>.</exception>
    public static ConfiguredBind WithOptions(RequestBinderOptions options) {
        if (options is null) { throw new ArgumentNullException(nameof(options)); }

        return new ConfiguredBind(options);
    }

    #endregion

}
