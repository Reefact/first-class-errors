namespace FirstClassErrors;

internal sealed class ErrorDocumentationBuilder :
    IErrorTitleStage,
    IErrorDescriptionStage,
    IErrorRuleStage,
    IErrorDiagnosticsStage,
    IErrorExamplesOrDiagnosticsStage,
    IErrorExamplesStage {

    #region Statics members declarations

    private static IEnumerable<TError> ComputeErrors<TError>(Func<TError>[] exampleFactories)
        where TError : Error {
        for (int factoryIndex = 0; factoryIndex < exampleFactories.Length; factoryIndex++) {
            Func<TError>? factory = exampleFactories[factoryIndex];
            if (factory is null) { throw ErrorDocumentationException.ExampleFactoryIsNull(factoryIndex); }

            TError? error;
            try {
                error = factory();
            } catch (Exception ex) {
                throw ErrorDocumentationException.ExampleFactoryThrewAnException(factoryIndex, ex);
            }

            if (error is null) {
                throw ErrorDocumentationException.NullExample(factoryIndex);
            }

            yield return error;
        }
    }

    private static IEnumerable<ErrorContextEntryDocumentation> BuildContext<TError>(TError[] errors)
        where TError : Error {
        return errors
              .SelectMany(error =>
                              error.Context.Values.Select(kvp => new {
                                  kvp.Key.Name,
                                  kvp.Key.ValueType,
                                  kvp.Key.Description,
                                  ExampleValue = kvp.Value
                              }))
              .GroupBy(x => x.Name)
              .Select(g => new ErrorContextEntryDocumentation {
                   Key         = g.Key,
                   ValueType   = g.First().ValueType?.FullName ?? g.First().ValueType?.Name,
                   Description = g.First().Description,
                   ExampleValues = g.Select(x => x.ExampleValue)
                                    .Where(v => v != null)
                                    .Distinct()
                                    .Select(v => v?.ToString())
                                    .ToArray()
               });
    }

    #endregion

    #region Fields declarations

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
    public IErrorExamplesOrDiagnosticsStage AndDiagnostic(string cause, ErrorOrigin type, string analysisLead) {
        _diagnostics.Add(new ErrorDiagnostic(cause, type, analysisLead));

        return this;
    }

    public ErrorDocumentation WithExamples<TError>(params Func<TError>[] exampleFactories)
        where TError : Error {
        if (exampleFactories is null) { throw new ArgumentNullException(nameof(exampleFactories)); }
        if (exampleFactories.Length == 0) { throw ErrorDocumentationException.AtLeastOneExampleMustBeProvided(); }

        TError[] errors = ComputeErrors(exampleFactories).ToArray();

        _doc.Diagnostics = _diagnostics.ToArray();
        _doc.Examples    = BuildExamples(errors).ToArray();
        _doc.Context     = BuildContext(errors).ToArray();

        return _doc;
    }

    /// <inheritdoc />
    public IErrorExamplesStage WithDiagnostics(params ErrorDiagnostic[] diagnostics) {
        if (diagnostics is null) { throw new ArgumentNullException(nameof(diagnostics)); }

        // Reject null elements at the call site: a null diagnostic would otherwise flow into _doc.Diagnostics and only
        // surface much later as a NullReferenceException while a renderer reads its members. This mirrors the per-item
        // validation WithExamples performs on its factories.
        for (int diagnosticIndex = 0; diagnosticIndex < diagnostics.Length; diagnosticIndex++) {
            if (diagnostics[diagnosticIndex] is null) {
                throw new ArgumentException($"Diagnostic at index {diagnosticIndex} is null. All diagnostics must be valid instances.", nameof(diagnostics));
            }
        }

        _diagnostics.AddRange(diagnostics);

        return this;
    }

    private IEnumerable<ErrorDescription> BuildExamples<TError>(TError[] errors)
        where TError : Error {
        for (int exampleIndex = 0; exampleIndex < errors.Length; exampleIndex++) {
            TError error = errors[exampleIndex];

            if (_doc.Code != null && _doc.Code != error.Code) { throw ErrorDocumentationException.InconsistentErrorCode(exampleIndex, _doc.Code, error.Code); }

            _doc.Code = error.Code;

            yield return new ErrorDescription(error.ShortMessage, error.DiagnosticMessage, error.DetailedMessage);
        }
    }

    /// <inheritdoc />
    IErrorExamplesOrDiagnosticsStage IErrorDiagnosticsStage.WithDiagnostic(string cause, ErrorOrigin type, string analysisLead) {
        _diagnostics.Add(new ErrorDiagnostic(cause, type, analysisLead));

        return this;
    }

}