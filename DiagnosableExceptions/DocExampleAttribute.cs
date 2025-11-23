namespace Reefact.DiagnosableExceptions;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DocExampleAttribute : Attribute {

    #region Constructors declarations

    public DocExampleAttribute(object value) {
        Value = value;
    }

    #endregion

    public object Value { get; }

}