# ADR-0018 | Bundle the binder's structural error code and messages in one definition

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0018-bundle-the-binders-structural-error-code-and-messages.fr.md)

**Status:** Proposed
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

* The binder manufactures exactly two structural errors of its own: a missing required
  argument (`REQUEST_ARGUMENT_REQUIRED`) and a present-but-invalid argument
  (`REQUEST_ARGUMENT_INVALID`). ADR-0016 made their **codes** configurable on
  `RequestBinderOptions`, resolving issue #147.
* Each of these two errors also carries **public messages** — a short summary and an
  optional detail — surfaced to an end user or an API client. Before this decision those
  messages were hardcoded English literals with no override or localization hook (issue
  #149).
* The rest of the library localizes public messages by the internationalization pattern:
  an error's factory resolves culture-specific resources **at the point the error is
  built**, so the message follows the ambient culture of the request; the diagnostic
  message stays in one internal language by convention.
* The binder manufactures these two errors itself — their factories are internal — so a
  consumer cannot inject localized strings the way it does when it writes its own errors'
  factories. This is the gap that breaks the library's i18n story at the primary-adapter
  boundary.
* `RequestBinderOptions` is the binder's single, immutable, entry-point configuration,
  fixed once before binding begins (ADR-0012) and inherited by nested binders; its
  application-wide default freezes on first use.
* A code and its message are one concept in the library's coded-error model. Exposing them
  as two independent knobs lets a consumer override one and forget the other, producing a
  code stranded from a message that no longer fits it.
* The library is pre-release (`v0.1.0-preview.1`), unpublished, with no external consumers,
  so reshaping the options surface carries no migration cost — and the shape should be
  settled before the v1 API freeze.

## Decision

Each of the binder's two structural errors is configured on `RequestBinderOptions` as a
single definition bundling its error code with a message builder evaluated when the error
is raised — superseding the code-only override of ADR-0016 and making the public messages
localizable per request.

## Rationale

* Bundling the code with its message keeps the coded-error model coherent: the two facets
  that together present a structural failure to a consumer are configured as one unit, so
  an override can never strand a code from its message — the failure mode the two
  independent knobs allowed.
* Reusing `RequestBinderOptions` (fixed once, inherited by nested binders, ADR-0012) keeps
  one place and one lifetime for all binder configuration, the same mechanism the naming
  policy and ADR-0016's codes already use.
* Making the message a **builder evaluated at emission**, rather than a stored string, is
  what lets one host serve several languages: it reads the ambient culture per request,
  matching the library's pattern of resolving messages when the error is built. A message
  captured at options-construction time would fix one language, and the options'
  freeze-on-first-use lifetime would make that permanent — defeating the very requirement.
* Localizing only the public messages, and leaving the diagnostic in one internal language,
  preserves the convention that logs for one error type do not fork by request language.
* Defaulting each definition to the shipped code and its English messages keeps the
  zero-configuration behaviour unchanged and the binder package's own catalog accurate; a
  consumer that overrides nothing is unaffected, and the defaults are exposed so a consumer
  can still branch on a structural code symbolically.
* Superseding ADR-0016's code-only override, rather than adding a parallel message knob, is
  free of migration cost while the library is pre-release, and settles the options surface
  as one concept before the v1 freeze.

## Alternatives Considered

### A separate message hook alongside the configurable codes

Considered because it is the smallest addition to the surface ADR-0016 already shipped.

Rejected because it re-creates exactly the decoupling the coded-error model exists to
avoid: a consumer could align the code and forget the message, or the reverse, and binder
configuration would split across two concepts for no gain.

### Store the overriding messages as fixed strings on the options

Considered because a plain string is simpler than a builder.

Rejected because the options freeze on first use, so stored strings would fix one language
for the process's lifetime — defeating per-request localization, the one property issue
#149 requires.

### Expose the message-building factories publicly, so a consumer manufactures the errors

Considered for symmetry with how a consumer localizes its own errors (it writes their
factories).

Rejected because it opens the binder's structural invariants — transience, the
argument-path context key, the inner-error wiring — to consumer error. The binder must keep
manufacturing these errors and expose only their presentation: the code and the public
messages.

## Consequences

### Positive

* The binder's structural code and messages are overridden as one coherent unit, and a
  consumer serving non-English clients localizes them per request — closing issue #149.
* All binder configuration stays on one options object with one lifetime (ADR-0012),
  inherited by nested binders.
* Zero-configuration behaviour and the binder package's own catalog are unchanged (defaults
  preserved and still documented, and still exposed for symbolic branching).

### Negative

* The options surface changes shape: the code-only override recorded by ADR-0016 is
  replaced by a code-and-message definition. A breaking change, acceptable pre-release.
* The diagnostic message stays non-overridable and English by convention; a consumer that
  wants a localized diagnostic is deliberately not served.

### Risks

* A consumer's message builder runs during error manufacturing and could throw, unlike the
  library's own always-safe factories. This matches the existing extension-point contract
  (the argument-name provider is consumer code invoked during binding too) and is the
  consumer's responsibility.

## Follow-up Actions

* On acceptance, decide whether ADR-0016 becomes `Superseded` by this ADR — the maintainer's
  call, not the drafter's.
* The overridden-code catalog-drift question deferred by ADR-0016 (relates to #140) is
  unchanged by this decision.

## References

* ADR-0016 — make the binder's structural error codes configurable; the code-only override
  this decision supersedes.
* ADR-0012 — fix the binder options before binding begins; the options lifetime this
  decision reuses.
* Issue #149 — the finding this decision resolves.
* Issue #147 — the finding ADR-0016 resolved (the codes half).
* [Internationalization](../../for-users/Internationalization.en.md) — the library's
  localization pattern this decision follows.
