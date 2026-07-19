namespace FirstClassErrors.RequestBinder;

/// <summary>
///     A named out-of-DTO argument awaiting its source and value: the stage between
///     <see cref="RequestBinder.Argument" /> and the value-object conversion. State where the value comes from
///     and supply it with <see cref="From{TArgument}(string, TArgument)" /> — or a host helper such as
///     <c>FromRoute</c> / <c>FromQuery</c> — then bind it with <c>AsRequired</c> / <c>AsOptional</c> on the returned
///     converter, exactly as a DTO property is bound.
/// </summary>
/// <remarks>
///     The source is a provenance label (<c>"route"</c>, <c>"query"</c>, <c>"header"</c>, …) captured in the failure's
///     context for diagnostics; it does not appear in the argument's error path, which is the name given to
///     <see cref="RequestBinder.Argument" />.
/// </remarks>
public sealed class ArgumentSource {

    #region Fields declarations

    private readonly RequestBinding _binding;
    private readonly string         _argumentPath;

    #endregion

    #region Constructors declarations

    internal ArgumentSource(RequestBinding binding, string argumentPath) {
        _binding      = binding;
        _argumentPath = argumentPath;
    }

    #endregion

    /// <summary>
    ///     Supplies the argument's provenance and value: a value that is <c>null</c> is treated as <b>absent</b> (the
    ///     <c>AsRequired</c> / <c>AsOptional</c> variant chosen next decides how absence is handled).
    /// </summary>
    /// <typeparam name="TArgument">The raw type of the supplied value.</typeparam>
    /// <param name="source">The provenance label recorded in the failure's context (for example <c>"route"</c>).</param>
    /// <param name="value">The raw value; <c>null</c> means the argument was absent.</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c> and their variants.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is <c>null</c>.</exception>
    public SimplePropertyConverter<TArgument> From<TArgument>(string source, TArgument? value) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new SimplePropertyConverter<TArgument>(_binding, _argumentPath, value, value is null, source);
    }

    /// <summary>
    ///     Supplies the provenance and value of a <b>value-type</b> argument (e.g. a <c>Guid?</c> route identifier),
    ///     surfacing the underlying non-nullable type to the converter — the value-type counterpart of
    ///     <see cref="From{TArgument}(string, TArgument)" />, existing for the same reason as the value-type
    ///     <c>SimpleProperty</c> overload.
    /// </summary>
    /// <typeparam name="TArgument">The underlying (non-nullable) value type of the supplied value.</typeparam>
    /// <param name="source">The provenance label recorded in the failure's context (for example <c>"route"</c>).</param>
    /// <param name="value">The raw value; <c>null</c> means the argument was absent.</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c> and their variants.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is <c>null</c>.</exception>
    public SimplePropertyConverter<TArgument> From<TArgument>(string source, TArgument? value) where TArgument : struct {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new SimplePropertyConverter<TArgument>(_binding, _argumentPath, value is null ? default : value.Value, value is null, source);
    }

}
