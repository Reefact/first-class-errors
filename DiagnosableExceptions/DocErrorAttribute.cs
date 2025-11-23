namespace Reefact.DiagnosableExceptions;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DocErrorAttribute : Attribute {

    public string            Title                { get; init; } = string.Empty;
    public string            Explanation          { get; init; } = string.Empty;
    public ErrorDiagnostic[] Diagnostic           { get; init; } = [];
    public Type?             DiagnosticType       { get; init; }
    public string?           DiagnosticMemberName { get; init; }

}