namespace Reefact.DiagnosableExceptions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ErrorExampleAttribute : Attribute {

    #region Constructors declarations

    public ErrorExampleAttribute(params object[] values) {
        ArgumentNullException.ThrowIfNull(values);

        Values = values;
    }

    #endregion

    public object[] Values { get; }

}