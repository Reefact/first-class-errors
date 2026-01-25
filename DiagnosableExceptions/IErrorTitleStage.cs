namespace Reefact.DiagnosableExceptions;

public interface IErrorTitleStage {

    IErrorExplanationStage WithTitle(string title);

}