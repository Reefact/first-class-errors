# ADR-0037 | Vary the DateTimeOffset offset dimension

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0037-vary-the-datetimeoffset-offset-dimension.fr.md)

**Status:** Superseded by [ADR-0051](0051-filter-the-datetimeoffset-pool-by-the-declared-offset.md)
**Proposed:** 2026-07-26
**Accepted:** 2026-07-26
**Decision Makers:** Reefact

## Context

A `DateTimeOffset` carries two dimensions: the instant (its `UtcTicks`) and the offset from UTC. The offset is the reason the type exists rather than a plain `DateTime`. `AnyDateTimeOffset` varies only the instant and pins the offset to `TimeSpan.Zero`, a limitation its own remarks document. Code whose behaviour depends on the offset — local rendering, offset arithmetic, "same instant, different offset" equality — therefore cannot obtain a varied-but-valid offset from JustDummies, and the common latent bug "the code assumes the offset is zero" is never surfaced by a dummy value.

`DateTimeOffset` constrains its offset to a whole number of minutes within ±14:00, and requires that the local ticks (`UtcTicks + offset`) stay inside the `DateTime` range; near the extremes of the domain, not every offset is valid for a given instant. JustDummies builds a value constructively to satisfy its constraints, detects contradictions eagerly at declaration, and never retries. Comparison is by instant, and `OneOf` already returns the supplied values verbatim, offset included, because rebuilding from the instant alone would normalise the offset away. Issue #226 records a bounded offset draw as a demand-driven addition; issue #297 tracks it.

## Decision

`AnyDateTimeOffset` gains an opt-in offset dimension — `WithOffset` pins a whole-minute offset and `WithOffsetBetween` draws a bounded one — while the unconstrained default stays `TimeSpan.Zero`, and the instant is tightened at declaration so that every admitted offset yields a valid value.

## Rationale

Reaching the offset makes `AnyDateTimeOffset` a faithful generator of its own type and surfaces the "assumes UTC offset" bug class that a zero-pinned generator hides. Keeping it opt-in — the default stays `TimeSpan.Zero` — makes the addition non-breaking: tests that today rely on a zero offset, or serialise to `+00:00`, keep working.

Tightening the instant at declaration, rather than clamping or rejecting the offset per draw, is what keeps the constructive, one-draw, no-retry model: once the instant window admits every offset in the requested range, the offset is an independent draw that can never produce an out-of-range value. It also reuses the interval engine's bound tightening, so an instant window with no room for the requested offset conflicts eagerly and names both sides — exactly as every other constraint does. Offering a pin and a bounded draw mirrors the library's existing pin/`Between` idiom, and the whole-minute ±14:00 rule mirrors `DateTimeOffset`'s own. `OneOf` keeps returning its values verbatim because it is a terminal enumeration of exact values, so the offset dimension governs only the constructed draw.

The offset arithmetic, the instant-tightening bounds, and the draw are implementation, documented in the `AnyDateTimeOffset` code and the JustDummies user documentation — not here.

## Alternatives Considered

### Vary the offset by default

Considered because "any valid `DateTimeOffset`" arguably includes any offset, making the current zero-pin the less faithful choice. Rejected because it is a behavioural breaking change: tests asserting `Offset == TimeSpan.Zero`, or serialising to a `+00:00` rendering, would break. Opt-in delivers the capability additively; varying by default can be revisited only under a future major version.

### Clamp or reject the offset per draw near the edges

Considered because it leaves the instant domain untouched. Rejected because it either reintroduces a per-draw conditional failure (against the no-retry model) or silently narrows the offset in a way that is hard to reason about. Tightening the instant once, up front, is simpler and always valid.

### Ship only `WithOffset` (pin), no bounded draw

Considered as the minimal surface. Rejected because the motivating use case — exercising offset-sensitive logic across a range of offsets — is exactly the bounded draw; a pin alone does not serve it.

### Leave the gap

Considered because most code treats a `DateTimeOffset` as an instant. Rejected because it keeps `AnyDateTimeOffset` an unfaithful generator whose offset never varies, and pushes anyone who needs a varied offset to a hand-rolled construction that typically ignores the seed.

## Consequences

### Positive

* Offset-sensitive code becomes exercisable, and the "assumes UTC offset" latent bug is catchable, with a value that stays valid by construction.
* The addition is non-breaking: the unconstrained default is unchanged.
* An impossible instant/offset combination is diagnosed eagerly through the existing engine, naming both constraints.

### Negative

* `AnyDateTimeOffset` now carries a second dimension and its own offset state threaded through every transform.
* The offset dimension is `DateTimeOffset`-specific — the other temporal generators have no offset — a deliberate specificity rather than a uniform surface.

### Risks

* A pinned offset near the domain edge tightens the reachable instant window; a user could read the resulting eager conflict as spurious. Mitigation: the conflict names both constraints, and the behaviour is documented.
* `WithOffset` combined with `OneOf` does not override a `OneOf` value's own offset. Mitigation: documented, and consistent with `OneOf`'s terminal-enumeration semantics.

## Follow-up Actions

* Document `WithOffset`/`WithOffsetBetween` in the JustDummies readme and the builder documentation (done in the implementing pull request).
* Consider a shorthand for "any valid offset" only if `WithOffsetBetween(-14h, +14h)` proves a friction in practice.
* Revisit varying the offset by default only under a future major version.

## References

* Issue [#297](https://github.com/Reefact/first-class-errors/issues/297) — the dedicated issue for this feature.
* Issue [#226](https://github.com/Reefact/first-class-errors/issues/226) — the Nice-to-Have backlog it was split from.
* [ADR-0030](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.md) — the terminal-enumeration semantics `OneOf` follows.
* `AnyDateTimeOffset` in the `JustDummies` project; the JustDummies NuGet readme.
