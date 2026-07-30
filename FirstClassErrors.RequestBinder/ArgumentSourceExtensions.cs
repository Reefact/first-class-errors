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

    // The provenance labels, named once. Each is passed by four overloads — scalar, scalar value-type, list, list
    // value-type — and the label is what reaches the caller in the argument path, so a typo in one of the four would
    // split a source in two and only surface in a consumer's error report. The XML docs below still spell the literal
    // out, so nothing is hidden from a reader of any single method.
    private const string Route  = "route";
    private const string Query  = "query";
    private const string Header = "header";
    private const string Body   = "body";
    private const string Form   = "form";

    /// <summary>Binds an argument sourced from the route (<c>From("route", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromRoute<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From(Route, value);
    }

    /// <summary>Binds a value-type argument sourced from the route (<c>From("route", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromRoute<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From(Route, value);
    }

    /// <summary>Binds an argument sourced from the query string (<c>From("query", value)</c>).</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together",
                                                     Justification =
                                                         "Overloads are organized by SHAPE first — every scalar binder, then every list binder — and by source within each block, so " +
                                                         "From*(value) reads as one family and From*(values) as another. The scalar pair of each source IS adjacent; what separates a name " +
                                                         "from itself is the list block below. This shape-first grouping is deliberate, and matches how OutcomeTaskExtensions groups by " +
                                                         "receiver type.")]
    public static SimplePropertyConverter<TArgument> FromQuery<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From(Query, value);
    }

    /// <summary>Binds a value-type argument sourced from the query string (<c>From("query", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromQuery<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From(Query, value);
    }

    /// <summary>Binds an argument sourced from a request header (<c>From("header", value)</c>).</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together",
                                                     Justification =
                                                         "Overloads are organized by SHAPE first — every scalar binder, then every list binder — and by source within each block, so " +
                                                         "From*(value) reads as one family and From*(values) as another. The scalar pair of each source IS adjacent; what separates a name " +
                                                         "from itself is the list block below. This shape-first grouping is deliberate, and matches how OutcomeTaskExtensions groups by " +
                                                         "receiver type.")]
    public static SimplePropertyConverter<TArgument> FromHeader<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From(Header, value);
    }

    /// <summary>Binds a value-type argument sourced from a request header (<c>From("header", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromHeader<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From(Header, value);
    }

    /// <summary>Binds an argument sourced from the request body (<c>From("body", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromBody<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From(Body, value);
    }

    /// <summary>Binds a value-type argument sourced from the request body (<c>From("body", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromBody<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From(Body, value);
    }

    /// <summary>Binds an argument sourced from a form field (<c>From("form", value)</c>).</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together",
                                                     Justification =
                                                         "Overloads are organized by SHAPE first — every scalar binder, then every list binder — and by source within each block, so " +
                                                         "From*(value) reads as one family and From*(values) as another. The scalar pair of each source IS adjacent; what separates a name " +
                                                         "from itself is the list block below. This shape-first grouping is deliberate, and matches how OutcomeTaskExtensions groups by " +
                                                         "receiver type.")]
    public static SimplePropertyConverter<TArgument> FromForm<TArgument>(this ArgumentSource argument, TArgument? value) {
        return Guarded(argument).From(Form, value);
    }

    /// <summary>Binds a value-type argument sourced from a form field (<c>From("form", value)</c>).</summary>
    public static SimplePropertyConverter<TArgument> FromForm<TArgument>(this ArgumentSource argument, TArgument? value) where TArgument : struct {
        return Guarded(argument).From(Form, value);
    }

    /// <summary>Binds a list argument sourced from the query string (<c>From("query", values)</c>).</summary>
    public static ListOfSimplePropertiesConverter<TArgument> FromQuery<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) {
        return Guarded(argument).From(Query, values);
    }

    /// <summary>Binds a value-type list argument sourced from the query string (<c>From("query", values)</c>).</summary>
    public static ListOfSimpleValuePropertiesConverter<TArgument> FromQuery<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) where TArgument : struct {
        return Guarded(argument).From(Query, values);
    }

    /// <summary>Binds a list argument sourced from repeated request headers (<c>From("header", values)</c>).</summary>
    public static ListOfSimplePropertiesConverter<TArgument> FromHeader<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) {
        return Guarded(argument).From(Header, values);
    }

    /// <summary>Binds a value-type list argument sourced from repeated request headers (<c>From("header", values)</c>).</summary>
    public static ListOfSimpleValuePropertiesConverter<TArgument> FromHeader<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) where TArgument : struct {
        return Guarded(argument).From(Header, values);
    }

    /// <summary>Binds a list argument sourced from repeated form fields (<c>From("form", values)</c>).</summary>
    public static ListOfSimplePropertiesConverter<TArgument> FromForm<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) {
        return Guarded(argument).From(Form, values);
    }

    /// <summary>Binds a value-type list argument sourced from repeated form fields (<c>From("form", values)</c>).</summary>
    public static ListOfSimpleValuePropertiesConverter<TArgument> FromForm<TArgument>(this ArgumentListSource argument, IEnumerable<TArgument?>? values) where TArgument : struct {
        return Guarded(argument).From(Form, values);
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
