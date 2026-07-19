# ADR-0021 | Bind out-of-DTO arguments as peers through a source-agnostic untyped entry

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

Primary adapters commonly assemble a command from several input origins: a request body, route values, query parameters, headers, claims, or message metadata.

The original Request Binder was DTO-first and had no natural place for an already-extracted value that did not belong to the DTO. Wrapping every such value in a synthetic DTO would distort its path and provenance.

All inputs must participate in the same collect-all binding result so failures from the body and from external arguments can be reported together with consistent structural errors and paths.

A DTO property can derive its name from reflection; an out-of-DTO argument must state its own path and can also carry provenance that distinguishes origins such as route, query, or header.

Typing the entry point on the eventual command conflicts with preserving method-group inference for nested bindings. The command type is only required when the final object is constructed.

## Decision

The Request Binder uses a source-agnostic untyped entry that declares the failure envelope first, attaches DTO property sources and individually named out-of-DTO argument sources as peers, and names the command type only at the `New` or `Create` terminal.

## Rationale

Treating sources as peers allows all failures to accumulate in one envelope regardless of where the host extracted the value.

The untyped entry removes a redundant command type from intermediate binding steps and preserves inference for nested bindings, while the terminal still states the constructed type through the assembler.

An argument must own its explicit path because no reflected property exists. Its provenance remains a separate typed diagnostic fact rather than being encoded into the path string.

Reusing the same converter vocabulary for properties and arguments preserves one mental model: sources differ in naming and provenance, not in validation or conversion.

Complex values assembled from multiple loose arguments are expressed by binding each argument as a peer and composing them at the terminal. The decision does not prohibit a future first-class complex argument if a concrete host-agnostic semantic emerges; it only declines to invent one without such a referent.

Exact entry types, source APIs, provenance helpers, generic signatures, and examples are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts) and the Request Binder guide.

## Alternatives Considered

### Keep the DTO-first entry and wrap arguments in synthetic DTOs

Considered because it reuses the existing path. Rejected because it adds ceremony and reports wrapper-property names rather than the caller's actual wire keys.

### Type the entry point on the command

Considered because it makes the target explicit early. Rejected because it degrades nested method-group inference and forces repeated explicit type arguments.

### Add a complex out-of-DTO argument immediately

Considered for symmetry with complex DTO properties. Rejected because a complex property represents a path into a DTO, while a loose argument has no equivalent object graph to traverse. Peer composition already covers construction from several loose values.

### Encode provenance into the error path

Considered to avoid a second context value. Rejected because path and origin answer different questions and should remain independently typed.

## Consequences

### Positive

* Body, route, query, header, and similar values bind together in one result.
* Nested binding retains method-group inference without explicit type arguments.
* Argument failures carry typed provenance without message parsing.
* Properties and arguments reuse the same converter model.

### Negative

* The entry-point shape differs from the DTO-first examples in earlier ADRs and documentation.
* The public surface now distinguishes property sources from argument sources.
* Callers must provide stable names and provenance conventions for loose arguments.

### Risks

* Provenance labels could drift across an application. Mitigation: provide and document standard shortcuts for common origins.
* Users may expect a complex-argument API from visual symmetry alone. Mitigation: document peer composition and revisit only from a concrete use case.

## Follow-up Actions

* Keep the bilingual Request Binder guide and package README aligned with the source-agnostic entry.
* Evaluate host-integration packages separately if consumers need framework-specific extraction rather than binding of already-extracted values.

## References

* [ADR implementation reference — Request Binder implementation contracts](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts)
* [ADR-0007](0007-name-the-binder-terminals-new-and-create.md) — terminal naming remains valid; illustrative entry shape updated by this ADR.
* [ADR-0012](0012-fix-the-binder-options-before-binding-begins.md) — options remain fixed at the source-agnostic entry; illustrative API shape updated by this ADR.
* [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) — the default remains valid; illustrative API shape updated by this ADR.
* Issue #148.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.
