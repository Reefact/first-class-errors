namespace Reefact.DiagnosableExceptions;

public interface IErrorBusinessRuleStage {

    IErrorDiagnosticsStage WithBusinessRule(string rule);
    IErrorDiagnosticsStage WithNoBusinessRule();

}