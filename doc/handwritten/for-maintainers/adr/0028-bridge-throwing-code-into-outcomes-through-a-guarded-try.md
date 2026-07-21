# ADR-0028 | Bridge throwing code into outcomes through a guarded Try

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0028-bridge-throwing-code-into-outcomes-through-a-guarded-try.fr.md)

**Status:** Accepted
**Date:** 2026-07-21
**Decision Makers:** Reefact

## Context

FirstClassErrors exists to make the failure path of an operation explicit: an
error is a value a caller returns and inspects (`Outcome`/`Outcome<T>`), not an
exception that travels invisibly. The library already provides the exits from
the outcome flow back into exceptions (`GetResultOrThrow()`, `ThrowIfFailure()`,
`Error.ToException()`); it had no sanctioned entrance in the other direction.

Real code must nonetheless enter the outcome flow from throwing sources. Many
primitives signal failure only by throwing and expose no non-throwing
counterpart on the frameworks this library supports: the library floors at
.NET Standard 2.0 / .NET Framework 4.7.2 (ADR-0022), where forms such as
`MailAddress.TryCreate` (.NET 5+) or `Convert.TryFromBase64String`
(.NET Core 2.1+) do not exist, and third-party libraries frequently ship a
throwing `Parse`/`Decode` with no `TryXxx` at all. Reaching the outcome flow
from such a call meant a hand-written `try`/`catch` that constructs an
`Outcome` — repeated at every call site.

That hand-written bridge has recurring, silent failure modes:

* Catching `System.Exception` relabels unexpected bugs (a null dereference, an
  invalid state) as *anticipated* errors — the one thing an `Outcome` is
  documented not to represent.
* Catching a rich protocol exception (HTTP, socket, database) collapses several
  distinct failures — each carrying status data the caller already holds — into
  a single "it threw".
* Wrapping a call that *does* have a non-throwing counterpart on the target
  framework pays for a `try`/`catch` where a result check would do.
* Binding the caught type to a cancellation type produces a catch that can never
  run, because cancellation is cooperative control flow, not a failure result;
  the compiler does not flag this.

The library also treats an error without a stable code as a smell (its error-code
analyzers), and treats a raw exception message as data that must be curated
before it enters an error. An automatic exception-to-error conversion would
violate both.

Finally, ADR-0005 reserved the *plain* factory name for the Outcome-returning
variant and removed the `Try` prefix from factories (`TryXxx` → `Xxx`), because
a `TryXxx` name borrows the BCL `bool TryXxx(..., out T)` shape without honouring
it.

## Decision

FirstClassErrors provides `Outcome.Try` as the one sanctioned bridge from a
throwing operation to an `Outcome` — catching a single caller-named exception
type, requiring an explicit error mapper, and always letting cancellation
propagate — and keeps that bridge deliberately narrow by flagging its misuse
with usage analyzers rather than by broadening the API.

## Rationale

* **One primitive replaces a repeated, error-prone pattern.** The hand-written
  `try`/`catch`-to-`Outcome` bridge recurs at every boundary and reproduces the
  same mistakes. Folding it into a single call makes the correct shape the
  default shape and the boilerplate disappear.
* **The mapper is mandatory because errors must stay first-class.** An automatic
  exception-to-error conversion would yield errors without a stable code and
  would leak raw exception messages — exactly what the library's error-code and
  error-context conventions discourage. Forcing the caller to map is what keeps
  the produced error curated and diagnosable.
* **A single caught type keeps the Outcome honest.** An `Outcome` models an
  anticipated failure; catching broadly would turn bugs into anticipated errors.
  Naming one exception type is what separates the failure the operation is
  expected to produce from the crash it is not.
* **Cancellation must propagate.** Cooperative cancellation is a request to stop,
  not a failure to capture; letting it through is what stops `Try` from
  swallowing a caller's cancellation.
* **Analyzers, not a narrower type surface, draw the boundary.** The legitimate
  uses (throw-only primitives with no counterpart on the target framework,
  third-party throwing APIs) and the misuses share the *same* signature and
  differ only by context the compiler cannot see. Removing capability would
  block the valid cases the primitive exists for; a build-time, tunable
  diagnostic marks the misuse while leaving the capability intact. Each guard
  names a specific hazard from the Context — over-broad catch, a discarded
  protocol result, an available non-throwing alternative, a dead cancellation
  catch — and the consumer can escalate or suppress it. ADR-0005 noted that a
  convention left to review alone slips back in; here the boundary is enforced by
  tooling from the start.
