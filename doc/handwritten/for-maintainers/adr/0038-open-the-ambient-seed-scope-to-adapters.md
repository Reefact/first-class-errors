# ADR-0038 | Open the ambient seed scope to test-framework adapters

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0038-open-the-ambient-seed-scope-to-adapters.fr.md)

**Status:** Accepted
**Date:** 2026-07-26
**Decision Makers:** Reefact

## Context

`Dummies` draws every arbitrary value from a random source. The static `Any`
entry points draw from an **ambient** source that flows with the execution
context, so it never leaks across tests running in parallel. Determinism over
that ambient source is opt-in, and today only two public paths reach it:

* `Any.Reproducibly(...)`, which pins a seed for the duration of a **delegate it
  owns**, runs that delegate, and reports the seed if it throws. ADR-0026 made
  this the repository's single seed story.
* `Any.WithSeed(...)`, which creates an **isolated** context. The static `Any`
  entry points do not draw from it, so it pins nothing for code that uses them.

The handle that opens and closes an ambient seed scope exists, but is internal.

A test-framework adapter — the xUnit companion package considered separately, or
any future adapter for another framework — does not own a delegate wrapping the
test body. The seam a framework offers is a pair of hooks that run *before* and
*after* the test method. An adapter must therefore open the ambient scope in one
hook and close it in the other, which no public path allows.

Two further facts bear on the shape of that opening.

* **Generation failures carry a replay snippet.** When a generator fails —
  typically a factory rejecting a drawn value — the exception message appends a
  guidance naming the mechanism that actually replays the run. That guidance is chosen
  per kind of source: the ambient source names the delegate runner, an isolated
  context names itself, because the two are replayed differently. Naming an
  snippet the caller's code does not contain is a misleading diagnostic, and
  avoiding it is the reason the guidance varies at all.
* **Neither existing phrasing fits an adapter-pinned run.** A test whose seed was
  pinned by an adapter contains no call to the delegate runner, and replaying it
  means changing whatever the adapter reads — an attribute argument, a runner
  setting — not adding a call the test never had.

The repository already has an established idiom for context-local overrides,
recorded in ADR-0006 and used by the testing package's clock and instance-id
seams: the override is opened by a `Use…` call and closed by disposing what it
returns.

`Dummies` is pre-1.0 and not yet published to NuGet (ADR-0011), so its public
surface can still grow without a compatibility ceremony. The library's identity
is that it depends on nothing beyond the standard library, a boundary an
architecture test asserts over its own assembly.

The alternative access path — granting a named companion package access to
`Dummies`' internals — is available: the library declares no such grant today.

## Decision

`Dummies` exposes the ambient seed scope as a public, disposable handle whose
opener may supply the replay snippet that generation-failure diagnostics will
name.

## Rationale

* **An adapter's shape is before/after, not around a delegate.** The delegate
  runner cannot serve a caller that has no delegate to wrap, and the isolated
  context is the wrong source — the code under test draws from the ambient one.
  A scope the caller opens and closes itself is the only shape that fits the seam
  a test framework actually offers.
* **Public, rather than an internals grant, is what keeps every adapter
  possible.** An internals grant privileges one named companion and forecloses
  the others: a third-party adapter, or a first-party one for another framework,
  would each need their own grant and their own change to `Dummies`. A public
  handle makes "adapt another framework later" an additive decision that touches
  nothing here — which is precisely the property that lets the xUnit decision be
  taken narrowly, without deciding the rest.
* **Carrying the replay snippet preserves an invariant the library already
  enforces.** The diagnostic names the mechanism that applies; that is why the
  guidance varies by source in the first place. An adapter introduces a third way of
  pinning the ambient source, and without a way to say so it would inherit the
  delegate runner's phrasing — advertising, to a developer whose test contains no
  such call, exactly the misleading snippet the mechanism exists to prevent.
  Letting the opener name the snippet extends that design rather than
  working around it.
* **The disposable-scope shape is already the house idiom.** ADR-0006 established
  it for the clock and instance-id overrides, so the addition is recognizable as
  the same thing rather than a second, unrelated mechanism.
* **This is the cheapest moment.** The package is pre-1.0 and unpublished, so the
  surface can be shaped now; a parameter added to a published member later is
  more disruptive than one present from the start.

## Alternatives Considered

### Grant the companion package access to Dummies' internals

Considered because it adds no public surface at all: the adapter would use the
existing internal handle unchanged. Rejected because it privileges one named
companion — every other adapter, first-party or third-party, would need its own
grant and therefore its own change to `Dummies` — and because it couples the two
packages' assembly identities for a capability that is not, in itself, private.

### Expose the scope without a replay snippet

Considered as the smallest possible addition, deferring the diagnostic question
until an adapter exists. Rejected because it ships the misleading diagnostic the
guidance mechanism exists to prevent: every adapter-pinned run whose generation fails
would tell the developer to use a call their test does not contain. It also
defers a parameter onto a published member, which is the more disruptive order.

### Phrase the ambient guidance neutrally, so it is never wrong

Considered because guidance that names no mechanism cannot name the wrong one.
Rejected because it pays for the rarer case with the dominant one: the delegate
runner's users would lose an actionable snippet — the exact call to write —
to accommodate a caller that can simply state its own.

### Let adapters reuse the delegate runner

Considered because it needs nothing new. Rejected because a framework's
before/after hooks give an adapter no delegate to pass: it observes the test, it
does not invoke it.

## Consequences

### Positive

* Any test framework can be adapted without privileged access to `Dummies` and
  without a further change to it, so each additional adapter is an independent,
  additive decision.
* A run whose seed an adapter pinned reports a replay snippet that matches
  the caller's own code, keeping the guarantee that a diagnostic never names a
  mechanism the reader does not use.
* The addition reuses the established disposable-scope idiom instead of
  introducing a second shape for the same concept.

### Negative

* A third public way to control seeding, alongside the delegate runner and the
  isolated context. The documentation must keep the three distinct and say which
  one a reader wants.
* The replay snippet is supplied by the caller and cannot be validated by
  `Dummies`, so a badly phrased one degrades the diagnostic it was meant to
  improve.

### Risks

* A caller that opens the scope and fails to close it leaks a pinned seed into
  whatever runs next in the same execution context. The risk is bounded by the
  idiom — ownership belongs to whoever opened the scope — and is the same
  contract the clock and instance-id overrides already carry.
* A public handle invites use outside a test-framework adapter, where the
  delegate runner would serve better. This is a documentation matter, not a
  correctness one: the scope behaves identically however it is opened.

## Follow-up Actions

* Document the addition in the user guide, in English and French in lockstep,
  distinguishing the three ways to control seeding and naming the adapter case as
  the one this handle exists for.
* Revisit the snippet-carrying form if a second adapter shows it is
  habitually left unused.

## References

* ADR-0006 — Supply arbitrary test values from a single seedable source: the
  disposable-scope idiom this addition reuses, and the follow-up anticipating a
  test-framework adapter.
* ADR-0011 — Host Dummies as a standalone package: the zero-dependency identity
  and the pre-1.0 latitude this decision relies on.
* ADR-0026 — Rebase the testing package's arbitrary values on Dummies: the single
  seed story this ambient source now carries.
* ADR-0039 — Adapt Dummies to xUnit v3 through a companion package: the first
  consumer of this handle.
* Issue #226 — the Dummies nice-to-have backlog where the adapter is tracked.
