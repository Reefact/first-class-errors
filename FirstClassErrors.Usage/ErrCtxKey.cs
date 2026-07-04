#region Usings declarations

using FirstClassErrors.Usage.Model;
using FirstClassErrors.Usage.Resources;

#endregion

namespace FirstClassErrors.Usage {

    internal static class ErrCtxKey {

        #region Static members

        // The description is provided lazily so it follows the current UI culture (localized via UsageErrorMessages),
        // exactly like the errors' own prose — the key is still registered once by its name.
        public static readonly ErrorContextKey<DateOnly> TransactionDate =
            ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE", () => UsageErrorMessages.Get("Bank_TransactionDate_Context"));

        // The keys below carry a plain (non-localized) description. Their example values all stringify culture-invariantly
        // (Guid, string, Currency, Amount), so the rendered context stays deterministic across languages.
        public static readonly ErrorContextKey<Amount> TransferAmount =
            ErrorContextKey.Create<Amount>("TRANSFER_AMOUNT", "The monetary amount of the attempted transfer.");

        public static readonly ErrorContextKey<Guid> RequestId =
            ErrorContextKey.Create<Guid>("REQUEST_ID", "The identifier of the incoming request.");

        public static readonly ErrorContextKey<string> Field =
            ErrorContextKey.Create<string>("FIELD", "The request field that failed validation.");

        public static readonly ErrorContextKey<string> Provider =
            ErrorContextKey.Create<string>("PROVIDER", "The external provider that was called.");

        public static readonly ErrorContextKey<Guid> CorrelationId =
            ErrorContextKey.Create<Guid>("CORRELATION_ID", "The correlation identifier of the outgoing call.");

        public static readonly ErrorContextKey<Currency> FromCurrency =
            ErrorContextKey.Create<Currency>("FROM_CURRENCY", "The source currency of the conversion.");

        public static readonly ErrorContextKey<Currency> ToCurrency =
            ErrorContextKey.Create<Currency>("TO_CURRENCY", "The target currency of the conversion.");

        #endregion

    }

}
