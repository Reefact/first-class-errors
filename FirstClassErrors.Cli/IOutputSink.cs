namespace FirstClassErrors.Cli;

/// <summary>
///     The generate command's port over the two output side effects it performs: writing a single document to standard
///     output, and writing a document to a file. Placing them behind this abstraction keeps the routing and path-safety
///     logic in the command testable with a recording fake, instead of writing to the real console and file system.
/// </summary>
internal interface IOutputSink {

    /// <summary>Writes <paramref name="content" /> to standard output, followed by a line terminator.</summary>
    void WriteStandardOutput(string content);

    /// <summary>Writes <paramref name="content" /> to <paramref name="fullPath" />, creating parent directories.</summary>
    void WriteFile(string fullPath, string content);

}
