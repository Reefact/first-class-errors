namespace FirstClassErrors.Cli;

/// <summary>
///     The production <see cref="IOutputSink" />: writes a single document to standard output (so the tool can be
///     piped) and writes files to disk, creating parent directories as needed. It carries none of the routing or
///     path-safety logic — that stays in the command; this adapter only performs the raw side effects.
/// </summary>
internal sealed class ConsoleAndFileOutputSink : IOutputSink {

    public void WriteStandardOutput(string content) {
        Console.Out.WriteLine(content);
    }

    public void WriteFile(string fullPath, string content) {
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }

}
