#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Provides the structural binding errors raised by the request binder itself: an argument that is missing, and
///     an argument that is present but does not convert. Both carry the full argument path (for example
///     <c>Guests[1].FirstName</c>) in their context, so the failing field is identifiable without parsing messages.
/// </summary>
/// <remarks>
///     These are the only errors the binder manufactures on its own. Every other error in a binding failure tree
///     comes from the application: the conversion errors returned by the value-object converters, and the envelope
///     errors declared with <c>FailWith</c>.
/// </remarks>
[ProvidesErrorsFor("RequestBinding",
                   Description = "Structural validation of an incoming request at the primary-adapter boundary: required arguments and value conversions.")]
public static class RequestBindingError {

    #region Statics members declarations

    /// <summary>
    ///     The default definition — code and public messages, kept together — of the missing-required-argument failure:
    ///     the code <c>REQUEST_ARGUMENT_REQUIRED</c> and its English messages. A consumer overrides it through
    ///     <see cref="RequestBinderOptions.ArgumentRequired" />, deriving from this default with
    ///     <see cref="BinderErrorDefinition.WithCode" /> / <see cref="BinderErrorDefinition.WithMessage" /> to change the
    ///     code, the messages, or both; whatever is left untouched keeps the value shown here.
    /// </summary>
    public static BinderErrorDefinition DefaultArgumentRequired { get; } =
        new BinderErrorDefinition(
            ErrorCode.Create("REQUEST_ARGUMENT_REQUIRED"),
            argumentPath => new BindingMessage(
                                "A required argument is missing.",
                                $"The argument '{argumentPath}' is required."));

    /// <summary>
    ///     The default definition — code and public messages, kept together — of the present-but-invalid-argument
    ///     failure: the code <c>REQUEST_ARGUMENT_INVALID</c> and its English messages. Override it through
    ///     <see cref="RequestBinderOptions.ArgumentInvalid" />, exactly as for <see cref="DefaultArgumentRequired" />.
    /// </summary>
    public static BinderErrorDefinition DefaultArgumentInvalid { get; } =
        new BinderErrorDefinition(
            ErrorCode.Create("REQUEST_ARGUMENT_INVALID"),
            argumentPath => new BindingMessage(
                                "An argument is invalid.",
                                $"The argument '{argumentPath}' is invalid."));

    /// <summary>
    ///     The default code raised when a required argument is missing (<c>REQUEST_ARGUMENT_REQUIRED</c>), unless a
    ///     consumer overrides it through <see cref="RequestBinderOptions.ArgumentRequired" />. Exposed so a consumer can
    ///     branch on the binder's structural failures symbolically rather than by string.
    /// </summary>
    public static ErrorCode DefaultArgumentRequiredCode => DefaultArgumentRequired.Code;

    /// <summary>
    ///     The default code raised when an argument is present but fails to convert (<c>REQUEST_ARGUMENT_INVALID</c>),
    ///     unless a consumer overrides it through <see cref="RequestBinderOptions.ArgumentInvalid" />. Exposed so a
    ///     consumer can branch on the binder's structural failures symbolically rather than by string.
    /// </summary>
    public static ErrorCode DefaultArgumentInvalidCode => DefaultArgumentInvalid.Code;

    /// <summary>
    ///     The argument at <paramref name="argumentPath" /> is required but was absent from the request.
    /// </summary>
    /// <param name="definition">The structural-error definition to raise — the binder's configured <see cref="RequestBinderOptions.ArgumentRequired" />, defaulting to <see cref="DefaultArgumentRequired" />.</param>
    /// <param name="argumentPath">The full path of the missing argument.</param>
    /// <remarks>
    ///     Non-transient by nature: resubmitting the same request cannot succeed. The diagnostic message stays in the
    ///     library's internal language (English) by convention; only the public messages are localizable, through
    ///     <paramref name="definition" />.
    /// </remarks>
    [DocumentedBy(nameof(ArgumentRequiredDocumentation))]
    internal static PrimaryPortError ArgumentRequired(BinderErrorDefinition definition, string argumentPath) {
        BindingMessage message = definition.GetMessage(argumentPath);

        return PrimaryPortError.Create(
                                   definition.Code,
                                   $"Argument '{argumentPath}' is required but was missing from the request.",
                                   Transience.NonTransient,
                                   ctx => ctx.Add(ErrCtxKey.RequestArgument, argumentPath))
                               .WithPublicMessage(message.ShortMessage, message.DetailedMessage);
    }

