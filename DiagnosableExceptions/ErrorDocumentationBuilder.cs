namespace DiagnosableExceptions;

internal sealed class ErrorDocumentationBuilder :
    IErrorTitleStage,
    IErrorDescriptionStage,
    IErrorRuleStage,
    IErrorDiagnosticsStage,
    IErrorExamplesOrDiagnosticsStage,
    IErrorExamplesStage {

    #region Static members

    private static IEnumerable<TException> ComputeExceptions<TException>(Func<TException>[] exampleFactories)
        where TException : DiagnosableException {
        return exampleFactories.Select(factory => factory() ?? throw new Exception("Factory must not return null."));
    }

    private static IEnumerable<ErrorContextEntryDocumentation> BuildContext<TException>(TException[] exceptions)
        where TException : DiagnosableException {
        return exceptions
              .SelectMany(exception =>
                              exception.Context.Values.Select(kvp => new {
                                  kvp.Key.Name,
                                  kvp.Key.ValueType,
                                  kvp.Key.Description,
                                  ExampleValue = kvp.Value
                              }))
              .GroupBy(x => x.Name)
              .Select(g => new ErrorContextEntryDocumentation {
                   Key         = g.Key,
                   ValueType   = g.First().ValueType,
                   Description = g.First().Description,
                   ExampleValues = g.Select(x => x.ExampleValue)
                                    .Where(v => v != null)
                                    .Distinct()
                                    .ToArray()
               });
    }

    #endregion

    #region Fields

    private readonly ErrorDocumentation    _doc;
    private readonly List<ErrorDiagnostic> _diagnostics = new();

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
    public IErrorDiagnosticsStage WithoutRule() {
        return this;
    }

    /// <inheritdoc />
    public IErrorExamplesStage WithoutDiagnostic() {
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

        TException[] exceptions = ComputeExceptions(exampleFactories).ToArray();

        _doc.Diagnostics = _diagnostics.ToArray();
        _doc.Examples    = BuildExamples(exceptions).ToArray();
        _doc.Context     = BuildContext(exceptions).ToArray();

        return _doc;
    }

    /// <inheritdoc />
    public IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics) {
        if (diagnostics is null) { throw new ArgumentNullException(nameof(diagnostics)); }

        _doc.Diagnostics = diagnostics;

        return this;
    }

    private IEnumerable<ErrorDescription> BuildExamples<TException>(TException[] exceptions)
        where TException : DiagnosableException {
        return exceptions
           .Select(exception => {
                if (_doc.Code != null && _doc.Code != exception.ErrorCode) { throw new Exception("Factories return different error code."); }

                _doc.Code = exception.ErrorCode;

                return new ErrorDescription(exception.Message, exception.ShortMessage);
            });
    }

    /// <inheritdoc />
    IErrorExamplesOrDiagnosticsStage IErrorDiagnosticsStage.WithDiagnostic(string cause, ErrorCauseType type, string analysisLead) {
        _diagnostics.Add(new ErrorDiagnostic(cause, type, analysisLead));

        return this;
    }

}