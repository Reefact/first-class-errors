#region Usings declarations

using FirstClassErrors.Usage.Resources;

#endregion

namespace FirstClassErrors.Usage {

    internal static class ErrCtxKey {

        #region Static members

        // The description is provided lazily so it follows the current UI culture (localized via UsageErrorMessages),
        // exactly like the errors' own prose — the key is still registered once by its name.
        public static readonly ErrorContextKey<DateOnly> TransactionDate =
            ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE", () => UsageErrorMessages.Get("Bank_TransactionDate_Context"));

        #endregion

    }

}