    /// <summary>
    ///     The argument at <paramref name="argumentPath" /> was present but failed to convert; the conversion error is
    ///     attached as the inner error.
    /// </summary>
    /// <param name="definition">The structural-error definition to raise — the binder's configured <see cref="RequestBinderOptions.ArgumentInvalid" />, defaulting to <see cref="DefaultArgumentInvalid" />.</param>
    /// <param name="argumentPath">The full path of the failing argument.</param>
    /// <param name="cause">
    ///     The error the converter failed with. Converters must fail with a <see cref="DomainError" /> or a
    ///     <see cref="PrimaryPortError" /> — the two families a <see cref="PrimaryPortInnerErrors" /> accepts. Any
    ///     other family is a converter bug, reported by throwing (the binder's bug channel), never recorded.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="cause" /> belongs to another error family.</exception>
    [DocumentedBy(nameof(ArgumentInvalidDocumentation))]
    internal static PrimaryPortError ArgumentInvalid(BinderErrorDefinition definition, string argumentPath, Error cause) {
        PrimaryPortInnerErrors innerErrors = new();
        switch (cause) {
            case DomainError domainError:
                innerErrors.Add(domainError);

                break;
            case PrimaryPortError primaryPortError:
                innerErrors.Add(primaryPortError);

                break;
            default:
                throw new InvalidOperationException(
                    $"The converter of argument '{argumentPath}' failed with a {cause.GetType().Name}; a converter must fail with a DomainError or a PrimaryPortError.");
        }

        BindingMessage message = definition.GetMessage(argumentPath);

        return PrimaryPortError.Create(
                                   definition.Code,
                                   $"Argument '{argumentPath}' is invalid.",
                                   innerErrors,
                                   ctx => ctx.Add(ErrCtxKey.RequestArgument, argumentPath))
                               .WithPublicMessage(message.ShortMessage, message.DetailedMessage);
    }

    /// <summary>
    ///     Builds a representative missing-required-argument error (<c>REQUEST_ARGUMENT_REQUIRED</c> by default) from
    ///     <paramref name="definition" />, for documentation. A consumer that overrides the definition through
    ///     <see cref="RequestBinderOptions.ArgumentRequired" /> uses this to render, in its own catalog, the structural
    ///     error it actually emits — built the same way the binder builds it at binding time, so the documented example
    ///     stays faithful to runtime.
    /// </summary>
    /// <param name="definition">The definition to render — typically the one injected into <see cref="RequestBinderOptions.ArgumentRequired" />.</param>
    /// <returns>A representative error carrying the definition's code and public messages.</returns>
    [SuppressMessage("FirstClassErrors.DocumentationWiring", "FCE009:ErrorFactoryNotDocumented",
                     Justification = "Not a catalog entry: a public sample builder a consumer calls to document, in its own catalog, the structural error it emits after overriding the definition. The binder's own documented factory is ArgumentRequired.")]
    public static PrimaryPortError SampleArgumentRequired(BinderErrorDefinition definition) {
        return ArgumentRequired(definition, "Guests[1].FirstName");
    }

