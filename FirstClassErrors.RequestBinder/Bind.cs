namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The entry point of request binding: builds a typed command or query of value objects from an incoming request at
///     the primary-adapter boundary, collecting <b>every</b> failure — instead of stopping at the first — into a single
///     coded <see cref="PrimaryPortError" /> tree.
/// </summary>
/// <example>
///     <code>
///     var bind = Bind.Request(PlaceBookingError.Invalid);
///
///     var body  = bind.PropertiesOf(request);
///     var email = body.SimpleProperty(r =&gt; r.GuestEmail).AsRequired(EmailAddress.Parse);
///     var stay  = body.ComplexProperty(r =&gt; r.Stay).FailWith(InvalidStayError.Invalid).AsRequired(BindStay);
///     var id    = bind.Argument("bookingId").FromRoute(routeBookingId).AsRequired(BookingId.From);
///
///     Outcome&lt;PlaceBookingCommand&gt; command =
///         bind.New(s =&gt; new PlaceBookingCommand(s.Get(id), s.Get(email), s.Get(stay)));
///     </code>
/// </example>
public static class Bind {

    #region Statics members declarations

    /// <summary>
    ///     Starts binding a request with the default options (argument names are the C# property names), declaring the
    ///     failure envelope up front — the factory producing the single <see cref="PrimaryPortError" /> under which every
    ///     failure recorded during the binding is grouped, typically an aggregate factory of the application's error
    ///     catalog passed as a method group. Attach the inputs next, through
    ///     <see cref="RequestBinder.PropertiesOf{TDto}" /> and <see cref="RequestBinder.Argument" />, and assemble the
    ///     command or query with <see cref="RequestBinder.New{TCommand}" />.
    /// </summary>
    /// <param name="envelope">The envelope factory, receiving the collected failures.</param>
    /// <returns>The request binder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope" /> is <c>null</c>.</exception>
    public static RequestBinder Request(Func<PrimaryPortInnerErrors, PrimaryPortError> envelope) {
        if (envelope is null) { throw new ArgumentNullException(nameof(envelope)); }

        return new RequestBinder(new RequestBinding(envelope, RequestBinderOptions.Default, argumentPrefix: null));
    }

    /// <summary>
    ///     Fixes the binding options — for example a serializer-aware <see cref="IArgumentNameProvider" /> — before any
    ///     input is bound, then starts binding with <see cref="ConfiguredBind.Request" />. The options are set once here,
    ///     so a binder's naming policy can never change mid-binding. The returned entry point holds no per-request state:
    ///     create it once (for example at application setup) and reuse it for every request.
    /// </summary>
    /// <param name="options">The options every binding started from the returned entry point (and its nested binders) binds with.</param>
    /// <returns>An options-configured entry point offering <see cref="ConfiguredBind.Request" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <c>null</c>.</exception>
    public static ConfiguredBind WithOptions(RequestBinderOptions options) {
        if (options is null) { throw new ArgumentNullException(nameof(options)); }

        return new ConfiguredBind(options);
    }

    #endregion

}