* **`Try` is a coherent name here, and does not reopen ADR-0005.** ADR-0005
  governs *factory* names, where a `TryXxx` prefix falsely advertised the BCL
  `bool`+`out` shape. `Outcome.Try` is not a factory and not a `TryXxx` prefix:
  it is a higher-order operation that takes the work as a delegate and returns an
  `Outcome<T>`. The delegate argument makes its shape unmistakable, so it borrows
  no false promise; the two decisions are orthogonal.

## Alternatives Considered

### No primitive — leave callers to write their own try/catch

Considered because it adds no public surface and imposes no opinion.

Rejected because it reproduces every failure mode above at every call site with
nothing to catch them, and buries the failure path in imperative plumbing the
library exists to surface. The absence of a sanctioned bridge is what let the
mistakes recur in the first place.

### A fluent multi-catch builder (`Try(...).Catch<A>(...).Catch<B>(...)`)

Considered because chaining reads well when several exception types map to
different errors.

Rejected because it actively invites catching many types — re-introducing the
over-broad-catch hazard as an API affordance — complicates where cancellation is
handled, and serves a case most bridges do not have: the honest majority anticipate
exactly one exception type. The narrow single-type primitive is the 90% tool;
genuinely multi-exception boundaries are better served by an explicit adapter.

### Automatic exception-to-error conversion (no mandatory mapper)

Considered for ergonomics — the caller would not have to write a mapper.

Rejected because the produced error would have no stable code and would carry a
raw exception message, the two things the library's error-code and
error-context conventions exist to prevent. The small ceremony of a mandatory
mapper is the price of a first-class error.

### Enforce the boundary by narrowing the type surface instead of analyzers

Considered because making misuse impossible is stronger than flagging it.

Rejected because legitimate and illegitimate calls share one signature and
differ only by context invisible to the type system; any narrowing that blocked
the misuse would also block the old-framework and third-party cases the
primitive is *for*. The judgment belongs at the use site, which is where an
analyzer speaks.

## Consequences

### Positive

* There is one obvious, safe way to enter the outcome flow from throwing code,
  and its default shape is the correct one; the recurring boilerplate collapses
  into a single call.
* Errors produced through `Try` keep a mapped, stable code and a curated
  message, so they remain first-class.
* The four misuse modes are caught at build time and are tunable per consumer,
  so the guard informs without blocking legitimate use.
* Cancellation semantics are preserved without the caller having to think about
  them.

### Negative

* New public surface — value and side-effecting forms, each synchronous and
  asynchronous — to maintain and document on the netstandard2.0 / net472 floor.
* Four usage analyzers (FCE019–FCE022) with their bilingual rule pages and tests
  must be kept consistent with the primitive's behaviour.
* The mandatory mapper adds a small, unavoidable ceremony to every call.

### Risks

* The analyzers are advisory and suppressible, so a determined misuse can still
  ship: the guard reduces error, it does not eliminate it. Mitigation — the two
  provable-defect guards (over-broad catch, dead cancellation catch) are on by
  default as warnings.
* If the caught-type or cancellation behaviour of `Try` ever changes, the four
  analyzers and their documentation must move in lockstep or they will mislead.
  Mitigation — the behaviour is pinned by tests.
* A reader may perceive the name `Try` as conflicting with ADR-0005. Mitigation —
  this ADR records why the two are orthogonal.

## Follow-up Actions

* Keep the EN README/guide and the French translation in sync for the `Try`
  guidance and the FCE019–FCE022 rule pages.
* Keep the FCE019–FCE022 analyzers and their tests aligned with the primitive's
  documented behaviour whenever either changes.

## References

* ADR-0005 — reserve the plain factory name for the Outcome-returning variant
  (naming; orthogonal — explains why `Try` here raises no conflict).
* ADR-0022 — floor the library on .NET Framework 4.7.2 (why throw-only
  primitives without a `TryXxx` are a real, supported case).
* ADR-0003 — unify Outcome value mapping under Then (Outcome API context).
* [Outcome guide](../../for-users/OutcomeGuide.en.md) — user-facing explanation of
  entering and leaving the outcome flow.
* Analyzer rule pages [FCE019](../../for-users/analyzers/FCE019.en.md),
  [FCE020](../../for-users/analyzers/FCE020.en.md),
  [FCE021](../../for-users/analyzers/FCE021.en.md),
  [FCE022](../../for-users/analyzers/FCE022.en.md).
