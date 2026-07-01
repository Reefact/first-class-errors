namespace FirstClassErrors.GenDoc.Worker;

/// <summary>
///     Flat, serializable snapshot of an <see cref="ErrorDocumentationExtractionResult" />. This is the JSON contract
///     the worker writes and the generator reads. It deliberately carries only primitives/strings so that it survives
///     process boundaries and holds no reference to the target's load context (a documentation object could otherwise
///     capture the target's <see cref="System.Type" /> instances or example values).
/// </summary>
public sealed class ExtractionResultDto {

    public List<DocumentationDto> Documentation { get; set; } = new();

    public List<FailureDto> Failures { get; set; } = new();

    public static ExtractionResultDto From(ErrorDocumentationExtractionResult result) {
        return new ExtractionResultDto {
            Documentation = result.Documentation.Select(DocumentationDto.From).ToList(),
            Failures      = result.Failures.Select(FailureDto.From).ToList()
        };
    }

}

/// <summary>Serializable form of an <see cref="ErrorDocumentation" />.</summary>
public sealed class DocumentationDto {

    public string? Code         { get; set; }
    public string? Title        { get; set; }
    public string? Explanation  { get; set; }
    public string? BusinessRule { get; set; }
    public string? Source       { get; set; }

    public List<DiagnosticDto>   Diagnostics { get; set; } = new();
    public List<ExampleDto>      Examples    { get; set; } = new();
    public List<ContextEntryDto> Context     { get; set; } = new();

    public static DocumentationDto From(ErrorDocumentation doc) {
        return new DocumentationDto {
            Code         = doc.Code,
            Title        = doc.Title,
            Explanation  = doc.Explanation,
            BusinessRule = doc.BusinessRule,
            Source       = doc.Source,
            Diagnostics  = doc.Diagnostics.Select(DiagnosticDto.From).ToList(),
            Examples     = doc.Examples.Select(ExampleDto.From).ToList(),
            Context      = doc.Context.Select(ContextEntryDto.From).ToList()
        };
    }

}

/// <summary>Serializable form of an <see cref="ErrorDiagnostic" />.</summary>
public sealed class DiagnosticDto {

    public string? PossibleCause { get; set; }
    public string? Origin        { get; set; }
    public string? AnalysisHint  { get; set; }

    public static DiagnosticDto From(ErrorDiagnostic diagnostic) {
        return new DiagnosticDto {
            PossibleCause = diagnostic.PossibleCause,
            Origin        = diagnostic.Origin.ToString(),
            AnalysisHint  = diagnostic.AnalysisHint
        };
    }

}

/// <summary>Serializable form of an <see cref="ErrorDescription" />.</summary>
public sealed class ExampleDto {

    public string? DetailedMessage { get; set; }
    public string? ShortMessage    { get; set; }

    public static ExampleDto From(ErrorDescription example) {
        return new ExampleDto {
            DetailedMessage = example.DetailedMessage,
            ShortMessage    = example.ShortMessage
        };
    }

}

/// <summary>Serializable form of an <see cref="ErrorContextEntryDocumentation" />.</summary>
public sealed class ContextEntryDto {

    public string?       Key           { get; set; }
    public string?       ValueType     { get; set; }
    public string?       Description   { get; set; }
    public List<string?> ExampleValues { get; set; } = new();

    public static ContextEntryDto From(ErrorContextEntryDocumentation entry) {
        return new ContextEntryDto {
            Key           = entry.Key,
            ValueType     = entry.ValueType?.FullName ?? entry.ValueType?.Name,
            Description   = entry.Description,
            ExampleValues = entry.ExampleValues.Select(value => value?.ToString()).ToList()
        };
    }

}

/// <summary>Serializable form of an <see cref="ErrorDocumentationExtractionFailure" />.</summary>
public sealed class FailureDto {

    public string? TypeName   { get; set; }
    public string? MemberName { get; set; }
    public string? Message    { get; set; }
    public string? Exception  { get; set; }

    public static FailureDto From(ErrorDocumentationExtractionFailure failure) {
        return new FailureDto {
            TypeName   = failure.TypeName,
            MemberName = failure.MemberName,
            Message    = failure.Message,
            Exception  = failure.Exception?.ToString()
        };
    }

}
