# ADR-0058 | Suppress CA1510 while the pre-.NET-6 floor stands

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0058-suppress-ca1510-while-the-netstandard-floor-stands.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-29
**Accepted:** 2026-07-29
**Decision Makers:** Reefact

## Context

`CA1510` asks that every argument guard of the shape

```csharp
if (source is null) { throw new ArgumentNullException(nameof(source)); }
```

be rewritten as `ArgumentNullException.ThrowIfNull(source);`. The helper is
terser, and it carries `[CallerArgumentExpression]` so the parameter name no
longer has to be repeated.

The SonarQube Cloud report counts **323** occurrences — by some distance the
largest single group of findings on the project, and about 55% of all code
smells. They fall into two populations that look identical in the report and are
not:

* **314 in `JustDummies`** and **1 in `JustDummies.UnitTests`**. Both projects
  multi-target across the .NET 6 boundary — `netstandard2.0;net8.0` for the
  library, `net10.0;net472` for its contract suite on the support floor
  (ADR-0022). `ArgumentNullException.ThrowIfNull` arrived in .NET 6, so the
  analyzer sees it on the modern leg and reports every guard, while the *same
  source file* must still compile on the leg that does not have it.
* **8 in `FirstClassErrors.GenDoc`**, which targets `net8.0` only. Nothing
  stands in the way there.

The obvious escape — a polyfill — does not exist for this API. Polyfills work
when the compiler binds by name and the shape is compile-time only: an attribute
such as `CallerArgumentExpressionAttribute` can simply be declared in your own
assembly and the compiler recognises it. `ThrowIfNull` is neither. It is a
**static method on a BCL type that already exists downlevel**, and C# has no
static extension methods, so the only way to supply it would be to declare a
competing `System.ArgumentNullException` that wins name resolution on the old
leg. Shadowing a framework exception type to satisfy a style rule trades a
cosmetic gain for a trap.

`CA1510` is reported at **Info** severity. It has never failed a build, and it
bears on neither reliability nor security.

## Decision

`CA1510` is suppressed, per project and with the reason stated in the project
file, for the two projects that must compile below .NET 6; it is honoured
everywhere the floor does not apply, and the eight guards in
`FirstClassErrors.GenDoc` are rewritten to use the helper.

## Rationale

* **The rule is unsatisfiable where it is loudest.** 315 of the 323 findings sit
  in source that has to compile on a target framework without the API. No
  edit to those files can resolve them; only the floor moving would.
* **The alternatives cost more than the rule is worth.** Rewriting every guard
  as a call to a home-grown `Guard.NotNull` helper would touch 315 call sites,
  add an indirection to every argument check, and lose the
  `[CallerArgumentExpression]` behaviour that motivates the rule in the first
  place. Wrapping each guard in `#if NET6_0_OR_GREATER` would double the line
  count of every guard in the library.
* **A suppression carrying its reason beats a silent one.** The `NoWarn` sits in
  the two project files that own the constraint, next to a comment naming the
  floor and this ADR, so the next maintainer reads the reason where they meet
  the effect — and knows what makes it obsolete.
* **The suppression is scoped, not global.** It is not in `Directory.Build.props`
  and not in `.editorconfig`, so a project that does not straddle the boundary
  keeps the rule. `FirstClassErrors.GenDoc` proves the point by complying.
* **It expires by itself.** The day `JustDummies` drops `netstandard2.0` and the
  test suite drops `net472`, the `NoWarn` lines become dead and the rule can be
  honoured throughout. Nothing else has to be remembered.

## Alternatives Considered

### Rewrite the guards through a first-party `Guard.NotNull` helper

A single internal helper called from every guard would remove the pattern the
analyzer matches, so the rule would fall silent without any suppression.

Rejected because it changes 315 call sites to buy nothing the reader wanted: the
guard reads no better, every argument check gains a level of indirection, and
the `[CallerArgumentExpression]` ergonomics that make `ThrowIfNull` attractive
are not reproducible on `netstandard2.0` anyway. It would also invent a second
guard idiom alongside the one the rest of the repository uses.

### Bracket every guard with `#if NET6_0_OR_GREATER`

Strictly correct, and honours the rule on the modern leg.

Rejected on legibility: it turns a one-line guard into five, 315 times, in a
library whose argument guards are its most-read lines.

### Polyfill `ArgumentNullException.ThrowIfNull`

Considered first, and the reason this ADR exists. Rejected because it is not
achievable: the member is static on a type that already exists downlevel, and
supplying it would require shadowing `System.ArgumentNullException` itself.

### Suppress globally in `Directory.Build.props` or `.editorconfig`

Cheaper still — one line for the whole repository.

Rejected because it would switch the rule off for projects that *can* honour it,
`FirstClassErrors.GenDoc` first among them, and because `.editorconfig` in this
repository deliberately carries no diagnostic severities (it says so at the top:
style and inspection severities are the DotSettings' job).

### Drop `netstandard2.0` from `JustDummies`

Resolves the finding outright.

Rejected because the floor is a product promise, not an implementation detail:
the package's reach — and the .NET Framework 4.7.2 support floor that ADR-0022
records — is worth more than a style rule.

## Consequences

### Positive

* 323 findings clear: 315 by a suppression that states its reason, 8 by
  complying.
* The constraint is written down where it bites, so the next reader does not
  re-derive it from a build error.
* Projects that can honour the rule still do, and new ones inherit it.

### Negative

* Two project files carry a `NoWarn` that must be removed when the floor moves;
  nothing enforces that removal beyond this ADR.
* A new guard written in `JustDummies` will not be nudged toward the modern
  helper on the modern leg, because the rule is off for the whole project rather
  than for the downlevel inner build only.

### Risks

* Reading the count alone ("55% of the smells gone") overstates the change.
  Nothing about the code improved for the 315; only the report did. The eight
  rewrites in `FirstClassErrors.GenDoc` are the whole of the substantive change.
* A future contributor may take the `NoWarn` as licence to ignore other
  analyzer guidance in these projects. It is scoped to one rule id precisely so
  that reading remains hard to sustain.

## Follow-up Actions

* Remove both `NoWarn` entries, and this ADR's reason for being, if and when
  `JustDummies` drops `netstandard2.0` and `JustDummies.UnitTests` drops
  `net472`.

## References

* ADR-0022 — the .NET Framework 4.7.2 support floor these projects are held to.
* ADR-0011 — `JustDummies` as a standalone package, whose reach the floor serves.
* `JustDummies/JustDummies.csproj`, `JustDummies.UnitTests/JustDummies.UnitTests.csproj` — where the suppression lives.
