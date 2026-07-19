namespace FirstClassErrors.RequestBinder;

/// <summary>
///     A named out-of-DTO <b>list</b> argument awaiting its source and values: the stage between
///     <see cref="RequestBinder.ArgumentList" /> and the per-element value-object conversion. State where the
///     values come from and supply them with <see cref="From{TArgument}(string, IEnumerable{TArgument})" /> — or a host
///     helper such as <c>FromQuery</c> — then bind them with <c>AsRequired</c> / <c>AsOptional</c>. Each failing element
///     is recorded under its indexed path (<c>tag[2]</c>).
/// </summary>
public sealed class ArgumentListSource {

    #region Fields declarations

    private readonly RequestBinding _binding;
    private readonly string         _argumentPath;

    #endregion

    #region Constructors declarations

    internal ArgumentListSource(RequestBinding binding, string argumentPath) {
        _binding      = binding;
        _argumentPath = argumentPath;
    }

    #endregion

    /// <summary>
    ///     Supplies the list argument's provenance and values: a <c>null</c> collection is treated as <b>absent</b>
    ///     (present-but-empty is a valid empty list), and a <c>null</c> element records the required-argument failure
    ///     under its indexed path.
    /// </summary>
    /// <typeparam name="TArgument">The raw element type of the supplied values.</typeparam>
    /// <param name="source">The provenance label recorded in each failing element's context (for example <c>"query"</c>).</param>
    /// <param name="values">The raw values; <c>null</c> means the argument was absent.</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is <c>null</c>.</exception>
    public ListOfSimplePropertiesConverter<TArgument> From<TArgument>(string source, IEnumerable<TArgument?>? values) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new ListOfSimplePropertiesConverter<TArgument>(_binding, _argumentPath, values, values is null, source);
    }

    /// <summary>
    ///     Supplies the provenance and values of a <b>value-type</b> list argument (e.g. a repeated <c>int?</c> query
    ///     parameter), surfacing the underlying non-nullable element type to the converter.
    /// </summary>
    /// <typeparam name="TArgument">The underlying (non-nullable) value type of the supplied elements.</typeparam>
    /// <param name="source">The provenance label recorded in each failing element's context (for example <c>"query"</c>).</param>
    /// <param name="values">The raw values; <c>null</c> means the argument was absent.</param>
    /// <returns>The converter stage offering <c>AsRequired</c> / <c>AsOptional</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is <c>null</c>.</exception>
    public ListOfSimpleValuePropertiesConverter<TArgument> From<TArgument>(string source, IEnumerable<TArgument?>? values) where TArgument : struct {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new ListOfSimpleValuePropertiesConverter<TArgument>(_binding, _argumentPath, values, values is null, source);
    }

}
