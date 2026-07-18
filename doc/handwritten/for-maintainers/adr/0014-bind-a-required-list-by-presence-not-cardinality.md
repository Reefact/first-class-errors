# ADR-0014 | Bind a required list by presence, not cardinality

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0014-bind-a-required-list-by-presence-not-cardinality.fr.md)

**Status:** Proposed
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

* The request binder binds a list request property through `AsRequired` or `AsOptional`,
  the same presence choice it offers for a scalar property.
* The binder derives "the argument is missing" from the DTO property being `null`. A
  serializer maps an absent JSON array to a `null` property; a present-but-empty JSON
  array (`[]`) deserializes to a non-null list of zero elements.
* On a missing (`null`) list, `AsRequired` records `REQUEST_ARGUMENT_REQUIRED` and
  `AsOptional` binds an empty list and records nothing.
* Before this decision, the behavior of a required list that is *present but empty* was
  unspecified: it was neither documented nor pinned by a test. The implementation bound
  it as success — an empty list — because "missing" is `null` only, so an empty list
  reaches the per-element loop, which iterates zero times.
* The maintainer-grade review of the request binder (design spec #126) raised this as
  finding 19 of 19 (issue #155): despite 100% line and branch coverage, no assertion
  fixed whether an empty required list is valid, so the behavior could change silently.
* For a scalar property, "required" means "present" — a non-null argument; conversion of
  its value is a separate concern the value-object factory owns.
* The binder's role is presence and per-element conversion: it records one coded error
  per failing element and collects every failure into a single envelope. It carries no
  notion of collection size.
* A minimum-cardinality rule ("at least one element") is a domain rule that varies by use
  case, and the library already places domain invariants in the value object or command a
  bound value feeds.
* The library is pre-release, unpublished on NuGet with no external consumers, so fixing
  the contract now carries no migration cost.

## Decision

A required list binding constrains only the list's presence — an absent (`null`) list
records `REQUEST_ARGUMENT_REQUIRED`, while a list that is present but empty is valid and
binds to an empty list.

## Rationale

* Presence is the one meaning "required" already carries for a scalar property, and a
  list is a property like any other; letting an empty list count as "missing" would make
  `AsRequired` mean "present" for a scalar and "present and non-empty" for a list — two
  contracts under one name, the kind of inconsistency the binder's deliberate,
  small-surface design avoids.
* Keeping cardinality out of the binder respects the separation the library already
  draws: the binder answers "was the argument sent, and does each element convert?", and
  the value object or command it feeds answers "is this a valid domain value?", where a
  minimum-size rule belongs. Baking "non-empty" into every required list would impose one
  domain's policy on all of them, and that policy genuinely varies (zero-or-more here,
  at-least-one there).
* Reading "missing" as `null` keeps the binder faithful to what the client actually sent:
  an absent array and an empty array are different messages on the wire, and an empty list
  is a deliberate value, not an absence. Recording `REQUEST_ARGUMENT_REQUIRED` for a list
  the client did send would misreport that message.
* The review found no defect in the behavior, only that it was unasserted; ratifying the
  existing behavior as the contract — rather than changing it — is the least-surprising
  choice, and the pre-release status lets the contract be fixed now with no consumers to
  migrate.

## Alternatives Considered

### Treat a present-but-empty required list as missing

Considered because "required" can colloquially suggest "at least one", so a caller might
expect an empty required list to be rejected.

Rejected because it conflates presence with cardinality: it would give `AsRequired` two
different meanings for scalars and lists, hard-code one domain's minimum-size policy into
the binder, and misreport a deliberately sent empty array as an absence.

### Add a distinct non-empty required binding (or a minimum-count option)

Considered because some requests genuinely need at least one element, and an explicit
`AsRequiredNonEmpty` (or a count option) would express that at the binder.

Rejected for now because it widens the binder's surface with a cardinality concern the
value object or command already expresses, and no current requirement calls for it. It
remains open as a future opt-in: this decision fixes only the default meaning of
"required", so a later non-empty variant would extend it, not contradict it.

### Leave the behavior unspecified

Considered because it is the smallest change — the implementation already binds an empty
required list as success.

Rejected because an unasserted contract is exactly the finding of #155: it can change
silently under a refactor. A stated decision plus a pinning test makes it stable.

## Consequences

### Positive

* "Required" has a single meaning across scalar and list properties: present, non-null.
* The binder stays a presence-and-conversion boundary; domain cardinality rules stay in
  the domain, where they can vary per use case.
* The contract is documented and pinned by tests, so it can no longer drift silently.

### Negative

* A request that truly needs a non-empty list must enforce that in the value object or
  command it feeds, not through the binder — a small explicit step for the consumer.

### Risks

* A future minimum-cardinality need would require a new opt-in binding surface; mitigated
  by this decision fixing only the default meaning (leaving room for one) and by
  cardinality being expressible in the domain today.

## Follow-up Actions

* Documented in the same change: the binder reference docs
  ([`RequestBinder.en.md`](../../for-users/RequestBinder.en.md) and its French
  translation) and the API XML docs on the three list converters.
* Pinned in the same change: unit tests across the simple, value-type, and complex list
  converters, plus property-based invariants for the collect-all and path-stability
  guarantees in `FirstClassErrors.RequestBinder.PropertyTests`.

## References

* Issue #155 — the finding this decision resolves (finding 19 of 19 of the request binder
  review).
* Pull request #126 — the request binder design spec this contract belongs to.
* ADR-0007 — name the binder terminals New and Create; a sibling public-API decision on
  the same binder.
* ADR-0012 — fix the binder options before binding begins; another binder contract
  decision.
