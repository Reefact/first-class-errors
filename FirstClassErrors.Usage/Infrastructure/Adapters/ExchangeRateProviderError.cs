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
        return new SecondaryPortError(
            Code.ServiceUnavailable,
            DocumentationFormatter.Format(UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_Message"), provider, correlationId),
            Transience.Transient,
            UsageErrorMessages.Get("ExchangeRate_ServiceUnavailable_ShortMessage"),
            ctx => {
                ctx.Add(ErrCtxKey.Provider, provider);
                ctx.Add(ErrCtxKey.CorrelationId, correlationId);
            });
    }

    [DocumentedBy(nameof(UnsupportedCurrencyPairDocumentation))]
    internal static SecondaryPortError UnsupportedCurrencyPair(Currency from, Currency to) {
        return new SecondaryPortError(
            Code.UnsupportedCurrencyPair,
            DocumentationFormatter.Format(UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_Message"), from, to),
            Transience.NonTransient,
            UsageErrorMessages.Get("ExchangeRate_UnsupportedPair_ShortMessage"),
            ctx => {
                ctx.Add(ErrCtxKey.FromCurrency, from);
                ctx.Add(ErrCtxKey.ToCurrency, to);
            });
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

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode ServiceUnavailable      = ErrorCode.Create("EXCHANGE_RATE_SERVICE_UNAVAILABLE");
        public static readonly ErrorCode UnsupportedCurrencyPair = ErrorCode.Create("UNSUPPORTED_CURRENCY_PAIR");

        #endregion

    }

    #endregion

}
