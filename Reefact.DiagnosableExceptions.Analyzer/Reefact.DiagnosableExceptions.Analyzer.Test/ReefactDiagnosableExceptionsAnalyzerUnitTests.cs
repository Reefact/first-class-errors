#region Usings declarations

using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = Reefact.DiagnosableExceptions.Analyzer.Test.CSharpAnalyzerVerifier<Reefact.DiagnosableExceptions.Analyzer.UnusedErrorDocumentationMethodSuppressorAnalyzer>;

#endregion

namespace Reefact.DiagnosableExceptions.Analyzer.Test {

    [TestClass]
    public class ReefactDiagnosableExceptionsAnalyzerUnitTest {

        [TestMethod]
        public async Task TestMethod1() {
            string testCode = @"class C { private void M() { /* non utilisé */ } }";

            DiagnosticResult expected = VerifyCS.Diagnostic(DiagnosticId.IDE0051).WithSpan(3, 18, 3, 19);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2() {
            const string testCode = @"class C { [ErrorDocumentation] private void M() { /* non utilisé */ } }";
            // Aucune diagnostic attendu car l'avertissement doit être supprimé
            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

    }

}