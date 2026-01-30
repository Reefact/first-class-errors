namespace DiagnosableExceptions.Generation;

internal sealed class NullGenerationLogger : IGenerationLogger {

    public void Info(string    message) { }
    public void Warning(string message) { }
    public void Error(string   message) { }
    public void Debug(string   message) { }

}