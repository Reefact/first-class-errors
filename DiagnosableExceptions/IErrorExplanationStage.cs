namespace Reefact.DiagnosableExceptions;

public interface IErrorExplanationStage {

    IErrorBusinessRuleStage WithExplanation(string explanation);

}