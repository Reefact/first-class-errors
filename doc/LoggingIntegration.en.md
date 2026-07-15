# Logging and Operational Integration

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./LoggingIntegration.fr.md)

FirstClassErrors does not replace structured logging. It adds stable semantic information to the technical and execution context already present in logs.

This guide explains what to log, how to preserve inner errors, and how to connect an occurrence to the generated catalog.

For catalog generation and publication, see [Generating and Publishing the Error Catalog](OperationalIntegration.en.md).

## Three complementary layers

| Layer | Answers |
| --- | --- |
| structured log properties | what happened technically? |
| scopes (ambient logging scopes such as `ILogger.BeginScope`) and correlation identifiers | in which request, message, or workflow did it happen? |
| FirstClassErrors | what does this recognized failure mean? |

A useful production event combines all three rather than flattening everything into one message.

## Minimum error properties to log

For every `Error`, capture at least:

| Property | Purpose |
| --- | --- |
| `Code` | stable grouping and alerting key |
| `InstanceId` | identity of this specific occurrence |
| `OccurredAt` | UTC creation time of the error |
| runtime error type | domain, infrastructure, primary port, or secondary port |
| `DiagnosticMessage` | internal explanation for developers and support |
| `Context` | structured facts specific to the occurrence |
| inner errors | causal or aggregated diagnostic depth |

For `InfrastructureError`, also capture:

- `Transience`;
- `InteractionDirection`.

Do not expose `DiagnosticMessage` or unrestricted context in public API responses merely because they are present in logs.

## Example structured event

A serialized event might look like this:

```json
{
  "level": "Error",
  "message": "Payment authorization failed",
  "traceId": "91c1d1bda3be4d0c",
  "service": "checkout-api",
  "deploymentVersion": "2.4.0",
  "error": {
    "code": "PAYMENT_PROVIDER_UNAVAILABLE",
    "instanceId": "be1226ef-8464-4f88-9dca-9ab1f74da824",
    "occurredAt": "2026-07-14T13:40:52.184Z",
    "type": "SecondaryPortError",
    "direction": "Outgoing",
    "transience": "Transient",
    "diagnosticMessage": "The payment provider timed out after 5 seconds.",
    "context": {
      "PaymentId": "f646943f-eec1-46bb-8989-32a97cba60fa",
      "Provider": "ExamplePay"
    },
    "innerErrors": []
  }
}
```

The exact JSON schema belongs to the application or logging adapter. The important point is to keep fields structured and queryable.

## Log the `Error`, not only `Exception.Message`

A `DiagnosableException` — the exception type that `error.ToException()` produces — exposes its semantic model through `.Error`:

```csharp
catch (DiagnosableException exception)
{
    Error error = exception.Error;

    logger.LogError(
        exception,
        "Operation failed with {ErrorCode} ({ErrorInstanceId})",
        error.Code,
        error.InstanceId);
}
```

Passing the exception preserves the stack trace. Logging named properties preserves the stable semantic keys.

A formatter, enricher, filter, or middleware can then serialize the remaining `Error` properties consistently.

## Preserve context as structured data

Do not concatenate context into one diagnostic string:

```text
Payment f646... failed for provider ExamplePay
```

Prefer separate fields that can be filtered and aggregated:

```json
{
  "PaymentId": "f646943f-eec1-46bb-8989-32a97cba60fa",
  "Provider": "ExamplePay"
}
```

Context keys are operational vocabulary. Keep their names stable and ensure the values are safe for the intended log destination.

See [Error Context](ErrorContext.en.md) for key design and data-safety rules.

## Traverse `InnerErrors`

`DiagnosableException` does not use `Exception.InnerException` for the FirstClassErrors diagnostic tree. Causes and aggregated failures live in:

```csharp
exception.Error.InnerErrors
```

A logging adapter must traverse that collection explicitly:

