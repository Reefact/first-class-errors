namespace FirstClassErrors.Cli;

/// <summary>
///     The exit codes every <c>fce</c> command returns, named once. They are a contract rather than an internal
///     detail: a build script branches on them, the README documents them, and the command tests assert them — so
///     the set is closed, and a command answering with a number outside it would be a defect no compiler could see.
/// </summary>
/// <remarks>
///     Written out here because a bare <c>return 130</c> at the end of a <c>catch</c> block says nothing about what
///     130 means, and the answer was previously carried by a prose comment repeated at each of the three sites that
///     returned it. A comment cannot keep three literals in step; a constant they all read can.
/// </remarks>
internal static class ExitCodes {

    /// <summary>The command did what it was asked.</summary>
    internal const int Success = 0;

    /// <summary>The command failed: an unusable argument, a missing input, a coded pipeline failure.</summary>
    internal const int Failure = 1;

    /// <summary>
    ///     <c>fce catalog diff</c> found changes at or above the impact it was told to fail on. Distinct from
    ///     <see cref="Failure" /> on purpose: the command worked, and the catalog is what the caller must look at.
    /// </summary>
    internal const int ChangesDetected = 2;

    /// <summary>
    ///     The run was cancelled (Ctrl+C). The conventional value for a process killed by a signal is
    ///     <c>128 + signal</c>, and SIGINT is signal 2 — an abort, not a failure.
    /// </summary>
    internal const int Canceled = 130;

}
