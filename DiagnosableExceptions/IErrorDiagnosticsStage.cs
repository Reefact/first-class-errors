namespace Reefact.DiagnosableExceptions;

public interface IErrorDiagnosticsStage {

    IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics);
    IErrorExamplesStage WithNoDiagnostic();

}