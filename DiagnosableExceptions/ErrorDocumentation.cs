namespace Reefact.DiagnosableExceptions;

public sealed class ErrorDocumentation {

    #region Constructors & Destructor

    public ErrorDocumentation(Type exceptionType, string factoryMethodName) {
        ExceptionType     = exceptionType;
        FactoryMethodName = factoryMethodName;
    }

    #endregion

    public Type   ExceptionType     { get; }
    public string FactoryMethodName { get; }

    public string Code         { get; internal set; } = null!;
    public string Title        { get; internal set; } = null!;
    public string Explanation  { get; internal set; } = null!;
    public string BusinessRule { get; internal set; } = null!;

    public IReadOnlyList<ErrorDiagnostic>  Diagnostics { get; internal set; } = [];
    public IReadOnlyList<ErrorDescription> Examples    { get; internal set; } = [];

}