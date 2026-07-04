#region Usings declarations

using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

/// <summary>
///     Provides factory methods for the primary-port (incoming) errors raised by the statement-upload endpoint.
/// </summary>
[ProvidesErrorsFor(nameof(StatementUploadEndpoint),
                   Description = "Errors raised by the HTTP endpoint that ingests uploaded bank statements (an incoming, primary-port adapter).")]
public static class StatementUploadEndpointError {

    #region Statics members declarations

    [DocumentedBy(nameof(MalformedPayloadDocumentation))]
    internal static PrimaryPortError MalformedPayload(Guid requestId, string field) {
        return new PrimaryPortError(
            Code.MalformedPayload,
            DocumentationFormatter.Format("The statement upload request {0} is malformed: the '{1}' field is missing or invalid.", requestId, field),
            Transience.NonTransient,
            "Malformed statement payload.",
            ctx => {
                ctx.Add(ErrCtxKey.RequestId, requestId);
                ctx.Add(ErrCtxKey.Field, field);
            });
    }

    [DocumentedBy(nameof(RateLimitedDocumentation))]
    internal static PrimaryPortError RateLimited(Guid requestId, int retryAfterSeconds) {
        return new PrimaryPortError(
            Code.RateLimited,
            DocumentationFormatter.Format("The statement upload request {0} was rate-limited; retry after {1} seconds.", requestId, retryAfterSeconds),
            Transience.Transient,
            "Statement upload rate-limited.",
            ctx => ctx.Add(ErrCtxKey.RequestId, requestId));
    }

    private static ErrorDocumentation MalformedPayloadDocumentation() {
        return DescribeError.WithTitle("Malformed statement payload")
                            .WithDescription("This error occurs when the statement upload endpoint receives a request whose body is missing a required field or carries an invalid value.")
                            .WithRule("An uploaded statement request must carry every required field with a valid value.")
                            .WithDiagnostic("The client sent an incomplete or malformed request body.",
                                            ErrorOrigin.External,
                                            "Inspect the field named in the context and confirm the client sends it with a valid value.")
                            .WithExamples(() => MalformedPayload(new Guid("11111111-1111-1111-1111-111111111111"), "statementPeriod"));
    }

    private static ErrorDocumentation RateLimitedDocumentation() {
        return DescribeError.WithTitle("Statement upload rate-limited")
                            .WithDescription("This error occurs when too many statement uploads arrive in a short window and the endpoint throttles the request. It is transient: the same request can be retried later.")
                            .WithRule("Callers must stay within the endpoint's upload rate limit.")
                            .WithDiagnostic("The caller exceeded the allowed request rate.",
                                            ErrorOrigin.External,
                                            "Back off and retry after the delay indicated in the message.")
                            .WithExamples(() => RateLimited(new Guid("11111111-1111-1111-1111-111111111111"), 30));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode MalformedPayload = ErrorCode.Create("MALFORMED_STATEMENT_PAYLOAD");
        public static readonly ErrorCode RateLimited      = ErrorCode.Create("STATEMENT_UPLOAD_RATE_LIMITED");

        #endregion

    }

    #endregion

}
