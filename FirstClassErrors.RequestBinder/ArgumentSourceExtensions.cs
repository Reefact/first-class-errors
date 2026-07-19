namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Host-agnostic provenance shortcuts over <see cref="ArgumentSource.From{TArgument}(string, TArgument)" /> and
///     <see cref="ArgumentListSource.From{TArgument}(string, IEnumerable{TArgument})" />: <c>FromRoute(v)</c> is exactly
///     <c>From("route", v)</c>. They only tag a provenance label on an already-extracted value — they carry no
///     dependency on any web framework, so they live in the core. A host integration package may add richer helpers that
///     extract the value from the incoming request itself.
/// </summary>
public static class ArgumentSourceExtensions {

    #region Statics members declarations

    /// <summary>Binds an argument sourced from the route (<c>From("route", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromRoute<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From("route", value);
    }

    /// <summary>Binds a value-type argument sourced from the route (<c>From("route", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromRoute<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From("route", value);
    }

    /// <summary>Binds an argument sourced from the query string (<c>From("query", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromQuery<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From("query", value);
    }

    /// <summary>Binds a value-type argument sourced from the query string (<c>From("query", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromQuery<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From("query", value);
    }

    /// <summary>Binds an argument sourced from a request header (<c>From("header", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromHeader<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From("header", value);
    }

    /// <summary>Binds a value-type argument sourced from a request header (<c>From("header", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromHeader<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From("header", value);
    }

    /// <summary>Binds an argument sourced from the request body (<c>From("body", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromBody<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From("body", value);
    }

    /// <summary>Binds a value-type argument sourced from the request body (<c>From("body", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromBody<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From("body", value);
    }

    /// <summary>Binds an argument sourced from a form field (<c>From("form", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromForm<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From("form", value);
    }

    /// <summary>Binds a value-type argument sourced from a form field (<c>From("form", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromForm<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From("form", value);
    }

    /// <summary>Binds a list argument sourced from the query string (<c>From("query", values)</c>).</summary>
    public static ListOfSimplePropertiesConverter<TArgument> FromQuery<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) {
        return Guarded(argument).From("query", values);
    }

    /// <summary>Binds a value-type list argument sourced from the query string (<c>From("query", values)</c>).</summary>
    public static ListOfSimpleValuePropertiesConverter<TArgument> FromQuery<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) where TArgument : struct {
        return Guarded(argument).From("query", values);
    }

    /// <summary>Binds a list argument sourced from repeated request headers (<c>From("header", values)</c>).</summary>
    public static ListOfSimplePropertiesConverter<TArgument> FromHeader<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) {
        return Guarded(argument).From("header", values);
    }

    /// <summary>Binds a value-type list argument sourced from repeated request headers (<c>From("header", values)</c>).</summary>
    public static ListOfSimpleValuePropertiesConverter<TArgument> FromHeader<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) where TArgument : struct {
        return Guarded(argument).From("header", values);
    }

    /// <summary>Binds a list argument sourced from repeated form fields (<c>From("form", values)</c>).</summary>
    public static ListOfSimplePropertiesConverter<TArgument> FromForm<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) {
        return Guarded(argument).From("form", values);
    }

    /// <summary>Binds a value-type list argument sourced from repeated form fields (<c>From("form", values)</c>).</summary>
    public static ListOfSimpleValuePropertiesConverter<TArgument> FromForm<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) where TArgument : struct {
        return Guarded(argument).From("form", values);
    }

    private static ArgumentSource Guarded(ArgumentSource argument) {
        if (argument is null) { throw new ArgumentNullException(nameof(argument)); }

        return argument;
    }

    private static ArgumentListSource Guarded(ArgumentListSource argument) {
        if (argument is null) { throw new ArgumentNullException(nameof(argument)); }

        return argument;
    }

    #endregion

}
