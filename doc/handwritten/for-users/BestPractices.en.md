# Best Practices

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./BestPractices.fr.md)

Use this page as a compact review checklist. Detailed explanations belong in the focused guides linked from each section.

## Model the right situation

- One factory represents one precise error situation.
- Avoid generic catch-all errors such as `INVALID_OPERATION` or `PROCESSING_FAILED`.
- Choose the error type from the meaning of the failure, not from the current class or folder.
- Do not document framework exceptions, accidental crashes, or low-level technical noise as application knowledge.

See [Writing Error Documentation](WritingErrorsGuide.en.md) and [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md).

## Keep identifiers stable

- Use a specific `UPPER_SNAKE_CASE` error code.
- Never reuse a code for a different situation.
- Do not rename or remove a code casually.
- Keep context-key names and value types stable when dashboards or consumers depend on them.

Codes and context shape form an operational contract. Use [Catalog Versioning](CatalogVersioning.en.md) to make contract changes visible.

## Centralize construction in factories

Prefer:

```csharp
throw InvalidAmountOperationError.CurrencyMismatch(left, right).ToException();
```

Do not assemble errors or exceptions inline in business logic.

A dedicated static factory class annotated with `[ProvidesErrorsFor(...)]` should group related situations, with one factory method per situation and its documentation nearby.

This keeps the happy path readable and gives every occurrence the same code, messages, context, and documentation anchor.

## Separate stable documentation from runtime messages

Stable documentation explains the error category:

- title;
- description;
- violated rule;
- diagnostic hypotheses;
- representative examples.

Runtime messages explain one occurrence:

- `ShortMessage`: safe public summary;
- `DetailedMessage`: optional controlled public detail;
- `DiagnosticMessage`: internal diagnostic detail.

Do not put occurrence-specific identifiers into stable documentation, and do not expose internal diagnostic detail through public messages.

See [Writing Error Documentation](WritingErrorsGuide.en.md) and [Writing Error Messages](WritingErrorMessages.en.md).

## Write for investigation, not blame

- Causes describe plausible states or conditions.
- `ErrorOrigin` classifies where a cause may lie; it does not assign responsibility.
- Analysis leads start with neutral guidance such as *Check*, *Verify*, or *Review*.
- Do not encode ticketing, escalation, or team-contact procedures in error documentation.

Operational processes change independently from application behavior.

## Keep context useful and safe

- Add context at factory level so every occurrence is consistent.
- Use named, typed, reusable keys.
- Include instance-level facts that materially improve diagnosis.
- Avoid secrets, oversized payloads, and data that cannot be logged safely.
- Prefer structured context to embedding every value inside a message.

See [Error Context](ErrorContext.en.md).

## Choose exceptions or `Outcome` intentionally

Use an exception when the failure should interrupt the current operation, such as an invariant violation or an unrecoverable state at that level.

Use `Outcome` / `Outcome<T>` when failure is an expected branch of the flow, such as validation, parsing, batch processing, or partial success.

Both paths should carry the same `Error` created by the same factory. Do not create a second, weaker error model for non-throwing flows.

See [Usage Patterns](UsagePatterns.en.md).

## Make examples educational

- Use simple, realistic values.
- Make the violated rule immediately visible.
- Call the documented factory rather than copying a message.
- Keep boundary and stress cases in tests, not in catalog examples.

## Pull-request checklist

Before merging an error-related change, verify that:

- [ ] each new factory represents one precise situation;
- [ ] every code is specific, stable, and unique;
- [ ] documentation is linked with `[DocumentedBy]`;
- [ ] public messages contain no internal or sensitive information;
- [ ] diagnostic messages explain concrete occurrences;
- [ ] queryable occurrence data uses typed context where appropriate;
- [ ] diagnostics are hypotheses and analysis leads are actionable;
- [ ] examples are realistic and call the real factory;
- [ ] English and French documentation remain aligned when both are changed;
- [ ] catalog baseline changes (the committed catalog snapshot — see [Catalog Versioning](CatalogVersioning.en.md)) are deliberate and reviewed when the contract changes.

---

<div align="center">
<a href="UsagePatterns.en.md">← Usage Patterns</a> · <a href="../../../README.md#-documentation">↑ Table of contents</a> · <a href="Testing.en.md">Testing Guide →</a>
</div>

---