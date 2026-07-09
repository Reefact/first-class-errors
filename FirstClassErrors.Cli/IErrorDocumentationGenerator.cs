#region Usings declarations

using FirstClassErrors.GenDoc;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     The generate command's port over the error-documentation generation pipeline. It mirrors the two entry points
///     of <see cref="SolutionErrorDocumentationGenerator" /> so the command depends on this abstraction rather than on
///     the static generator; tests substitute a fake to exercise the command without spawning real <c>dotnet</c>
///     processes.
/// </summary>
internal interface IErrorDocumentationGenerator {

    IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(string solutionPath, SolutionGenerationOptions options);

    IEnumerable<ErrorDocumentation> GetErrorDocumentationFromAssemblies(IReadOnlyList<string> assemblyPaths, SolutionGenerationOptions options);

}