```csharp
static object ToLogModel(Error error)
{
    return new
    {
        Code = error.Code.ToString(),
        error.InstanceId,
        error.OccurredAt,
        Type = error.GetType().Name,
        error.DiagnosticMessage,
        Context = error.Context,
        InnerErrors = error.InnerErrors.Select(ToLogModel).ToArray()
    };
}
```

This example shows the recursive shape; adapt context serialization and infrastructure-specific fields to the application.

If only the outer error is logged, the most useful cause may disappear from operational analysis.

## Keep the outer and inner meanings

Consider an incoming request rejected because a domain value cannot be created:

```text
PrimaryPortError: REQUEST_REJECTED
└── DomainError: AMOUNT_NEGATIVE
```

The outer error explains what happened at the boundary. The inner error explains the violated domain rule. Logging both preserves the complete diagnostic story.

Do not flatten the tree into one synthetic code or replace the outer error with the deepest cause.

## Correlate the occurrence

`InstanceId` identifies one error occurrence. It complements, but does not replace:

- trace and span identifiers;
- request or message identifiers;
- business identifiers stored in `ErrorContext`;
- deployment version.

A useful investigation path is:

```text
alert → traceId → error InstanceId → business context → catalog entry
```

Include the deployment version so support can open the catalog that matches the running code rather than the latest documentation by accident.

## Link logs to the catalog

A log event may include a documentation URL derived from the code and deployed catalog location:

```text
https://docs.mycompany/errors/releases/2.4.0/payment-provider-unavailable
```

The URL may be emitted as a structured property such as `error.documentationUrl`.

When an exception object is used by tooling that recognizes `Exception.HelpLink`, the application may also set it before logging or rethrowing:

```csharp
exception.HelpLink = documentationUrl;
```

The generated catalog remains the source of explanation; the log stores the navigational link, not a duplicated copy of the documentation.

## Logging outcomes without throwing

An `Outcome` failure may never become an exception. Log its `Error` directly when the application decides the failure belongs in operational logs:

```csharp
Outcome<Receipt> outcome = checkout.Pay(order);

if (outcome.IsFailure)
{
    logger.LogWarning(
        "Checkout failed with {ErrorCode} ({ErrorInstanceId})",
        outcome.Error!.Code,
        outcome.Error.InstanceId);
}
```

Do not throw solely to make the logging framework see the error. A shared serializer can handle both `Error` and `DiagnosableException.Error`.

## Choose the log level from operational impact

The existence of an `Error` does not automatically imply an error-level log.

Examples:

| Situation | Possible treatment |
| --- | --- |
| user input rejected as expected | information or warning, depending on policy |
| item rejected inside a normal batch | warning or metric, possibly sampled |
| transient outgoing dependency failure | warning or error, depending on retries and final outcome |
| exhausted retries causing request failure | error |
| domain failure already returned to a caller and fully expected | potentially no log at this layer |

Use `Transience`, `InteractionDirection`, application outcome, and repetition rate to inform the policy. Avoid duplicate logs at every layer of the call stack.

## Protect sensitive data

Before logging context or diagnostic messages, verify that they do not contain:

- secrets, passwords, tokens, or credentials;
- unrestricted personal data;
- full request or response payloads;
- payment data or other regulated values;
- unbounded collections or files.

The internal audience permits technical detail; it does not remove security, privacy, retention, or access-control obligations.

## Review checklist

Before approving a logging integration, verify that:

- the exception stack trace and the structured `Error` are both preserved when an exception exists;
- code, instance id, occurrence time, type, diagnostic message, and context are queryable fields;
- infrastructure direction and transience are logged;
- `InnerErrors` are traversed recursively;
- outcome failures can be logged without manufacturing exceptions;
- trace, business, occurrence, and deployment identifiers remain distinct;
- the log level reflects operational impact rather than merely the presence of an error;
- sensitive and unbounded data are excluded;
- documentation links target the catalog version matching the deployment;
- the same failure is not logged repeatedly by every layer.

---

<div align="center">
<a href="OperationalIntegration.en.md">← Generating and Publishing the Catalog</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="CatalogVersioning.en.md">Catalog Versioning →</a>
</div>

---