namespace Reefact.DiagnosableExceptions;

public interface IErrorExamplesStage {

    ErrorDocumentation WithExamples<TException>(params Func<TException>[] exampleFactories)
        where TException : DiagnosableException;

}