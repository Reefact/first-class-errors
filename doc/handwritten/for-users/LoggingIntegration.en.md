# Integrate FirstClassErrors with Structured Logging

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
catch (DiagnosableException exception) {
    Error error = exception.Error;

    logger.LogError(
        exception,
        "Operation failed with {ErrorCode} ({ErrorInstanceId})",
        error.Code,
        error.InstanceId);
}
```

Passing the exception preserves the stack trace. Logging named properties preserves the stable semantic keys.

This logs the two most important keys inline, but it is not the whole error. The remaining properties — type, context, inner errors, and infrastructure fields — should be serialized through one shared projection rather than re-listed in every `catch`; see [Centralize the projection to a log model](#centralize-the-projection-to-a-log-model) below.

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

Context keys are operational vocabulary. Keep their names stable and ensure the values are safe for the intended log destination. A payment identifier logged as `PaymentId` must stay `PaymentId` in every error and every service — not drift to `Id` in one place, `PaymentIdentifier` in another, and `payment_id` in a third. A stable key is what lets a single query correlate the same fact across the whole system.

See [Error Context](ErrorContext.en.md) for key design and data-safety rules.

## Centralize the projection to a log model

Do not re-list error properties in every `catch`. Project an `Error` to a log model once, in one place, and reuse it everywhere a failure is logged.

The projection must be recursive: `DiagnosableException` does not use `Exception.InnerException` for the FirstClassErrors diagnostic tree — causes and aggregated failures live in `error.InnerErrors`. (A `DiagnosableException` may still carry a plain runtime exception — an I/O failure, for instance — on `Exception.InnerException`; that channel holds the technical cause with its stack trace, not modeled errors, so log it as you would any exception cause.) It must also add the infrastructure-specific fields when the error is an `InfrastructureError`:

```csharp
public static class ErrorLogModel {
    public static object From(Error error) {
        var model = new Dictionary<string, object?> {
            ["code"]              = error.Code.ToString(),
            ["instanceId"]        = error.InstanceId,
            ["occurredAt"]        = error.OccurredAt,
            ["type"]              = error.GetType().Name,
            ["diagnosticMessage"] = error.DiagnosticMessage,
            ["context"]           = error.Context.ToNameDictionary(),
            ["innerErrors"]       = error.InnerErrors.Select(From).ToArray()
        };

        if (error is InfrastructureError infrastructure) {
            model["direction"]  = infrastructure.Direction.ToString();
            model["transience"] = infrastructure.Transience.ToString();
        }

        return model;
    }
}
```

Every `catch` or outcome handler then logs the whole error in one call:

```csharp
catch (DiagnosableException exception) {
    logger.LogError(
        exception,
        "Operation failed with {ErrorCode} {@FirstClassError}",
        exception.Error.Code,
        ErrorLogModel.From(exception.Error));
}
```

`{@FirstClassError}` is [Serilog](https://github.com/serilog/serilog)'s destructuring syntax: it serializes the log model as structured data instead of calling `ToString()`. With `Microsoft.Extensions.Logging` alone, the object is captured but rendered by the active provider, so use a provider that serializes state (or `BeginScope`) to keep the fields queryable. The projection itself does not change with the logging framework — that is the point of centralizing it.

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

The primary mechanism is a structured log property such as `error.documentationUrl`, which any log query or dashboard can surface directly.

`Exception.HelpLink` is a secondary, tool-dependent option: only some tooling reads it, so set it as a convenience for those tools rather than as the main navigation path.

```csharp
exception.HelpLink = documentationUrl;
```

The generated catalog remains the source of explanation; the log stores the navigational link, not a duplicated copy of the documentation.

## Logging outcomes without throwing

An `Outcome` failure may never become an exception. Log its `Error` directly when the application decides the failure belongs in operational logs:

```csharp
Outcome<Receipt> outcome = checkout.Pay(order);

if (outcome.IsFailure) {
    logger.LogWarning(
        "Checkout failed with {ErrorCode} {@FirstClassError}",
        outcome.Error!.Code,
        ErrorLogModel.From(outcome.Error));
}
```

Do not throw solely to make the logging framework see the error. The same `ErrorLogModel.From` projection serves both paths — a thrown `DiagnosableException.Error` and a returned `Outcome` failure — so neither one re-lists the error's properties.

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
<a href="OperationalIntegration.en.md">← Generating and Publishing the Catalog</a> · <a href="../../../README.md#-documentation">↑ Table of contents</a> · <a href="CatalogVersioning.en.md">Catalog Versioning →</a>
</div>

---