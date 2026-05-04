//#region Usings declarations

//using System.Diagnostics.CodeAnalysis;

//using JetBrains.Annotations;

//using NFluent;

//#endregion

//namespace DiagnosableExceptions.UnitTests;

//[Collection("SmartEnumSideEffects")]
//[TestSubject(typeof(InfrastructureException))]
//public sealed class InfrastructureExceptionTests : IDisposable {

//    #region Constructors declarations

//    public InfrastructureExceptionTests() {
//        ErrorContextKey.ResetForTests();
//        ErrorCode.ResetForTests();
//    }

//    #endregion

//    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
//    public void Dispose() {
//        ErrorContextKey.ResetForTests();
//        ErrorCode.ResetForTests();
//    }

//    [Theory(DisplayName = "An infrastructure exception preserves its transient classification.")]
//    [InlineData(Transience.Transient)]
//    [InlineData(Transience.NonTransient)]
//    [InlineData(Transience.Unknown)]
//    public void AnInfrastructureExceptionPreservesItsTransientClassification(Transience isTransient) {
//        // Setup
//        ErrorCode anyErrorCode = ErrorCodeFactory.CreateAny();
//        string    anyMessage   = ExceptionMessageFactory.CreateAnyMessage();

//        // Exercise
//        TestInfrastructureException exception = new(anyErrorCode, anyMessage, isTransient);

//        // Verify
//        Check.That(exception.Transience).IsEqualTo(isTransient);
//    }

//    [Theory(DisplayName = "An infrastructure exception with an inner exception preserves its transient classification.")]
//    [InlineData(Transience.Transient)]
//    [InlineData(Transience.NonTransient)]
//    [InlineData(Transience.Unknown)]
//    public void AnInfrastructureExceptionWithAnInnerExceptionPreservesItsTransientClassification(Transience isTransient) {
//        // Setup
//        ErrorCode anyErrorCode = ErrorCodeFactory.CreateAny();
//        string    anyMessage   = ExceptionMessageFactory.CreateAnyMessage();
//        Exception inner        = new InvalidOperationException("inner");

//        // Exercise
//        TestInfrastructureException exception = new(anyErrorCode, anyMessage, isTransient, inner);

//        // Verify
//        Check.That(exception.Transience).IsEqualTo(isTransient);
//    }

//    [Theory(DisplayName = "An infrastructure exception with multiple inner exceptions preserves its transient classification.")]
//    [InlineData(Transience.Transient)]
//    [InlineData(Transience.NonTransient)]
//    [InlineData(Transience.Unknown)]
//    public void AnInfrastructureExceptionWithMultipleInnerExceptionsPreservesItsTransientClassification(Transience isTransient) {
//        // Setup
//        ErrorCode anyErrorCode = ErrorCodeFactory.CreateAny();
//        string    anyMessage   = ExceptionMessageFactory.CreateAnyMessage();

//        Exception first  = new InvalidOperationException("first");
//        Exception second = new ArgumentException("second");

//        IEnumerable<Exception> innerExceptions = new[] { first, second };

//        // Exercise
//        TestInfrastructureException exception = new(anyErrorCode, anyMessage, isTransient, innerExceptions);

//        // Verify
//        Check.That(exception.Transience).IsEqualTo(isTransient);
//    }

//    #region Nested types declarations

//    //private static class aMessageFactory {

//        #region Statics members declarations

//        public static string CreateAnyMessage() {
//            return "boom";
//        }

//        #endregion

//    }

//    #endregion

//}

