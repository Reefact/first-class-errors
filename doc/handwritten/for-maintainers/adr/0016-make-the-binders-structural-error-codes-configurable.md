# ADR-0016 | Make the binder's structural error codes configurable

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0016-make-the-binders-structural-error-codes-configurable.fr.md)

**Status:** Proposed
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

* The binder manufactures exactly two structural errors of its own: a missing required
  argument (`REQUEST_ARGUMENT_REQUIRED`) and a present-but-invalid argument
  (`REQUEST_ARGUMENT_INVALID`). Every other error in a failure tree comes from the
  application — the converters' errors and the envelope.
* These two errors surface in the consumer's failure envelope, and therefore in the
  consumer's error surface and its generated catalog.
* Before this decision the two codes were hardcoded `private` constants and the factories
  were `internal`, so `RequestBindingError` exposed no member a consumer could reference:
  a consumer that needed to branch on a structural code — to map it to an HTTP status,
  for example — could only compare a string literal, the very thing the library's
  coded-error model exists to avoid.
* An application commonly gives all its error codes one convention (a shared prefix or
  scheme); two foreign-looking codes injected by the binder break that convention.
* `RequestBinderOptions` is the binder's single, immutable, entry-point configuration,
  fixed once before binding begins (ADR-0012) and inherited by nested binders.
* `ErrorCode` has value equality, so a consumer can branch on a code it holds
  symbolically rather than by string.
* The binder package's own generated catalog is produced statically from the documented
  factories, which describe the default codes.
* The library is pre-release, unpublished on NuGet with no external consumers, so moving
  where the codes are set carries no downstream migration cost.

## Decision

The binder's two structural error codes are carried on `RequestBinderOptions`, defaulting
to `REQUEST_ARGUMENT_REQUIRED` / `REQUEST_ARGUMENT_INVALID` (whose defaults are exposed
publicly), so a consumer overrides them once at the entry point and every structural
failure — including a nested binder's — uses the configured codes.

## Rationale

* Carrying the codes on the options bag lets a consumer align them with its catalog's
  convention, resolving the inconsistency a fixed foreign prefix would create — and it
  reuses the exact mechanism the naming policy already uses (options, fixed once at the
  entry point, inherited by nested binders, ADR-0012), so there is one place and one
  lifetime for all binder configuration.
* Overriding the codes solves the branching problem by *ownership* rather than by mere
  exposure: a consumer that sets the codes holds the `ErrorCode` symbols it branches on,
  so it never compares a string; a consumer that keeps the defaults branches on the
  publicly exposed defaults. Either way the coded-error model's promise — reference codes
  symbolically — is finally kept for the binder's own errors too.
* Keeping the factories `internal` while exposing only the codes matches how a consumer
  meets these errors: it recognises them (needs the code) but does not manufacture them
  (the binder does), so the manufacturing surface stays closed.
* Defaulting to the current codes and exposing them publicly keeps the zero-configuration
  behaviour unchanged and the binder package's own catalog accurate, since the documented
  factories still describe those defaults.
* The pre-release status means the options surface is settled now, when there are no
  consumers to migrate.

## Alternatives Considered

### Expose the two codes read-only, without an override

Considered because it is the smallest change that kills the magic-string branching: a
consumer references public `ErrorCode` constants instead of literals.

Rejected because it leaves the deeper inconsistency the Context describes — the binder
still imposes two foreign-prefixed codes on the consumer's catalog. Reading the codes
lets a consumer recognise them; it does not let the consumer make them fit.

### Predicates (`IsRequestArgumentRequired` / `IsRequestArgumentInvalid`)

Considered because they read intent-fully and encapsulate the comparison.

Rejected as less composable and still non-overridable: a boolean cannot key a
code-to-status map, and it does nothing for catalog consistency. Overridable `ErrorCode`s
on the options subsume both needs, and a predicate can always be layered on later.

### A code-naming policy abstraction (an `IBindingCodeProvider` mirroring `IArgumentNameProvider`)

Considered for symmetry with the argument-name policy.

Rejected as unnecessary surface: two `ErrorCode` properties express the same intent, and
a prefix is one `ErrorCode.Create` a consumer writes once; an interface would add a type
without adding capability.

## Consequences

### Positive

* The binder's structural codes can be aligned with the consumer's catalog convention,
  and are branched on symbolically (owned codes, or the public defaults) — closing both
  halves of the finding.
* All binder configuration lives in one options object with one lifetime (ADR-0012); the
  codes are inherited by nested binders exactly like the naming policy.
* Zero-configuration behaviour and the binder package's own catalog are unchanged (the
  defaults are preserved and still documented).

### Negative

* `RequestBinderOptions` grows two properties and two optional constructor parameters, and
  `RequestBindingError` grows two public default-code members to document.
* The **documentation** of an overridden code is a separate concern: the binder package's
  catalog documents the defaults, so a consumer that overrides the codes must surface the
  effective codes in its own catalog.

### Risks

* A consumer that overrides the codes at runtime but documents the defaults would drift
  between the emitted and the documented code; only partially mitigated here (the defaults
  stay documented) and deferred to the separate catalog-linking decision.

## Follow-up Actions

* Decide, separately, how a consumer's generated catalog surfaces the binder's — possibly
  overridden — codes without drifting from runtime (relates to #140).

## References

* ADR-0012 — fix the binder options before binding begins; the options lifetime this
  decision reuses.
* ADR-0006 — supply arbitrary test values from a single seedable source; the
  no-ambient-mutable-state stance the options approach respects.
* Issue #147 — the finding this decision resolves.
* Issue #140 — surfacing the binder's codes in a consumer's generated catalog (the
  deferred documentation half).
