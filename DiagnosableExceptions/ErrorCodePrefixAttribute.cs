namespace Reefact.DiagnosableExceptions;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ErrorCodePrefixAttribute : Attribute {

    #region Constructors declarations

    public ErrorCodePrefixAttribute(string value) {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    #endregion

    public string Value { get; }

}