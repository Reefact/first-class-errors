namespace DiagnosableExceptions.UnitTests {

    public static class ErrorCodeFactory {

        #region Static members

        public static ErrorCode CreateAny() {
            return ErrorCode.Create("ANY");
        }

        #endregion

    }

}