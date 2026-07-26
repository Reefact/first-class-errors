# ADR-0035 | Enforce structural Any conflicts at compile time, value-dependent ones at run time

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0035-enforce-structural-any-conflicts-at-compile-time.fr.md)

**Status:** Accepted
**Date:** 2026-07-26
**Decision Makers:** Reefact

## Context

* Every generator on `Dummies`' `Any` entry point — and its `AnyContext` mirror — has until now been a
  **flat builder**: a single type exposes every constraint method, the methods chain in any order, and an
  incompatible combination is reported at **run time** by a `ConflictingAnyConstraintException` whose message
  names both sides ("Cannot apply X because Y is already defined"). A spec that only proves unsatisfiable while
  a value is being produced throws `AnyGenerationException`, which carries the seed. The type system is never
  used to prevent a combination.
* Two different kinds of incompatibility occur on that surface. One is **structural**: it holds for the
  combination itself, for every argument value — on `Any.String()`, a second character set after a first is
  always wrong. The other is **value-dependent**: the same method call is legal or illegal according to its
  argument's run-time value — `Any.String().Numeric().StartingWith("ORD-")` conflicts because the prefix's
  letters fall outside the numeric set, while `Any.String().Numeric().StartingWith("123")` is valid; the call
  site and the static types are identical in both.
