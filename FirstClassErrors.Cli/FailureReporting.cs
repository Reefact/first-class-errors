#region Usings declarations

using FirstClassErrors.GenDoc;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Shared rendering of coded pipeline failures, so every command reports a <see cref="DiagnosableException" />
///     with the exact same line format. That format is a contract: it is documented, test-asserted, and grep-able.
/// </summary>
internal static class FailureReporting {

    #region Statics members declarations

    /// <summary>
    ///     Reports a coded failure (GENDOC_… and friends): leads with the stable error code so the line is grep-able
    ///     and can be looked up in the generated catalog of the tool's own errors. The full exception goes to the
    ///     debug channel, which surfaces only under <c>--verbose</c>.
    /// </summary>
    /// <returns>The command exit code for a failure (1).</returns>
    internal static int ReportCodedFailure(IGenerationLogger logger, DiagnosableException exception) {
        logger.Error($"{exception.Error.Code}: {exception.Message}");
        logger.Debug(exception.ToString());

        return 1;
    }

    #endregion

}
