#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Resources;
using FirstClassErrors.RequestBinder.Usage.Utils;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The domain error a <see cref="Tag" /> fails with when a request list carries a malformed tag (empty, too long,
///     or containing whitespace).
/// </summary>
[ProvidesErrorsFor(nameof(Tag),
                   Description = "Tag_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class InvalidTagError {

    #region Statics members declarations

    /// <summary>An element of the request's tag list is not a well-formed tag.</summary>
    [DocumentedBy(nameof(MalformedDocumentation))]
    internal static DomainError Malformed(string rawValue) {
        return DomainError.Create(
                              Code.Malformed,
                              DocumentationFormatter.Format("'{0}' is not a valid tag.", rawValue))
                          .WithPublicMessage(
                              RequestBinderUsageMessages.Get("Tag_Malformed_ShortMessage"),
                              RequestBinderUsageMessages.Get("Tag_Malformed_DetailedMessage"));
    }

    private static ErrorDocumentation MalformedDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("Tag_Malformed_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("Tag_Malformed_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("Tag_Malformed_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("Tag_Malformed_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("Tag_Malformed_Hint1"))
                            .WithExamples(() => Malformed("late checkout"));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode Malformed = ErrorCode.Create("BOOKING_TAG_MALFORMED");

        #endregion

    }

    #endregion

}
