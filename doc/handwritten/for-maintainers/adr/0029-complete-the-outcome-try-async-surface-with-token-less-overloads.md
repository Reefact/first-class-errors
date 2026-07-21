# ADR-0029 | Complete the Outcome.Try async surface with token-less overloads

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0029-complete-the-outcome-try-async-surface-with-token-less-overloads.fr.md)

**Status:** Accepted
**Date:** 2026-07-21
**Decision Makers:** Reefact

## Context

ADR-0028 introduced `Outcome.Try` with four overloads: a synchronous value form
(`Func<T>`), a synchronous side-effecting form (`Action`), and their asynchronous
counterparts, which take a `CancellationToken` (`Func<CancellationToken, Task<T>>`
and `Func<CancellationToken, Task>`). Its Decision sentence keeps the bridge
narrow "by flagging its misuse with usage analyzers rather than by broadening the
API", and its Rationale rejects "narrowing the type surface" — both arguments
scoped, in the ADR's own words, to misuses where "legitimate and illegitimate
calls share one signature and differ only by context the compiler cannot see"
(the four semantic guards FCE019–FCE022).

A code review then found a defect that is *not* of that kind. Passing a
parameterless asynchronous lambda to the synchronous side-effecting overload —
`Outcome.Try<E>(async () => { await …; }, map)` — binds the lambda to `Action`
as **`async void`**, because no token-less `Task`-returning overload exists for
it to prefer. The `Action` returns at the first `await`, so `Try` observes no
exception, returns success, and any exception raised after the first `await`
escapes the `try`/`catch` out-of-band — an unobserved `async void` exception that
typically crashes the process. The same shape silently drops a fire-and-forget
`() => ReturnsTask()`. This is a **silent** defect on the very boundary `Try`
exists to make safe.

Three facts frame the fix:

* The asynchronous **value** case does not share the defect. An
  `async () => … return T` lambda produces a `Task<T>`, which does not match
  `Func<T>`, so it is a **compile error** — a fail-safe that steers the caller to
  the token overload. Only the side-effecting `Action` case binds silently.
* The .NET BCL solved this exact footgun by **overload design**:
  `System.Threading.Tasks.Task.Run` ships `Action`, `Func<Task>`, `Func<T>` and
  `Func<Task<T>>` side by side precisely so an asynchronous lambda binds to the
  awaited `Task`-returning overload instead of `async void`.
* The defect is **visible to the type system** and has **no legitimate use**:
  nobody intends to pass an async-void lambda to a synchronous `Try`.

## Decision

FirstClassErrors adds token-less `Func<Task>` and `Func<Task<T>>` overloads to
`Outcome.Try` so an asynchronous lambda binds to an awaited `Task`-returning
delegate rather than to the synchronous `Action` as `async void`, completing the
async surface — a structural correctness fix that ADR-0028's "rather than
broadening the API" was not meant to preclude.

## Rationale

* **A type-system defect deserves a type-system fix.** The binding accident is
  created at overload resolution, and C# betterness makes an asynchronous lambda
  prefer a `Task`-returning delegate over `Action`. Once the token-less
  `Func<Task>` / `Func<Task<T>>` overloads exist, the async lambda binds there and
  is awaited inside the `try`/`catch`; the crash becomes a compile-time
  impossibility rather than a runtime hazard. This is the correct altitude — the
  same one the async value case already relies on to fail safely.
* **It matches the governing BCL precedent.** `Task.Run` established this exact
  four-way shape for this exact footgun; reproducing it is the conventional,
  least-surprising move for a .NET library author, and it completes the
  sync/async × value/side-effecting symmetry ADR-0028 already committed to (the
  async forms exist today only in token-taking shape).
* **ADR-0028's analyzer preference does not reach this case.** That decision
  argues against *narrowing* the type surface to police *semantic* misuses whose
  legitimate and illegitimate forms share a signature. This defect is the
  opposite: the async-vs-sync distinction is visible to the type system, the
  misuse has no legitimate counterpart, and the fix *adds* an overload rather
  than removing capability — so it blocks zero valid uses and does not trigger
  ADR-0028's objection.
