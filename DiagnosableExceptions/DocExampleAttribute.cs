namespace Reefact.DiagnosableExceptions;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DocExampleAttribute : Attribute {

    #region Constructors declarations

    public DocExampleAttribute(params object[] examples) {
        ArgumentNullException.ThrowIfNull(examples);

        Examples = examples;
    }

    #endregion

    public object[] Examples { get; }

}