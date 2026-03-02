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
        for (int factoryIndex = 0; factoryIndex < exampleFactories.Length; factoryIndex++) {
            Func<TException>? factory = exampleFactories[factoryIndex];
            if (factory is null) { throw ErrorDocumentationException.ExampleFactoryIsNull(factoryIndex); }

            TException? exception;
            try {
                exception = factory();
            } catch (Exception ex) {
                throw ErrorDocumentationException.ExampleFactoryThrewAnException(factoryIndex, ex);
            }

            if (exception is null) {
                throw ErrorDocumentationException.NullExample(factoryIndex);
            }

            yield return exception;
        }
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

    private readonly ErrorDocumentation    _doc         = new();
    private readonly List<ErrorDiagnostic> _diagnostics = new();

    #endregion

    public IErrorDescriptionStage WithTitle(string title) {
        if (title is null) { throw new ArgumentNullException(nameof(title)); }
        if (string.IsNullOrWhiteSpace(title)) { throw new ArgumentException("Value cannot be empty or whitespace.", nameof(title)); }

        _doc.Title = title.Trim();

        return this;
    }

    public IErrorRuleStage WithDescription(string explanation) {
        if (explanation is null) { throw new ArgumentNullException(nameof(explanation)); }

        _doc.Explanation = explanation.Trim();

        return this;
    }

    public IErrorDiagnosticsStage WithRule(string rule) {
        if (rule is null) { throw new ArgumentNullException(nameof(rule)); }

        _doc.BusinessRule = rule.Trim();

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
        if (exampleFactories.Length == 0) { throw ErrorDocumentationException.AtLeastOneExampleMustBeProvided(); }

        TException[] exceptions = ComputeExceptions(exampleFactories).ToArray();

        _doc.Diagnostics = _diagnostics.ToArray();
        _doc.Examples    = BuildExamples(exceptions).ToArray();
        _doc.Context     = BuildContext(exceptions).ToArray();

        return _doc;
    }

    /// <inheritdoc />
    public IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics) {
        if (diagnostics is null) { throw new ArgumentNullException(nameof(diagnostics)); }

        _diagnostics.AddRange(diagnostics);

        return this;
    }

    private IEnumerable<ErrorDescription> BuildExamples<TException>(TException[] exceptions)
        where TException : DiagnosableException {
        for (int exampleIndex = 0; exampleIndex < exceptions.Length; exampleIndex++) {
            TException exception = exceptions[exampleIndex];

            if (_doc.Code != null && _doc.Code != exception.ErrorCode) { throw ErrorDocumentationException.InconsistentErrorCode(exampleIndex, _doc.Code, exception.ErrorCode); }

            _doc.Code = exception.ErrorCode;

            yield return new ErrorDescription(exception.Message, exception.ShortMessage);
        }
    }

    /// <inheritdoc />
    IErrorExamplesOrDiagnosticsStage IErrorDiagnosticsStage.WithDiagnostic(string cause, ErrorCauseType type, string analysisLead) {
        _diagnostics.Add(new ErrorDiagnostic(cause, type, analysisLead));

        return this;
    }

}