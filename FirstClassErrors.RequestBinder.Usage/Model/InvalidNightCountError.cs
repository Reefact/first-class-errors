#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Resources;
using FirstClassErrors.RequestBinder.Usage.Utils;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The domain error a <see cref="NightCount" /> fails with when a request carries a number of nights that is not
///     strictly positive.
/// </summary>
[ProvidesErrorsFor(nameof(NightCount),
                   Description = "NightCount_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class InvalidNightCountError {

    #region Statics members declarations

    /// <summary>The number of nights carried by the request is zero or negative.</summary>
    [DocumentedBy(nameof(NotStrictlyPositiveDocumentation))]
    internal static DomainError NotStrictlyPositive(int nights) {
        return DomainError.Create(
                              Code.NotStrictlyPositive,
                              DocumentationFormatter.Format("A booking must be for at least one night, but {0} was requested.", nights))
                          .WithPublicMessage(
                              RequestBinderUsageMessages.Get("NightCount_NotPositive_ShortMessage"),
                              RequestBinderUsageMessages.Get("NightCount_NotPositive_DetailedMessage"));
    }

    private static ErrorDocumentation NotStrictlyPositiveDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("NightCount_NotPositive_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("NightCount_NotPositive_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("NightCount_NotPositive_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("NightCount_NotPositive_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("NightCount_NotPositive_Hint1"))
                            .WithExamples(() => NotStrictlyPositive(0));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode NotStrictlyPositive = ErrorCode.Create("NIGHT_COUNT_NOT_POSITIVE");

        #endregion

    }

    #endregion

}
