#region Usings declarations

using FirstClassErrors.Usage.Model;
using FirstClassErrors.Usage.Resources;

#endregion

namespace FirstClassErrors.Usage {

    internal static class ErrCtxKey {

        #region Static members

        // Descriptions are provided lazily so they follow the current UI culture (localized via UsageErrorMessages),
        // exactly like the errors' own prose — each key is still registered once by its name. The value types below all
        // stringify culture-invariantly (Guid, string, Currency, Amount), so the rendered context stays deterministic.
        public static readonly ErrorContextKey<DateOnly> TransactionDate =
            ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE", () => UsageErrorMessages.Get("Bank_TransactionDate_Context"));

        public static readonly ErrorContextKey<Amount> TransferAmount =
            ErrorContextKey.Create<Amount>("TRANSFER_AMOUNT", () => UsageErrorMessages.Get("MoneyTransfer_TransferAmount_Context"));

        public static readonly ErrorContextKey<Guid> RequestId =
            ErrorContextKey.Create<Guid>("REQUEST_ID", () => UsageErrorMessages.Get("StatementUpload_RequestId_Context"));

        public static readonly ErrorContextKey<string> Field =
            ErrorContextKey.Create<string>("FIELD", () => UsageErrorMessages.Get("StatementUpload_Field_Context"));

        public static readonly ErrorContextKey<string> Provider =
            ErrorContextKey.Create<string>("PROVIDER", () => UsageErrorMessages.Get("ExchangeRate_Provider_Context"));

        public static readonly ErrorContextKey<Guid> CorrelationId =
            ErrorContextKey.Create<Guid>("CORRELATION_ID", () => UsageErrorMessages.Get("ExchangeRate_CorrelationId_Context"));

        public static readonly ErrorContextKey<Currency> FromCurrency =
            ErrorContextKey.Create<Currency>("FROM_CURRENCY", () => UsageErrorMessages.Get("ExchangeRate_FromCurrency_Context"));

        public static readonly ErrorContextKey<Currency> ToCurrency =
            ErrorContextKey.Create<Currency>("TO_CURRENCY", () => UsageErrorMessages.Get("ExchangeRate_ToCurrency_Context"));

        #endregion

    }

}
