#region Usings declarations

using DiagnosableExceptions.Generation;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests {

    public class Class1 {

        [Fact]
        public void Test01() {
            SolutionGenerationOptions solutionGenerationOptions = new() { BuildSolution = false };
            ErrorDocumentation[]      result                    = SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom("C:\\Users\\sylva\\source\\repos\\diagnosable-exceptions\\DiagnosableExceptions.sln", solutionGenerationOptions).ToArray();
            Check.That(result).CountIs(4);
        }

    }

}