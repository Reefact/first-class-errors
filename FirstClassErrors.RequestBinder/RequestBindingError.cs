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
    ///     The argument at <paramref name="argumentPath" /> is required but was absent from the request.
    /// </summary>
    /// <remarks>Non-transient by nature: resubmitting the same request cannot succeed.</remarks>
    [DocumentedBy(nameof(ArgumentRequiredDocumentation))]
    internal static PrimaryPortError ArgumentRequired(string argumentPath) {
        return PrimaryPortError.Create(
                                   Code.ArgumentRequired,
                                   $"Argument '{argumentPath}' is required but was missing from the request.",
                                   Transience.NonTransient,
                                   ctx => ctx.Add(ErrCtxKey.RequestArgument, argumentPath))
                               .WithPublicMessage(
                                   "A required argument is missing.",
                                   $"The argument '{argumentPath}' is required.");
    }

    /// <summary>
    ///     The argument at <paramref name="argumentPath" /> was present but failed to convert; the conversion error is
    ///     attached as the inner error.
    /// </summary>
    /// <param name="argumentPath">The full path of the failing argument.</param>
    /// <param name="cause">
    ///     The error the converter failed with. Converters must fail with a <see cref="DomainError" /> or a
    ///     <see cref="PrimaryPortError" /> — the two families a <see cref="PrimaryPortInnerErrors" /> accepts. Any
    ///     other family is a converter bug, reported by throwing (the binder's bug channel), never recorded.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="cause" /> belongs to another error family.</exception>
    [DocumentedBy(nameof(ArgumentInvalidDocumentation))]
    internal static PrimaryPortError ArgumentInvalid(string argumentPath, Error cause) {
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

        return PrimaryPortError.Create(
                                   Code.ArgumentInvalid,
                                   $"Argument '{argumentPath}' is invalid.",
                                   innerErrors,
                                   ctx => ctx.Add(ErrCtxKey.RequestArgument, argumentPath))
                               .WithPublicMessage(
                                   "An argument is invalid.",
                                   $"The argument '{argumentPath}' is invalid.");
    }

    private static ErrorDocumentation ArgumentRequiredDocumentation() {
        return DescribeError.WithTitle("Required request argument missing")
                            .WithDescription("An incoming request omits an argument that the bound command requires. The full path of the missing argument is carried in the error context.")
                            .WithRule("Every argument bound with AsRequired must be present in the request.")
                            .WithDiagnostic("The client did not send the argument (wrong payload shape, renamed field, or version mismatch between client and API).",
                                            ErrorOrigin.External,
                                            "Compare the request payload with the API contract; check the client and API versions.")
                            .AndDiagnostic("The argument name reported in the path does not match the wire format (the binder uses the C# property name unless an IArgumentNameProvider is configured).",
                                           ErrorOrigin.Internal,
                                           "Configure an IArgumentNameProvider aligned with the serializer naming policy.")
                            .WithExamples(() => ArgumentRequired("Guests[1].FirstName"));
    }

    private static ErrorDocumentation ArgumentInvalidDocumentation() {
        return DescribeError.WithTitle("Request argument invalid")
                            .WithDescription("An incoming request carries an argument that fails to convert into its value object. The full path of the failing argument is carried in the error context, and the precise conversion error is attached as the inner error.")
                            .WithRule("Every bound argument must convert successfully into its target value object.")
                            .WithDiagnostic("The client sent a malformed value (wrong format, out of range, unknown identifier).",
                                            ErrorOrigin.External,
                                            "Read the inner error: it is the value object's own coded error and states the violated rule.")
                            .AndDiagnostic("The converter rejects values the contract intends to accept (over-strict parsing rule).",
                                           ErrorOrigin.Internal,
                                           "Review the value object's parsing rule against the API contract.")
                            .WithExamples(() => ArgumentInvalid("GuestEmail", SampleCause()));
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

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode ArgumentRequired = ErrorCode.Create("REQUEST_ARGUMENT_REQUIRED");
        public static readonly ErrorCode ArgumentInvalid  = ErrorCode.Create("REQUEST_ARGUMENT_INVALID");

        #endregion

    }

    private static class ErrCtxKey {

        #region Statics members declarations

        public static readonly ErrorContextKey<string> RequestArgument =
            ErrorContextKey.Create<string>("RequestArgument", "Full path of the request argument that failed to bind (e.g. 'Guests[1].FirstName').");

        #endregion

    }

    #endregion

}
