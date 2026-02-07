namespace DiagnosableExceptions;

internal sealed class ErrorDocumentationBuilder :
    IErrorTitleStage,
    IErrorDescriptionStage,
    IErrorRuleStage,
    IErrorDiagnosticsStage,
    IErrorExamplesOrDiagnosticsStage,
    IErrorExamplesStage {

    #region Fields declarations

    private readonly ErrorDocumentation    _doc;
    private readonly List<ErrorDiagnostic> _diagnostics = new();

    #endregion

    #region Constructors declarations

    public ErrorDocumentationBuilder(ErrorDocumentation doc) {
        if (doc is null) { throw new ArgumentNullException(nameof(doc)); }

        _doc = doc;
    }

    public ErrorDocumentationBuilder() {
        _doc = new ErrorDocumentation();
    }

    #endregion

    public IErrorDescriptionStage WithTitle(string title) {
        if (string.IsNullOrWhiteSpace(title)) { throw new ArgumentException("Value cannot be null or whitespace.", nameof(title)); }

        _doc.Title = title;

        return this;
    }

    public IErrorRuleStage WithDescription(string explanation) {
        if (explanation is null) { throw new ArgumentNullException(nameof(explanation)); }

        _doc.Explanation = explanation;

        return this;
    }

    public IErrorDiagnosticsStage WithRule(string rule) {
        if (rule is null) { throw new ArgumentNullException(nameof(rule)); }

        _doc.BusinessRule = rule;

        return this;
    }

    /// <inheritdoc />
    public IErrorDiagnosticsStage WithNoRule() {
        return this;
    }

    /// <inheritdoc />
    public IErrorExamplesStage WithNoDiagnostic() {
        return this;
    }

    /// <inheritdoc />
    public IErrorExamplesOrDiagnosticsStage AndDiagnostic(string cause, ErrorCauseType type, string analysisLead) {
        _diagnostics.Add(new ErrorDiagnostic(cause, type, analysisLead));

        return this;
    }

    public ErrorDocumentation WithExamples<TException>(params Func<TException>[] exampleFactories)
        where TException : DiagnosableException {
        if (exampleFactories is null) { throw new ArgumentNullException(nameof(exampleFactories)); }
        if (exampleFactories.Length == 0) { throw new ArgumentException("At least one example must be provided."); }

        _doc.Diagnostics = _diagnostics.ToArray();
        _doc.Examples = exampleFactories
                       .Select(factory => {
                            TException? exception = factory();
                            // TODO: revoir les exceptions levées
                            if (exception == null) { throw new Exception("Factory must not return null."); }
                            if (_doc.Code != null && _doc.Code != exception.ErrorCode) { throw new Exception("Factories return different error code."); }
                            _doc.Code = exception.ErrorCode;

                            return new ErrorDescription(exception.Message, exception.ShortMessage);
                        })
                       .ToArray();

        return _doc;
    }

    /// <inheritdoc />
    public IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics) {
        if (diagnostics is null) { throw new ArgumentNullException(nameof(diagnostics)); }

        _doc.Diagnostics = diagnostics;

        return this;
    }

    /// <inheritdoc />
    IErrorExamplesOrDiagnosticsStage IErrorDiagnosticsStage.WithDiagnostic(string cause, ErrorCauseType type, string analysisLead) {
        _diagnostics.Add(new ErrorDiagnostic(cause, type, analysisLead));

        return this;
    }

}