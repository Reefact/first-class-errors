namespace Reefact.DiagnosableExceptions;

public class ErrorContext {

    #region Constructors declarations

    public ErrorContext(int version, object content) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentNullException.ThrowIfNull(content);

        Version = version;
        Content = content;
    }

    #endregion

    public int    Version { get; }
    public object Content { get; }

}