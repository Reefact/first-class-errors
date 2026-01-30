#region Usings declarations

using System.Reflection;

using DiagnosableExceptions.Generation;
using DiagnosableExceptions.Usage.Model;

#endregion

namespace DiagnosableExceptions.UnitTests {

    public class AssemblyErrorDocumentationReaderTests {

        [Fact]
        public void Test1() {
            // Setup
            Assembly assembly = Assembly.GetAssembly(typeof(Temperature))!;

            // Exercise
            IEnumerable<ErrorDocumentation> documentation = AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly);

            // Verify
            ErrorDocumentation[] errorDocumentations = documentation.ToArray();

            // TODO: improve test
            Assert.NotNull(errorDocumentations);
        }

    }

}