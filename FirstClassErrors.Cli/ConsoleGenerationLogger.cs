#region Usings declarations

using FirstClassErrors.GenDoc;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     Writes generation logs to standard error so that standard output carries only the rendered document (which
///     lets the tool be piped, e.g. <c>fce-gendoc --solution app.sln | jq</c>). Informational and debug lines are
///     emitted only in verbose mode; warnings and errors are always shown.
/// </summary>
internal sealed class ConsoleGenerationLogger : IGenerationLogger {

    private readonly bool _verbose;

    public ConsoleGenerationLogger(bool verbose) {
        _verbose = verbose;
    }

    public void Info(string message) {
        if (_verbose) { Console.Error.WriteLine($"info: {message}"); }
    }

    public void Debug(string message) {
        if (_verbose) { Console.Error.WriteLine($"debug: {message}"); }
    }

    public void Warning(string message) {
        Console.Error.WriteLine($"warning: {message}");
    }

    public void Error(string message) {
        Console.Error.WriteLine($"error: {message}");
    }

}
