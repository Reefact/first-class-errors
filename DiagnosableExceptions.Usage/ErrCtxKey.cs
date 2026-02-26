namespace DiagnosableExceptions.Usage {

    internal static class ErrCtxKey {

        #region Static members

        public static readonly ErrorContextKey<DateOnly> TransactionDate = ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE");

        #endregion

    }

}