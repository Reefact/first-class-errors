namespace Reefact.DiagnosableExceptions;

internal sealed class ErrorDocumentationBuilder :
    IErrorTitleStage,
    IErrorExplanationStage,
    IErrorBusinessRuleStage,
    IErrorDiagnosticsStage,
    IErrorExamplesStage {

    #region Fields

    private readonly ErrorDocumentation _doc;

    #endregion

    #region Constructors & Destructor

    public ErrorDocumentationBuilder(ErrorDocumentation doc) {
        if (doc is null) { throw new ArgumentNullException(nameof(doc)); }

        _doc = doc;
    }

    public ErrorDocumentationBuilder() {
        _doc = new ErrorDocumentation();
    }

    #endregion

    public IErrorExplanationStage WithTitle(string title) {
        if (string.IsNullOrWhiteSpace(title)) { throw new ArgumentException("Value cannot be null or whitespace.", nameof(title)); }

        _doc.Title = title;

        return this;
    }

    public IErrorBusinessRuleStage WithExplanation(string explanation) {
        _doc.Explanation = explanation;

        return this;
    }

    public IErrorDiagnosticsStage WithBusinessRule(string rule) {
        _doc.BusinessRule = rule;

        return this;
    }

    public IErrorDiagnosticsStage WithNoBusinessRule() {
        return this;
    }

    public IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics) {
        if (diagnostics is null) { throw new ArgumentNullException(nameof(diagnostics)); }

        _doc.Diagnostics = diagnostics;

        return this;
    }

    public IErrorExamplesStage WithNoDiagnostic() {
        return this;
    }

    public ErrorDocumentation WithExamples<TException>(params Func<TException>[] exampleFactories)
        where TException : DiagnosableException {
        if (exampleFactories is null) { throw new ArgumentNullException(nameof(exampleFactories)); }
        if (exampleFactories.Length == 0) { throw new ArgumentException("At least one example must be provided."); }

        _doc.Examples = exampleFactories
                       .Select(factory => {
                            TException? exception = factory();
                            _doc.Code = exception.ErrorCode; // TODO: Can do better ?

                            return new ErrorDescription { ShortMessage = exception.ShortMessage, DetailedMessage = exception.Message };
                        })
                       .ToArray();

        return _doc;
    }

}