* `Any.Uri()` (issue #226) is the first generator whose space is partitioned into structurally different
  **shapes**: an absolute web, WebSocket, FTP or mailto URI, or a relative reference. Each shape admits a
  different, RFC-fixed set of components — a mailto has no port or authority (RFC 6068), a WebSocket URI no
  user-info or fragment (RFC 6455), an FTP URI no query or fragment, a relative reference no scheme or
  authority. Which components are legal is fixed by the shape, not by any value.
* A category error across those shapes — a port on a mailto, a fragment on a WebSocket URI — is therefore
  structural in the sense above, and known before any value is drawn.
* C# can make a member unavailable on a type. A generator that returns a **different type per shape**, each
  exposing only that shape's components, turns a category error into code that does not compile, whereas a
  single flat `AnyUri` exposing every component could only reject the same error at run time.
* `Dummies` is pre-release: no `dum-v*` tag, no external consumers, an empty *Unreleased* changelog. The shape
  of its public generator surface can still be set at no migration cost.
* The repository records decisions that shape the `Any` public surface as ADRs — ADR-0020 (materialize only
  through `Generate()`), ADR-0031 (name factories after their CLR type), ADR-0006 (a single seeded source). A
  new, cross-cutting rule for *how* the surface reports an illegal combination is a decision of that same class.

## Decision

An illegal constraint combination on the `Any` surface is made unrepresentable at compile time — through a
typed progression that returns a shape-specific builder exposing only that shape's members — when the
illegality is structural, and is otherwise left to the run-time `ConflictingAnyConstraintException` /
`AnyGenerationException` path when it depends on a generated value.

## Rationale

* The dividing line is decidability by the compiler, and it falls exactly where the two kinds of incompatibility
  from Context already fall. A structural error is a property of the combination, so the type system *can* carry
  it; a value-dependent error is a property of an argument the compiler never sees, so the type system *cannot*
  carry it and a run-time check is the only option. The rule follows the grain of what each enforcement point is
  able to know.
* Applying typed progression to the value-dependent case is not merely unhelpful, it is impossible: no
  arrangement of types tells `StartingWith("ORD-")` from `StartingWith("123")`, because they differ only in a
  value. The flat, run-time pattern is therefore not a weaker fallback there — it is the only mechanism that can
  express the constraint at all.
* Conversely, leaving a structural URI error to run time throws away a guarantee that is freely available.
  `Mailto().WithPort(...)` is wrong for every possible argument; surfacing it as a failed generation, or even as
  a thrown `ConflictingAnyConstraintException`, defers to run time an error the compiler would otherwise catch at
  the keystroke, for no gain.
* Making category errors unrepresentable also removes them from the surface a reader must learn: a shape-specific
  builder that never offers `WithPort` cannot be misused that way, so the RFC rule "a mailto has no port" is
  taught by the API rather than by a run-time message. This is the same "make the rule un-break-able rather than
  merely checked" reasoning ADR-0031 applied to factory naming.
* The cost of the typed path — several public builder types for a family instead of one — is the kind of
  one-time surface decision the pre-release window absorbs for free, and it is confined to generators whose space
  genuinely splits into fixed shapes; the flat pattern stays the default everywhere else, so the surface does not
  fragment builder by builder.

## Alternatives Considered

### Keep every generator flat and report all conflicts at run time

Considered because it is the library's established pattern, gives one uniform mental model ("chain freely, learn
the conflicts from exceptions"), and keeps the smallest public type count — a single `AnyUri` instead of a
family.

Rejected because it spends a guarantee it need not spend: a category error such as a port on a mailto is knowable
at compile time, and a run-time-only surface can at best throw for it after the code already builds and runs.
Uniformity would be preserved in the wrong place — making the compiler-decidable error behave like the
value-dependent one, when only the latter is genuinely forced to run time.

### Make every generator a typed progression

Considered for symmetry — one enforcement model across the whole `Any` surface — and because it would move more
errors to compile time in general.

Rejected because most conflicts on the surface are value-dependent (prefixes, contained values, exclusions,
length interplay), which no type arrangement can decide; forcing types onto them cannot work, and would either
multiply builder types without removing a single run-time check or quietly narrow the surface below what the
generator is meant to express. Typed progression earns its cost only where a space splits into fixed shapes.

### Enforce the URI category rules with a Roslyn analyzer over a flat builder

Considered because the library already ships analyzers, so a diagnostic could flag `Mailto().WithPort()` on a
single flat `AnyUri` while keeping one type.

Rejected because it reintroduces, as an external check, an invariant the type system can hold intrinsically: an
analyzer can be suppressed, lags the compiler, and must be documented and tested as its own surface, whereas an
absent member simply cannot be written. An analyzer is the right tool for a *value-dependent* smell the types
cannot catch, not for a structural rule they can.

## Consequences

### Positive

* Category errors in a shape-partitioned generator become compile-time errors: `Mailto().WithPort(...)` and
  `WebSocket().WithFragment(...)` do not build, rather than failing when run.
* The legal component set of each URI shape is taught by that shape's own builder — the API is self-documenting
  where it used to rely on a run-time message.
* The rule states cleanly which enforcement point a new generator should use, keyed on a property (structural
  vs value-dependent) that is already the meaningful distinction on the surface.

### Negative

* The `Any` surface is no longer single-model: a contributor must recognise which of the two patterns a new
  generator calls for, instead of always reaching for the flat builder.
* A shape-partitioned generator carries several public builder types instead of one, enlarging the type count
  and the public-API baseline for that family.

### Risks

* The "structural vs value-dependent" line can be misjudged for a future generator — typing something whose
  conflicts are actually value-dependent (dead type surface), or leaving a genuinely structural split to run
  time (a missed compile-time guarantee); mitigated by keeping the flat, run-time pattern the default and
  reserving typed progression for a space that demonstrably splits into fixed shapes.
* Typed progression could be over-applied for its novelty, fragmenting the surface; mitigated by recording here
  that it is the exception — justified by a fixed-shape partition — not the new default.

## Follow-up Actions

* None required. `Any.Uri()` (issue #226, first application) already realises the typed-progression side, and
  the existing `AnyString` surface already realises the run-time side; this ADR records the rule they jointly
  establish.
* Apply the rule when a future generator's space splits into fixed shapes; otherwise keep the flat, run-time
  pattern.

## References

* ADR-0020 — materialize dummies only through `Generate()`; shares the "shape of the `Any` surface" subject.
* ADR-0031 — name Any's factories after their CLR type; precedent for "make the rule un-break-able rather than
  merely checked", and for recording `Any`-surface decisions as ADRs.
* ADR-0006 — supply arbitrary values from a single seedable source; the seed carried by `AnyGenerationException`
  on the run-time path.
* PR #295 — add the `Any.Uri()` family, the first typed progression.
* Issue #226 — the Dummies Nice-to-Have backlog that prompted `Any.Uri()`.
