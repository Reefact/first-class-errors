namespace Reefact.DiagnosableExceptions;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DocErrorCodePrefixAttribute : Attribute {

    #region Constructors declarations

    public DocErrorCodePrefixAttribute(string value) {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    #endregion

    public string Value { get; }

}