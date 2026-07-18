# ADR-0019 | Document overridden binder errors in the consumer's own catalog

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0019-document-overridden-binder-errors-in-the-consumers-catalog.fr.md)

**Status:** Accepted
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

* ADR-0018 (which superseded ADR-0016) made the binder's two structural errors configurable
  as a `BinderErrorDefinition` — code and public messages together — on
  `RequestBinderOptions`. Both ADRs deferred one question to issue #140: how a consumer's
  generated catalog surfaces the binder's — possibly overridden — codes without drifting
  from what it emits at runtime.
* The documentation generator documents an error from a static factory carrying
  `[DocumentedBy]` inside a `[ProvidesErrorsFor]` type: it runs the named documentation
  method (the prose) and the example it holds (a live error), reading the code, messages and
  context off the built error. It discovers these types by scanning the solution's opted-in
  projects; it does not scan referenced packages.
* The binder manufactures its two structural errors itself — their factories are internal —
  and already documents its own defaults in its own package catalog.
* When a consumer overrides a definition, it owns the effective code and messages: they live
  in the consumer's own assembly (the `BinderErrorDefinition` it injects into the options)
  and are runtime configuration, not statically discoverable from the binder's binary.
* The binder's structural-error prose (title, rule, diagnoses) is code-independent: it
  describes the meaning of "a required argument was missing" whatever code carries it.
* The library's coded-error model documents an error where it is defined, through
  `[ProvidesErrorsFor]` / `[DocumentedBy]`, and forbids referencing an error by a magic
  string.
* Exactly one package ships documented, emittable codes today (`FirstClassErrors.RequestBinder`),
  and the library is pre-release with no external consumers.

## Decision

A consumer surfaces its overridden binder structural errors in its own generated catalog by
documenting them in its own `[ProvidesErrorsFor]` type — built from public binder seams that
reuse the binder's code-independent prose and build a faithful example from the consumer's
definition — rather than the generator auto-discovering a referenced package's codes.

## Rationale

* The consumer owns the effective codes (ADR-0018), so documenting them in its own catalog —
  where the coded-error model documents every other owned error — keeps one consistent rule
  instead of a special cross-package path.
* Reusing the binder's code-independent prose through public seams means the consumer writes
  no description text and constructs no error by hand: the mechanical facts (code, messages,
  transience, the argument-path context key, inner-error wiring) come from the binder building
  the sample the same way it does at binding time, so the documented entry cannot drift from
  what is emitted — closing the exact risk #140 named.
* The generator needs no change: the consumer's catalog is an ordinary
  `[ProvidesErrorsFor]` / `[DocumentedBy]` type in an already-scanned project. This avoids the
  machinery an auto-discovery step would need — resolving a project's reference closure, a
  metadata pre-check to skip non-documenting assemblies, running the worker over third-party
  binaries, a documentation-contract-version gate, and a code-collision policy — and its
  correctness hazards, since a referenced package is not always emitted on the public surface
  and its defaults are wrong for a consumer that overrode them.
* Keeping the link between an error and its description in code, through `nameof` / `typeof`,
  leaves it under the compiler — consistent with the library's stance against magic strings —
  where a link expressed in build configuration would reintroduce them.
* With one documented-code package and no consumers, the small per-consumer glue is an
  acceptable price, and the options surface and documentation seams are settled before any
  auto-discovery is committed to.

## Alternatives Considered

### Auto-discover a referenced package's documented codes

Considered because it is zero-boilerplate: a consumer references the package and its codes
appear in the consumer's catalog.

Rejected because it is unnecessary and misleading. The only package that ships emittable codes
is `FirstClassErrors.RequestBinder`, whose binary carries only the defaults — so auto-discovery
could never surface a consumer's overrides (these seams do), and for an overriding consumer it
would document the default codes it no longer emits. Scoping it to that one package removes none
of this: the limitation is that overrides live in the consumer's assembly, not the package's. It
would buy only sparing a zero-config consumer the small glue that documents the defaults.

### A build-configuration link binding an error to a description method

Considered because it moves the documentation wiring out of code into project configuration.

Rejected because it would reference members by string in project files — reintroducing the
magic strings the coded-error model exists to eliminate, with no compile-time check,
navigation, or refactoring safety. A compile-safe assembly attribute would be the fallback if
a declarative link is ever wanted.

## Consequences

### Positive

* The override half of #140 is closed: a consumer documents exactly what it emits, faithful to
  runtime, with the binder's prose and no change to the generator.
* The link between an error and its documentation stays compile-safe and in code, consistent
  with the library's magic-string stance.

### Negative

* A consumer folds the binder's codes into its catalog with a small per-error glue type (a
  `[ProvidesErrorsFor]` type delegating to the public seams); there is no zero-boilerplate
  auto-discovery.
* The binder grows four public members (a describe and a sample seam per structural error).
  The two sample seams return an error without `[DocumentedBy]` and are suppressed against
  FCE009 as deliberate non-catalog helpers.

### Risks

* A consumer could call a sample seam to manufacture a structural error outside documentation.
  It produces the same shape the binder emits and is injected nowhere, so it is inert — no
  different from building any error through the public `PrimaryPortError.Create`.

## Follow-up Actions

* None. Auto-discovery is not needed (see Alternatives): the one package that ships emittable
  codes is covered by these seams.

## References

* ADR-0018 — bundle the binder's structural error code and messages in one definition; the
  ownership this decision builds on.
* ADR-0016 — make the binder's structural error codes configurable (superseded); it first
  deferred this concern to #140.
* Issue #140 — document error codes from referenced FirstClassErrors packages in a consumer's
  catalog.
* FCE009 — `ErrorFactoryNotDocumented`, the analyzer the sample seams are suppressed against.
