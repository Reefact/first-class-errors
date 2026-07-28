# ADR-0048 | Guarantee a generated regex value matches its pattern, by bounded redraw

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0048-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md)

**Status:** Proposed
**Date:** 2026-07-27
**Decision Makers:** Reefact

## Context

`Any.StringMatching(...)` parses a pattern into a tree once and, on each draw, walks it to **build** a value
directly — never generate-then-filter. The build mirrors the regular subset of the .NET engine's
semantics, so a generated value is a genuine member of the pattern.

A few corners of the engine's **empty-match** handling cannot be mirrored structurally, because .NET's
answer to "does the empty string match?" for a **nullable alternative under a quantifier** is
implementation-defined and depends on details a structural build does not carry: the **order** of the
alternatives, and the **form** of the empty branch (a bare `|` versus a zero-quantified atom such as
`\S{0}`). Measured (issue #335):

| pattern (anchored, `IgnoreCase`)      | engine matches `""` |
| ------------------------------------- | ------------------- |
| `(?:\S{0}b{0}){1,2}`                   | yes                 |
| `(?:r{1,2}\|\S{0}){1,2}`               | **no**              |
| `(?:\S{0}\|r{1,2}){1,2}` (order swapped) | yes               |
| `(?:r\|){1,2}` (bare-empty branch)    | yes                 |

The structural build picked the `\S{0}b{0}` branch and emitted `""`, which the engine then refuses for
that shape — so `Any.StringMatching` returned a value the very pattern it was built from does not match.
The patterns that trigger this are degenerate: FsCheck *generates* `\S{0}` (match `\S` zero times); a
human writes `\S*`. But the contract "a generated value matches its pattern" was broken.

## Decision

After the structural build, the value is **verified against the real .NET engine** (a full, anchored
match under the one option the generator honoured — `IgnoreCase`) and **redrawn on a miss**, bounded. The
check is the last word: a value the engine would reject is never returned. Exhausting the cap raises an
`AnyGenerationException`.

## Rationale

* **Keep the invariant by construction, not by modelling.** The engine's empty-match corners are
  order-dependent, form-dependent, and implementation- and version-specific — a losing game to chase in a
  hand-written model. Verifying the output against the engine makes "a generated value matches its
  pattern" hold for this defect **and any future divergence** between the structural build and the engine,
  with no arcane rule to maintain.
* **Bounded redraw is the house idiom.** ADR-0033 already meets string exclusions with a bounded redraw:
  a structural fast path plus a bounded safety net. This is the same shape for the same reason.
* **The cost is immaterial.** A supported pattern matches on the first build; only these rare corners
  redraw, and a valid value appears within a handful of draws. Generation is not a hot loop, and
  `Any.StringMatching(Regex)` already holds a compiled `Regex`. The cap turns a pattern the build can
  never satisfy into a clear error instead of an unbounded loop.
* **Reproducibility is preserved.** The redraw consumes further draws from the same seeded source, so a
  seed still replays the run exactly.

## Alternatives Considered

### Model the engine's empty-match semantics

Rejected. The behaviour above is order-dependent, form-dependent, and not something the engine documents
as a stable contract; a model of it would be brittle and would need revisiting on engine changes — while
never being provably complete.

### Refuse the degenerate patterns as unsupported

Considered: refuse a zero-quantified term (`X{0}`) and/or a nullable alternative under a quantifier with
an `UnsupportedRegexException`, keeping generation purely structural. Rejected because detecting **every**
divergence eagerly is nearly as hard as modelling it — the risk is refusing some valid patterns while
still missing others — and it shrinks a documented capability for patterns that are merely unusual, not
outside the supported subset. The bounded redraw covers the whole class without a fragile detector.

## Consequences

### Positive

* "A generated value matches its pattern" is un-breakable — for this bug and for any future model/engine
  divergence. The `IgnoreCase` round-trip property (#335) holds by construction, not by luck of the seed.

### Negative

* A value is built and then checked, rather than built and returned unconditionally — a small departure
  from "never generated then filtered". The primary mechanism stays structural; the check is a rare-miss
  safety net, and the generator's documentation says so.
* A genuinely unsatisfiable pattern now raises an `AnyGenerationException` after the cap instead of
  returning a wrong value — a clearer failure, but a failure where the old code silently returned garbage.

## References

* ADR-0033 — Meet string exclusions with a bounded redraw: the idiom this reuses.
* ADR-0030 — Draw arbitrary strings from an explicit terminal set: the structural, build-don't-filter
  philosophy this complements rather than replaces.
* Issue #335 — the `IgnoreCase` round-trip flake that made the empty-match divergence concrete.
