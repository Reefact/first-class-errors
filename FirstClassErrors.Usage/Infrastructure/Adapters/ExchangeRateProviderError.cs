#region Usings declarations

using FirstClassErrors.Usage.Model;
using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

/// <summary>
///     Provides factory methods for the secondary-port (outgoing) errors raised while calling the external
///     exchange-rate provider.
/// </summary>
[ProvidesErrorsFor(nameof(ExchangeRateProvider),
                   Description = "Errors raised while calling the external exchange-rate provider (an outgoing, secondary-port adapter).")]
public static class ExchangeRateProviderError {

    #region Statics members declarations

    [DocumentedBy(nameof(ServiceUnavailableDocumentation))]
    internal static SecondaryPortError ServiceUnavailable(string provider, Guid correlationId) {
        return new SecondaryPortError(
            Code.ServiceUnavailable,
            DocumentationFormatter.Format("The exchange-rate provider '{0}' is unavailable (correlation {1}).", provider, correlationId),
            Transience.Transient,
            "Exchange-rate service unavailable.",
            ctx => {
                ctx.Add(ErrCtxKey.Provider, provider);
                ctx.Add(ErrCtxKey.CorrelationId, correlationId);
            });
    }

    [DocumentedBy(nameof(UnsupportedCurrencyPairDocumentation))]
    internal static SecondaryPortError UnsupportedCurrencyPair(Currency from, Currency to) {
        return new SecondaryPortError(
            Code.UnsupportedCurrencyPair,
            DocumentationFormatter.Format("The exchange-rate provider does not quote the {0} to {1} currency pair.", from, to),
            Transience.NonTransient,
            "Unsupported currency pair.",
            ctx => {
                ctx.Add(ErrCtxKey.FromCurrency, from);
                ctx.Add(ErrCtxKey.ToCurrency, to);
            });
    }

    private static ErrorDocumentation ServiceUnavailableDocumentation() {
        return DescribeError.WithTitle("Exchange-rate service unavailable")
                            .WithDescription("This error occurs when the external exchange-rate provider cannot be reached (a timeout, a connection reset, or a 5xx response). It is transient: the call can be retried.")
                            .WithRule("Currency conversion depends on a reachable exchange-rate provider.")
                            .WithDiagnostic("The provider timed out or returned a server error.",
                                            ErrorOrigin.External,
                                            "Check the provider's health and retry the call, ideally with a backoff.")
                            .AndDiagnostic("The outgoing network path to the provider is disrupted.",
                                           ErrorOrigin.InternalOrExternal,
                                           "Verify outbound connectivity and any proxy or firewall between the service and the provider.")
                            .WithExamples(() => ServiceUnavailable("acme-fx", new Guid("22222222-2222-2222-2222-222222222222")));
    }

    private static ErrorDocumentation UnsupportedCurrencyPairDocumentation() {
        return DescribeError.WithTitle("Unsupported currency pair")
                            .WithDescription("This error occurs when the exchange-rate provider does not quote a rate for the requested source/target currency pair.")
                            .WithRule("A currency conversion can only be performed for a pair the provider quotes.")
                            .WithDiagnostic("The requested currency pair is not offered by the provider.",
                                            ErrorOrigin.External,
                                            "Confirm the provider supports both the source and target currencies before requesting a conversion.")
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
