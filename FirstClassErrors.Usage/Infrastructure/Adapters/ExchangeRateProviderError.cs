#region Usings declarations

using FirstClassErrors.Usage.Model;
using FirstClassErrors.Usage.Resources;
using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

/// <summary>
///     Provides factory methods for the secondary-port (outgoing) errors raised while calling the external
///     exchange-rate provider.
/// </summary>
[ProvidesErrorsFor(nameof(ExchangeRateProvider),
                   Description = "ExchangeRate_Source",
                   DescriptionResourceType = typeof(UsageErrorMessages))]
public static class ExchangeRateProviderError {

    #region Statics members declarations

    [DocumentedBy(nameof(ServiceUnavailableDocumentation))]
    internal static SecondaryPortError ServiceUnavailable(string provider, Guid correlationId) {
        return SecondaryPortError.Create(
                                     Code.ServiceUnavailable,
                                     DocumentationFormatter.Format("The exchange-rate provider '{0}' is unavailable (correlation {1}).", provider, correlationId),
                                     Transience.Transient,
                                     ctx => {
                                         ctx.Add(ErrCtxKey.Provider, provider);
                                         ctx.Add(ErrCtxKey.CorrelationId, correlationId);
                                     })
                                 .WithPublicMessage(
                                     UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_ShortMessage"),
                                     UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_DetailedMessage"));
    }

    [DocumentedBy(nameof(UnsupportedCurrencyPairDocumentation))]
    internal static SecondaryPortError UnsupportedCurrencyPair(Currency from, Currency to) {
        return SecondaryPortError.Create(
                                     Code.UnsupportedCurrencyPair,
                                     DocumentationFormatter.Format("The exchange-rate provider does not quote the {0} to {1} currency pair.", from, to),
                                     Transience.NonTransient,
                                     ctx => {
                                         ctx.Add(ErrCtxKey.FromCurrency, from);
                                         ctx.Add(ErrCtxKey.ToCurrency, to);
                                     })
                                 .WithPublicMessage(
                                     UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_ShortMessage"),
                                     UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_DetailedMessage"));
    }

    private static ErrorDocumentation ServiceUnavailableDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Title"))
                            .WithDescription(UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Description"))
                            .WithRule(UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Hint1"))
                            .AndDiagnostic(UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Cause2"),
                                           ErrorOrigin.InternalOrExternal,
                                           UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Hint2"))
                            .WithExamples(() => ServiceUnavailable("acme-fx", new Guid("22222222-2222-2222-2222-222222222222")));
    }

    private static ErrorDocumentation UnsupportedCurrencyPairDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_Title"))
                            .WithDescription(UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_Description"))
                            .WithRule(UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_Hint1"))
                            .WithExamples(() => UnsupportedCurrencyPair(Currency.EUR, Currency.USD));
    }

    #endregion

    #region Nested types declarations

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" members",
        Justification = "Each error-code constant deliberately mirrors the name of its factory method; both are always qualified (Code.X versus the X(...) call), so there is no real ambiguity.")]
    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode ServiceUnavailable      = ErrorCode.Create("EXCHANGE_RATE_SERVICE_UNAVAILABLE");
        public static readonly ErrorCode UnsupportedCurrencyPair = ErrorCode.Create("UNSUPPORTED_CURRENCY_PAIR");

        #endregion

    }

    #endregion

}
