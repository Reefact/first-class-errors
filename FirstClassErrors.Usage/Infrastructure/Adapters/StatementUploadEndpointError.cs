#region Usings declarations

using FirstClassErrors.Usage.Resources;
using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

/// <summary>
///     Provides factory methods for the primary-port (incoming) errors raised by the statement-upload endpoint.
/// </summary>
[ProvidesErrorsFor(nameof(StatementUploadEndpoint),
                   Description = "StatementUpload_Source",
                   DescriptionResourceType = typeof(UsageErrorMessages))]
public static class StatementUploadEndpointError {

    #region Statics members declarations

    [DocumentedBy(nameof(MalformedPayloadDocumentation))]
    internal static PrimaryPortError MalformedPayload(Guid requestId, string field) {
        return PrimaryPortError.Create(
                                   Code.MalformedPayload,
                                   DocumentationFormatter.Format("The statement upload request {0} is malformed: the '{1}' field is missing or invalid.", requestId, field),
                                   Transience.NonTransient,
                                   ctx => {
                                       ctx.Add(ErrCtxKey.RequestId, requestId);
                                       ctx.Add(ErrCtxKey.Field, field);
                                   })
                               .WithPublicMessage(
                                   UsageErrorMessages.Get("StatementUpload_Malformed_ShortMessage"),
                                   UsageErrorMessages.Get("StatementUpload_Malformed_DetailedMessage"));
    }

    [DocumentedBy(nameof(RateLimitedDocumentation))]
    internal static PrimaryPortError RateLimited(Guid requestId, int retryAfterSeconds) {
        return PrimaryPortError.Create(
                                   Code.RateLimited,
                                   DocumentationFormatter.Format("The statement upload request {0} was rate-limited; retry after {1} seconds.", requestId, retryAfterSeconds),
                                   Transience.Transient,
                                   ctx => ctx.Add(ErrCtxKey.RequestId, requestId))
                               .WithPublicMessage(
                                   UsageErrorMessages.Get("StatementUpload_RateLimited_ShortMessage"),
                                   UsageErrorMessages.Get("StatementUpload_RateLimited_DetailedMessage"));
    }

    private static ErrorDocumentation MalformedPayloadDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("StatementUpload_Malformed_Title"))
                            .WithDescription(UsageErrorMessages.Get("StatementUpload_Malformed_Description"))
                            .WithRule(UsageErrorMessages.Get("StatementUpload_Malformed_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("StatementUpload_Malformed_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("StatementUpload_Malformed_Hint1"))
                            .WithExamples(() => MalformedPayload(new Guid("11111111-1111-1111-1111-111111111111"), "statementPeriod"));
    }

    private static ErrorDocumentation RateLimitedDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("StatementUpload_RateLimited_Title"))
                            .WithDescription(UsageErrorMessages.Get("StatementUpload_RateLimited_Description"))
                            .WithRule(UsageErrorMessages.Get("StatementUpload_RateLimited_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("StatementUpload_RateLimited_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("StatementUpload_RateLimited_Hint1"))
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
