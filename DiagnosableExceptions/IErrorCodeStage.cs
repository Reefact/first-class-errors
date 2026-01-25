namespace Reefact.DiagnosableExceptions;

public interface IErrorCodeStage {

    IErrorTitleStage WithCode(string code);

}