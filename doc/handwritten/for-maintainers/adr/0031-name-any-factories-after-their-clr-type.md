# ADR-0031 | Name Any's scalar factories after their CLR type

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0031-name-any-factories-after-their-clr-type.fr.md)

**Status:** Accepted
**Date:** 2026-07-21
**Decision Makers:** Reefact

## Context

* `Dummies` exposes a static entry point `Any` whose parameterless factories each start a
  generator for one .NET simple type — `Any.Int32()`, `Any.SByte()`, `Any.Single()`,
  `Any.UInt64()`, `Any.String()`, `Any.Guid()`, `Any.DateTime()`, … — and each returns a
  builder type named `Any{Name}` (`Any.Int32()` returns `AnyInt32`). `AnyContext` mirrors
  every one of these scalar factories for the seeded-context surface, so each factory exists
  in two places that must agree.
* Across that whole scalar surface the factory name and the builder name are the **CLR type
  name** — the value `System.Type.Name` returns — not the C# keyword: `Int32` not `Int`,
  `Single` not `Float`, `Int64` not `Long`, `Byte`/`SByte`, `Decimal`, `Char`. The C# keyword
  forms cannot all serve as identifiers (`Any.int()` is a syntax error), and the CLR names
  read uniformly with the builder type names.
* This mirrors the .NET base class library's own per-primitive method families, which name
  each method after the CLR type: `Convert.ToInt32` / `ToSingle` / `ToBoolean`,
  `BitConverter.ToInt32` / `ToBoolean` — never `ToBool`, `ToFloat`, or `ToInt`.
* One factory deviated: `Any.Bool()`, returning `AnyBool`, produced a `System.Boolean`, whose
  CLR name is `Boolean`. It was the single member of the scalar surface not named after its
  CLR type. The 2026-07-20 Dummies architecture & design audit surfaced it (§8.2, §8.4) and
  recommended the choice be settled deliberately and recorded before release.
* `Dummies` is pre-release: no `dum-v*` tag, no external NuGet consumers, an empty *Unreleased*
  changelog. Renaming a public factory and a public builder type is a breaking change once
  consumers depend on it, and costs nothing before the first publication.
* The repository records naming decisions of this class as ADRs — ADR-0005 reserves the plain
  factory name for the `Outcome`-returning variant, ADR-0007 names the binder terminals `New`
  and `Create`.

## Decision

Every parameterless scalar factory on `Any` and `AnyContext`, and the builder type it returns,
is named after the CLR name of the type it produces, with no exception — renaming `Any.Bool()`
/ `AnyContext.Bool()` / `AnyBool` to `Any.Boolean()` / `AnyContext.Boolean()` / `AnyBoolean`.

## Rationale

* The convention was already "CLR type names" for every scalar factory but one. Keeping `Bool`
  leaves the rule statable only with a caveat — "CLR names, except the one we shortened" — and
  checkable only with an allow-list entry carrying that exception. Naming `Boolean` makes the
  rule exception-free: one sentence describes it and one guard enforces it.
* The ergonomic case for the short `Bool` is exactly the case already declined for `Int`
  (`int`), `Long` (`long`), `Short` (`short`), and `Float` (`float`) — all spellings a
  developer writes far more often than `Boolean`. Honouring the short form for `bool` alone
  would privilege one keyword with no principle distinguishing it from the widths and reals the
  surface already spells out in full.
* The choice aligns the surface with the BCL's own `Convert` / `BitConverter` per-primitive
  families named in Context, the closest existing analog to "one method per simple type, named
  after the type"; a consumer who reaches for `Convert.ToBoolean` finds `Any.Boolean()` where
  they expect it.
* Matching factory name to builder name to CLR name keeps one mental model — `Any.X()` →
  `AnyX` → `System.X` — which a single reflection guard can assert for the whole surface, so a
  future added type cannot silently reintroduce the deviation.
* Settling this pre-release costs nothing and, per the audit, is the cheapest moment to decide;
  deferring past 1.0 turns a free rename into a breaking change in either direction.

## Alternatives Considered

### Keep `Bool()` / `AnyBool` and record the short form as a deliberate exception

Considered because `bool` is the near-universal spelling in modern C# while `Boolean` is rarely
hand-written, so `Any.Bool()` is marginally more familiar at the call site, and a one-line
README rationale could make the deviation read as chosen rather than accidental.

Rejected because the same familiarity argument applies verbatim to `Int`/`Long`/`Short`/`Float`,
which the surface already declines; recording the exception preserves a rule that must then be
stated with a caveat and guarded with an allow-list entry, trading a one-time pre-release rename
for permanent asymmetry in the surface whose whole value is its uniformity.

### Rename only one side — the builder to `AnyBoolean` while keeping `Bool()`, or the reverse

Considered because it would touch fewer call sites.

Rejected because it breaks the factory-name-equals-builder-name correspondence that holds
everywhere else (`Any.Int32()` → `AnyInt32`), replacing one deviation with another and losing
the single-mental-model benefit that motivates the change.

### Offer both `Bool()` and `Boolean()`, one delegating to the other

Considered because it would keep the familiar name while adding the conventional one.

Rejected because two names for one generator double the discoverable surface, invite
inconsistent call sites, and still leave an off-convention public member to document and guard;
the pre-release window makes a single clean name available at no cost.

## Consequences

### Positive

* The scalar factory surface follows one exception-free rule (`Any.X()` → `AnyX` → CLR `X`),
  statable in a sentence and enforceable by a single reflection guard.
* The surface matches the BCL's own `Convert` / `BitConverter` per-primitive naming, lowering
  surprise for consumers.
* The naming is settled before the first release, so no consumer ever migrates.

### Negative

* `Any.Boolean()` is more verbose than the near-universal `bool` keyword spelling; a consumer
  expecting `Bool()` must reach for `Boolean()`.
* The rename touches the builder type, both entry points, the packaged-asset check, and the
  tests at once — a mechanical, pre-release cost.

### Risks

* Without enforcement a future scalar type could reintroduce a keyword-short name (a second
  `Bool`); mitigated by the factory-naming parity guard added with this decision, which fails
  when a factory, its builder, or its `AnyContext` mirror departs from the CLR name.

## Follow-up Actions

* Rename `Any.Bool()` / `AnyContext.Bool()` / `AnyBool` to `Boolean` / `AnyBoolean`, and update
  the tests, the `dummies-check` packaged-asset probe, and the package README *(done in this
  change)*.
* Add the factory-naming parity guard to `Dummies.UnitTests` *(done in this change)*.

## References

* ADR-0005 — reserve the plain factory name for the Outcome-returning variant; naming-decision
  precedent.
* ADR-0007 — name the binder terminals New and Create; naming-decision precedent.
* ADR-0020 — materialize dummies only through Generate(); shares the pre-1.0 "cheapest moment to
  decide" framing.
* 2026-07-20 Dummies architecture & design audit, §8.2 and §8.4 — surfaced the deviation.
* Issue #222.
