#region Usings declarations

using FirstClassErrors.GenDoc;

#endregion

namespace FirstClassErrors.Cli;

/// <summary>
///     The production <see cref="IErrorDocumentationGenerator" />: a thin adapter that delegates to the static
///     <see cref="SolutionErrorDocumentationGenerator" />. It holds no state and adds no behavior; its only purpose is
///     to place the real generation pipeline behind the port the command depends on.
/// </summary>
internal sealed class SolutionErrorDocumentationGeneratorAdapter : IErrorDocumentationGenerator {

    public IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(string solutionPath, SolutionGenerationOptions options) {
        return SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(solutionPath, options);
    }

    public IEnumerable<ErrorDocumentation> GetErrorDocumentationFromAssemblies(IReadOnlyList<string> assemblyPaths, SolutionGenerationOptions options) {
        return SolutionErrorDocumentationGenerator.GetErrorDocumentationFromAssemblies(assemblyPaths, options);
    }

}
