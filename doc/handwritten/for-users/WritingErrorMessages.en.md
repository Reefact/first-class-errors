# Writing Error Messages

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./WritingErrorMessages.fr.md)

An error carries three runtime messages, but they serve only two audiences:

- public messages for end users or API clients;
- an internal diagnostic message for logs, support, and developers.

Keeping those audiences separate prevents internal information from leaking while preserving enough detail to investigate a concrete occurrence.

## The three messages at a glance

| Message | Required | Audience | Purpose |
| --- | --- | --- | --- |
| `ShortMessage` | yes | public | a safe, concise summary |
| `DetailedMessage` | no | public | an optional controlled explanation |
| `DiagnosticMessage` | yes | internal | concrete diagnostic information for this occurrence |

They are created through a staged builder — a fluent builder whose steps require the diagnostic message and the public short message before an `Error` can exist:

```csharp
return DomainError.Create(
        Code.CurrencyMismatch,
        diagnosticMessage: $"Cannot combine {left.Currency} and {right.Currency} amounts: {left} and {right}.")
    .WithPublicMessage(
        shortMessage: "The amounts use different currencies.",
        detailedMessage: "All amounts in this operation must use the same currency.");
```

The builder requires the internal message first and cannot produce an `Error` until the mandatory public short message is supplied.

## `ShortMessage`: the safe public summary

The short message tells the caller what happened without exposing implementation details.

It should be:

- safe to display directly;
- understandable without internal knowledge;
- concise enough for a UI notification or the `title` of an RFC 9457 (Problem Details for HTTP APIs) response;
- stable in meaning, although its wording may evolve or be localized.

Good:

> “The amounts use different currencies.”

Avoid:

> “Currency validation failed in `Amount.AddOrThrow` for order 42.”

The second example exposes implementation detail and an occurrence identifier that does not belong in a reusable public summary.

## `DetailedMessage`: optional controlled public detail

The detailed message gives the caller additional information when the application deliberately chooses to expose it.

It may explain:

- which public constraint was not satisfied;
- what kind of correction is expected from the caller;
- which safe input category caused rejection.

Good:

> “All amounts in this operation must use the same currency.”

Avoid:

> “The EUR amount was read from PostgreSQL while the USD amount came from provider X.”

The detailed message remains public. It must not contain secrets, internal topology, stack traces, database details, private identifiers, or support-only instructions.

Omit it when it would merely repeat the short message.

## `DiagnosticMessage`: the internal occurrence detail

The diagnostic message is intended for developers, support, and logs. It describes the concrete occurrence rather than the generic error definition.

It may include useful internal facts such as:

- identifiers needed for correlation;
- offending values, when safe for internal logs;
- dependency names;
- timeouts or response codes;
- expected and actual state.

Good:

> “Cannot combine EUR 127.33 and USD 84.10 in operation ORDER-7392.”

Avoid messages that remain too generic:

> “Currency mismatch.”

The stable error documentation already says what a currency mismatch means. The diagnostic message should explain what was distinctive about this occurrence.

`error.ToException()` uses `DiagnosticMessage` as the resulting exception’s `Message`.

## Public does not mean harmless by default

Before putting data in either public message, ask:

1. Could this reveal personal, financial, security, or tenancy-specific information?
2. Could it expose an internal service, database, file path, class, or implementation choice?
3. Could the wording help an attacker distinguish internal states that should remain indistinguishable?
4. Would the same text still be appropriate in a client-visible API response?

When in doubt, keep the detail in `DiagnosticMessage` or structured `ErrorContext`, subject to the application’s logging policy.

## Messages and `ErrorContext` have different roles

Do not turn the diagnostic message into an unstructured data dump.

Use the message to provide a readable summary:

```text
Order ORDER-7392 cannot be charged because the payment provider timed out.
```

Use typed context for fields that logs, dashboards, or tooling must query:

```csharp
configureContext: ctx => ctx
    .Add(ErrCtxKey.OrderId, orderId)
    .Add(ErrCtxKey.Provider, providerName)
```

The two complement each other:

- `DiagnosticMessage` helps a human read the event;
- `ErrorContext` helps humans and tools filter, correlate, and aggregate it.

See [Error Context](ErrorContext.en.md) for key design and sensitivity guidance.

## HTTP and RFC 9457

RFC 9457 defines “Problem Details”, the standard error-response format for HTTP APIs. The core error model is HTTP-agnostic. An application may map:

| FirstClassErrors value | Typical RFC 9457 field |
| --- | --- |
| stable `Code` represented as a URI | `type` |
| `ShortMessage` | `title` |
| `DetailedMessage`, when explicitly exposed | `detail` |

For example:

```json
{
  "type": "urn:problem:billing-api:amount-currency-mismatch",
  "title": "The amounts use different currencies.",
  "detail": "All amounts in this operation must use the same currency.",
  "status": 422
}
```

The `urn:problem:{service}:{code}` form of `type` is a convention your application chooses (the catalog renderer uses the same one); RFC 9457 only requires a URI.

`DiagnosticMessage` is not a default response field. The application remains responsible for choosing `status`, deciding whether to expose `detail`, and enforcing its security policy.

## Localization

Public messages are suitable for localization because they address the caller. Read `ShortMessage` and `DetailedMessage` from resources under the selected UI culture when needed.

Keep `DiagnosticMessage` in the team’s invariant authoring language. A consistent internal language makes logs and support investigations easier to search and correlate across callers.

See [Internationalization](Internationalization.en.md) for the extraction and rendering flow.

## Bad and improved example

Too little separation:

```csharp
return DomainError.Create(
        Code.CurrencyMismatch,
        diagnosticMessage: "Invalid operation.")
    .WithPublicMessage(
        shortMessage: $"Order {orderId}: database currencies {left.Currency}/{right.Currency} do not match.",
        detailedMessage: "Contact the Payments team with the SQL trace.");
```

Improved:

```csharp
return DomainError.Create(
        Code.CurrencyMismatch,
        diagnosticMessage: $"Order {orderId} cannot combine {left.Currency} and {right.Currency} amounts: {left} and {right}.",
        configureContext: ctx => ctx.Add(ErrCtxKey.OrderId, orderId))
    .WithPublicMessage(
        shortMessage: "The amounts use different currencies.",
        detailedMessage: "All amounts in this operation must use the same currency.");
```

The improved version gives the caller safe information, gives support occurrence-specific detail, and keeps queryable data structured.

## Review checklist

Verify that:

- the short message is concise, public, and free of implementation detail;
- the detailed message adds useful public information rather than repeating the summary;
- neither public message contains sensitive or support-only data;
- the diagnostic message explains the concrete occurrence;
- queryable values are also captured as structured context where appropriate;
- the diagnostic message is never exposed externally by default;
- public messages are localized when the application supports multiple caller languages;
- internal diagnostic wording remains consistent across cultures.

For the stable title, description, rule, diagnostics, and examples, return to [Writing Error Documentation](WritingErrorsGuide.en.md).

---

<div align="center">
<a href="WritingErrorsGuide.en.md">← Writing Error Documentation</a> · <a href="../../../README.md#-next-steps">↑ Table of contents</a> · <a href="UsagePatterns.en.md">Usage Patterns →</a>
</div>

---