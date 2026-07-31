namespace FirstClassErrors.GenDoc.Worker;

/// <summary>
///     The exit codes this worker answers with, named once so the table in <c>Program.cs</c>'s header note and the
///     numbers the code actually returns cannot say different things. The generator that launches the worker reads
///     them back to tell a bad call from a failed extraction, which makes the set a contract rather than a detail.
/// </summary>
internal static class ExitCodes {

    /// <summary>The documentation model was extracted and written.</summary>
    internal const int Success = 0;

    /// <summary>The extraction failed: the target would not load, or a documentation factory threw.</summary>
    internal const int ExtractionError = 1;

    /// <summary>The worker was called wrongly: a missing assembly path, or an unusable <c>--culture</c>.</summary>
    internal const int BadUsage = 2;

}