* **An analyzer cannot substitute for prevention here.** A suppressible,
  Warning-by-default diagnostic that only runs where analyzers are enabled is the
  wrong guard for a *silent* process crash — weakest precisely in the older
  .NET Standard 2.0 / .NET Framework consumers `Try` exists for. Prevention beats
  detection when the failure is silent.

## Alternatives Considered

### An analyzer (FCE023) instead of overloads

Considered because it changes no public surface and continues the FCE019–FCE022
family the library already ships.

Rejected as the *primary* guard because it detects rather than prevents: a
silent process crash can still ship where the warning is off or suppressed, and
the analyzer must chase each syntactic shape (async lambda, fire-and-forget
expression) whereas the overload closes the whole family at the type layer. It
remains available as optional defence-in-depth for the residual non-accidental
cases below, but is not needed to close the defect.

### Add only the side-effecting `Func<Task>` overload

Considered because only the `Action` case crashes silently, so `Func<Task>` alone
closes the defect while avoiding the value-side ambiguity below.

Rejected because it leaves the async surface asymmetric — a token-less
`Task`-returning form for the side-effecting case but not the value case — for no
principled reason a caller could predict. Completing both mirrors `Task.Run` and
the sync/async × value/side-effecting grid.

### Leave the defect deferred to a follow-up

Considered because ADR-0028 is freshly accepted and the change touches its
wording.

Rejected because shipping a public error-handling primitive with a known silent
crash on its async boundary is not acceptable; the correct response is to record
the refinement here and let the maintainer accept it, not to release the hazard.

## Consequences

### Positive

* The async-void crash and the fire-and-forget drop become structurally
  impossible for an asynchronous lambda; they are prevented at compile time, not
  merely diagnosed.
* The async surface is symmetric and matches the BCL's `Task.Run` shape, so async
  lambdas behave predictably.
* No legitimate call is blocked: the overloads add capability rather than
  removing it.

### Negative

* Two more public overloads to maintain and document on the netstandard2.0 floor.
* Adding `Func<Task<T>>` beside `Func<T>` makes a lambda with **no natural return
  type** — a bare `() => throw …` or `() => null` — ambiguous between the two, a
  source-compatibility change. It fails as a **compile error** (fail-safe, never a
  runtime surprise) and bites only pathological, effectively test-only lambdas;
  a real operation returns a concrete value or a real `Task`. This is the same
  tradeoff `Task.Run` already accepts.

### Risks

* Two residual, **non-accidental** cases still bind to `Action` by their static
  type: an explicitly `Action`-typed variable, and an `async void` method group
  passed by name. Neither is the accidental lambda path this decision targets, and
  `async void` is already broadly discouraged across the ecosystem. Mitigation, if
  desired later: an optional analyzer as defence-in-depth (see Alternatives).
* The token-less async overloads thread no `CancellationToken`; callers needing a
  token still have the existing token overloads, which a `ct => …` lambda selects
  cleanly. This is a convenience gap, not a correctness one.

## Follow-up Actions

* Cover the async-lambda and fire-and-forget cases with regression tests, and
  document the ambiguity of natural-type-less lambdas in the XML remarks.
* Keep the EN/FR guide and the Try rule pages in sync with the completed surface.
* Decide whether the residual method-group case warrants an optional analyzer;
  close or repurpose the FCE023 tracking issue accordingly.

## References

* ADR-0028 — bridge throwing code into outcomes through a guarded Try (this ADR
  refines its "rather than broadening the API" clause; it does not change the
  guarded-bridge decision itself).
* ADR-0005 — reserve the plain factory name for the Outcome-returning variant
  (API-conservatism this decision is weighed against).
* `System.Threading.Tasks.Task.Run` — the BCL precedent for the `Action` /
  `Func<Task>` / `Func<T>` / `Func<Task<T>>` overload shape.
* PR #265; issue #267 (the deferred async-void follow-up this decision resolves).
* [Outcome guide](../../for-users/OutcomeGuide.en.md).
