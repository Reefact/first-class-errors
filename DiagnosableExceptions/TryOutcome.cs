namespace Reefact.DiagnosableExceptions;

public readonly struct TryOutcome<T> {

    public static TryOutcome<T> Success(T value) {
        return new TryOutcome<T>(value);
    }

    public static TryOutcome<T> Failure(Exception error) {
        if (error is null) { throw new ArgumentNullException(nameof(error)); }

        return new TryOutcome<T>(error);
    }

    public bool HasError => Error != null;

    public T?         Value { get; }
    public Exception? Error { get; }

    private TryOutcome(T value) {
        Value = value;
        Error = null;
    }

    private TryOutcome(Exception error) {
        Value = default;
        Error = error;
    }

}