    /// <summary>
    ///     The full documentation of the missing-required-argument failure for <paramref name="definition" /> — the
    ///     binder's own generic prose (title, rule, diagnoses), with a live example built from the definition. A consumer
    ///     that overrides the definition surfaces its effective structural error in its own catalog by returning this from
    ///     a <c>[DocumentedBy]</c> documentation method (see the RequestBinder guide).
    /// </summary>
    /// <param name="definition">The definition to document — typically the one injected into <see cref="RequestBinderOptions.ArgumentRequired" />.</param>
    /// <returns>The error documentation, ready to return from a documentation method.</returns>
    public static ErrorDocumentation DescribeArgumentRequired(BinderErrorDefinition definition) {
        return DescribeError.WithTitle("Required request argument missing")
                            .WithDescription("An incoming request omits an argument that the bound command requires. The full path of the missing argument is carried in the error context.")
                            .WithRule("Every argument bound with AsRequired must be present in the request.")
                            .WithDiagnostic("The client did not send the argument (wrong payload shape, renamed field, or version mismatch between client and API).",
                                            ErrorOrigin.External,
                                            "Compare the request payload with the API contract; check the client and API versions.")
                            .AndDiagnostic("The argument name reported in the path does not match the wire format (the binder uses the C# property name unless an IArgumentNameProvider is configured).",
                                           ErrorOrigin.Internal,
                                           "Configure an IArgumentNameProvider aligned with the serializer naming policy.")
                            .WithExamples(() => SampleArgumentRequired(definition));
    }

    /// <summary>
    ///     Builds a representative present-but-invalid-argument error (<c>REQUEST_ARGUMENT_INVALID</c> by default) from
    ///     <paramref name="definition" />, for documentation — see <see cref="SampleArgumentRequired" />. A sample
    ///     converter cause is attached, exactly as the binder attaches the real converter's error at binding time.
    /// </summary>
    /// <param name="definition">The definition to render — typically the one injected into <see cref="RequestBinderOptions.ArgumentInvalid" />.</param>
    /// <returns>A representative error carrying the definition's code and public messages, wrapping a sample cause.</returns>
    [SuppressMessage("FirstClassErrors.DocumentationWiring", "FCE009:ErrorFactoryNotDocumented",
                     Justification = "Not a catalog entry: a public sample builder a consumer calls to document, in its own catalog, the structural error it emits after overriding the definition. The binder's own documented factory is ArgumentInvalid.")]
    public static PrimaryPortError SampleArgumentInvalid(BinderErrorDefinition definition) {
        return ArgumentInvalid(definition, "GuestEmail", SampleCause());
    }

    /// <summary>
    ///     The full documentation of the present-but-invalid-argument failure for <paramref name="definition" /> — see
    ///     <see cref="DescribeArgumentRequired" />.
    /// </summary>
    /// <param name="definition">The definition to document — typically the one injected into <see cref="RequestBinderOptions.ArgumentInvalid" />.</param>
    /// <returns>The error documentation, ready to return from a documentation method.</returns>
    public static ErrorDocumentation DescribeArgumentInvalid(BinderErrorDefinition definition) {
        return DescribeError.WithTitle("Request argument invalid")
                            .WithDescription("An incoming request carries an argument that fails to convert into its value object. The full path of the failing argument is carried in the error context, and the precise conversion error is attached as the inner error.")
                            .WithRule("Every bound argument must convert successfully into its target value object.")
                            .WithDiagnostic("The client sent a malformed value (wrong format, out of range, unknown identifier).",
                                            ErrorOrigin.External,
                                            "Read the inner error: it is the value object's own coded error and states the violated rule.")
                            .AndDiagnostic("The converter rejects values the contract intends to accept (over-strict parsing rule).",
                                           ErrorOrigin.Internal,
                                           "Review the value object's parsing rule against the API contract.")
                            .WithExamples(() => SampleArgumentInvalid(definition));
    }

    private static ErrorDocumentation ArgumentRequiredDocumentation() {
        return DescribeArgumentRequired(DefaultArgumentRequired);
    }

    private static ErrorDocumentation ArgumentInvalidDocumentation() {
        return DescribeArgumentInvalid(DefaultArgumentInvalid);
    }

    /// <summary>A representative converter failure used only by the documentation example above.</summary>
    private static DomainError SampleCause() {
        return DomainError.Create(
                              ErrorCode.Create("EMAIL_ADDRESS_INVALID"),
                              "The value 'not-an-email' is not a valid email address.")
                          .WithPublicMessage("The email address is invalid.");
    }

    #endregion

    #region Nested types declarations

    private static class ErrCtxKey {

        #region Statics members declarations

        public static readonly ErrorContextKey<string> RequestArgument =
            ErrorContextKey.Create<string>("RequestArgument", "Full path of the request argument that failed to bind (e.g. 'Guests[1].FirstName').");

        #endregion

    }

    #endregion

}
