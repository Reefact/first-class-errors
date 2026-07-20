# Dummies — Architecture & Design Audit

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./2026-07-20-dummies-architecture-and-design-audit.fr.md)

**Date:** 2026-07-20
**Audited revision:** `3bf89e3` (tip of `main` at audit time)
**Scope:** the `Dummies` library only — `Dummies/`, `Dummies.UnitTests/`, its guard tooling
(`tools/dummies-check/`, `.github/workflows/dummies.yml`), its documentation, and the ADRs that govern it.
**Status:** advisory. Per the repository's own convention (ADR-0004), this audit produces
recommendations, never blockers; every proposed ADR change is a draft for `@reefact` to accept or reject.

**Method.** The whole library source (~8,700 lines across 54 C# files) and test suite (~2,500 lines,
17 files) were read; all 26 ADRs were classified for applicability and the 8 applicable ones reviewed
for both intrinsic quality and implementation compliance; findings were adversarially verified against
the code, and the three behavioral defects reported below were **independently reproduced at runtime**
against the built library. The full unit-test suite was executed: **222/222 pass** (`dotnet test
Dummies.UnitTests`, net10.0 runner). Judgments are calibrated against the library's stated goals —
readable tests, expressive test data, opt-in determinism, a fluent discoverable API, simplicity — and
deliberately *not* against the goals of property-based-testing or fuzzing frameworks, which this
library explicitly is not.

---

## 1. Executive Summary

Dummies is a young library built to an unusually high standard. Its central architectural idea — map
every discrete type into a shared 64-bit ordinal space so that one engine owns bounds, exclusions,
conflict detection, and sampling for thirteen builders at once — is elegant and correctly executed.
Its error-message discipline (every conflicting constraint names *both* sides, every generation
failure names the seed that replays it) is better than that of most mature libraries in this space.
Its ADR base is exemplary: decisions are recorded with honest constraints, real alternatives, and
priced trade-offs.

The audit nevertheless found **three genuine behavioral defects**, all reproduced at runtime:

1. **Critical — `AnyDecimal` can never generate the upper half of its range.** A fraction intended
   to be uniform in [0, 1) is constructed from three 31-bit draws against a 96-bit denominator and
   tops out near 0.5; `Any.Decimal().Between(0m, 100m)` never exceeds ~49.9999
   (`DecimalIntervalSpec.cs:145`).
2. **Major — `AnySingle`/`AnyHalf` exclusion nudge stalls.** The exclusion-collision walk steps by a
   *double* ulp instead of the type's own ulp, so quantization lands back on the same value and a
   satisfiable spec such as `Any.Half().Between((Half)1f, (Half)1.001f).DifferentFrom((Half)1f)`
   throws `AnyGenerationException` for ~half of all seeds (`ContinuousIntervalSpec.cs:189`).
3. **Major — a regex character-class range ending at `￿` hangs forever.** The class-expansion
   loop increments a 16-bit `char` that wraps at `0xFFFF`, so
   `Any.StringMatching(@"[ -￿]")` never returns (`RegexParser.cs:398`).

All three share one root cause worth naming: **the test suite asserts membership, never
reachability.** Tests check that generated values satisfy the constraints; no test checks that the
whole declared domain is reachable, or that a declared-satisfiable spec actually generates. That is
the precise blind spot in an otherwise well-designed suite, and closing it matters more than any
individual fix.

One framing fact softens all of this considerably: **Dummies has never been released.** There is no
`dum-v*` tag; the changelog holds only an empty *Unreleased* section. Every defect above can be fixed,
and every contract decided, with zero compatibility cost. This audit's headline recommendation is to
treat the pre-1.0 window the way ADR-0020 did — as the cheapest moment to decide — and close the
items in §11–§12 before the first publication.

Beyond the defects, the significant findings are: the hand-mirrored `Any`/`AnyContext` surface and
the fourteen cloned numeric builders carry **no parity guard** (and documentation drift has already
begun); the **determinism contract has documentation gaps** (concurrent draws inside one seeded scope
silently void replayability; cross-version seed stability is neither promised nor disclaimed; the
contract's ADR anchoring was lost when ADR-0006 was superseded); the **netstandard2.0 leg is never
executed by Dummies' own test suite** (only transitively, via FirstClassErrors' floor job); and there
is **no user-facing reference of the constraint surface** — the repository README does not even
mention the package. The feature-gap analysis (§10) finds the type coverage genuinely complete for
the library's philosophy; the two absences that qualify as surprising are a *top-level* choice
combinator (`Any.OneOf<T>(params T[])` / `Any.ElementOf(...)`) and exclusion constraints on
`AnyString` — the only scalar builder without them.

## 2. Overall Assessment

**Verdict: a very strong pre-release library — architecture and process are its strengths; value-space
correctness testing is its one systemic weakness.**

Judged area by area against the stated goals:

| Area | Assessment |
|---|---|
| Architecture | Excellent. Clean layering (public builders → internal specs → sampling), one shared ordinal engine, principled engine split, composition seams over one tiny interface. |
| API design | Excellent, with a handful of deliberate-looking but unrecorded asymmetries (§8). |
| Error diagnostics | Exceptional — the library's signature strength. |
| Determinism | Sound design, correctly implemented at the `AsyncLocal`/`ExecutionContext` level; contract under-documented at its edges (§7.3). |
| Correctness | Three reproduced defects, two of them in exactly the code a membership-only test suite cannot see (§4.1). |
| Testing strategy | Well-shaped (behavior-first, black-box, oracle-backed for regex, flake-safe) but reachability-blind (§9.3). |
| Documentation | XML docs outstanding; user-facing documentation thin and hard to discover (§4.4). |
| Maintainability | Duplication is large but disciplined (zero copy-paste slips found in the clone families); the risk is unguarded drift, not present-day rot (§9). |
| ADR base | Exemplary quality; two structural gaps — the determinism contract and the ordinal engine have no ADR of their own (§5). |

The overall shape is characteristic of a library written with great care by a small number of hands:
the *decisions* are consistently right and consistently recorded, while the safety nets that protect
those decisions from future hands (parity guards, reachability tests, API baselines) are not yet in
place. Pre-1.0 is the moment to install them.

## 3. Strengths

These are earned, verified against the code, and worth preserving deliberately.

### 3.1 The ordinal-space unification

Every discrete type — all ten 64-bit-or-narrower integers, `DateTime`, `DateTimeOffset`, `TimeSpan`,
`DateOnly`, `TimeOnly` — maps order-preservingly into unsigned 64-bit ordinal space
(`OrdinalMapping.FromInt64` flips the sign bit; `OrdinalIntervalSpec.cs:9-23`) and shares **one**
engine for bounds, allow-lists, exclusions, conflict detection, cardinality, and sampling
(`OrdinalIntervalSpec`). The exclusion algorithm is exact — a drawn index is mapped onto the k-th
non-excluded ordinal in a single pass over a sorted exclusion list (`OrdinalIntervalSpec.cs:194-202`)
— so generation is one draw, never draw-and-retry. A fix to a conflict message or an edge case
reaches every discrete builder simultaneously. This is the right level at which to be DRY: the
*logic* is shared while the thin per-type facades stay simple and readable.

### 3.2 Constraint provenance and eager validation

Every bound remembers the constraint string that set it (`"Between(1, 6)"`, `"Positive()"`), so a
conflict names **both** sides at the moment of declaration:

```
Cannot apply LessThan(10) because GreaterThan(100) already requires values greater than or equal to 101.
```

The discipline holds uniformly across every builder and spec engine, including
cross-cutting validations one would not expect to find (a `Numeric()` charset rejects a prefix
containing letters, naming the offending character — `StringSpec.cs:254-275`). Combined with eager
satisfiability checking ("a generator that exists can always generate"), an impossible `Arrange`
fails at the line that wrote it, not at some later draw. This is the library's signature, and it is
executed consistently.

### 3.3 The determinism machinery is done right at the hard level

The linchpin — generators store a `RandomSource` and resolve `.Current` only at `Generate()` time —
is what lets a recipe built outside `Any.Reproducibly(...)` generate deterministically inside one
(`RandomSource.cs:3-9`). The `AsyncLocal` scope semantics were scrutinized closely and hold: the
sync overload's `using`-restore is correct; the async overload's `UseSeed` mutation cannot leak to
the caller (an async method's `ExecutionContext` mutations do not flow back); nesting restores the
outer scope untouched; `ConfigureAwait(false)` is immaterial to `ExecutionContext` flow. The seed is
reported end-to-end: a user factory that throws inside `.As(...)` produces an
`AnyGenerationException` naming the generated value *and* the seed (`AnyDerivation.cs:59-73`); a
distinct-collection exhaustion does the same (`CollectionState.cs:246-254`).

Two subtle dual-target traps were caught in advance and documented at the exact point of danger:
`RandomSampling`'s inclusive sampler is deliberately *not* named `NextInt64` because on the net8.0
leg the framework's own exclusive-bound instance method would win overload resolution and silently
change semantics (`RandomSource.cs:129-137`); and the `OrNull` extension is split into two classes
because `struct`- and `class`-constrained overloads of one name would collide
(`NullableExtensions.cs:47-52`). This is the kind of care that cannot be retrofitted.

### 3.4 Bounded escapes everywhere — no unbounded retry anywhere

The library's "built to satisfy, never generate-then-filter" claim survives scrutiny with three
honest, ADR-recorded exceptions, each *bounded*: the distinct-collection dedup draw (budgeted,
coupon-collector-generous, reset-on-progress — `CollectionState.cs:236-244`), the continuous-domain
exclusion nudge (walks to the neighbouring representable value), and `AnyGuid`'s collision escape (a
full-width carry increment that provably terminates — `AnyGuid.cs:27-36`). Each failure mode
produces an actionable, seed-bearing message instead of a hang.

### 3.5 Care at the edges

Small things that reveal the quality bar: `AnyDateTime.OneOf` remembers callers' original values so
the ordinal round-trip does not silently normalize `DateTimeKind` (`AnyDateTime.cs:124-131`);
collection layout is Fisher-Yates-shuffled so a dummy collection never advertises a positional
invariant a test might accidentally rely on (`CollectionState.cs:46-53`); `CountSpec` and
`StringSpec` saturate rather than overflow on huge declared minima; `IAny<out T>` covariance means
the read-only collection interfaces (`IReadOnlyList<T>`, etc.) are served for free.

### 3.6 The regex subsystem is well-built for its decided scope

The hand-written recursive-descent parser (`RegexParser.cs`, 457 lines — the largest single piece of
logic in the library) is well-structured, why-commented, and disciplined about its two-channel
rejection taxonomy: `ArgumentException` for malformed patterns, `UnsupportedRegexException` naming
the construct and position for well-formed-but-non-regular ones. The test suite validates generated
strings against the **real .NET regex engine as an oracle** over a fixed-seed corpus — exactly the
right way to test a generator. (Defects found at its edges are cataloged in §4.1 and §4.2; they do
not change the assessment that the ADR-0025 approach was sound and honestly argued.)

### 3.7 Packaging, boundary, and process

The zero-dependency, error-agnostic boundary is enforced three ways: a `.csproj` comment stating the
rule, an intent-based architecture test that fails on any non-BCL assembly reference
(`ArchitectureTests.cs:27-37`), and the `dummies-check` packaged-asset guard — a real consumer
program, run in CI against the *packed artifact* per target framework, that proves the net8.0 asset
carries the modern generators, the netstandard2.0 asset does not, constraints and conflicts behave,
and same-seed contexts replay (`tools/dummies-check/Program.cs`). Packaging itself is
production-grade: SourceLink with embedded untracked sources, deterministic CI builds, snupkg
symbols, an SPDX SBOM embedded at pack time, provenance-attested release assets
(`Directory.Build.props:10-23`, `Dummies.csproj:60-66`, `release.yml`). The ADR base recording all
of this is discussed in §5 — it is a strength in itself.

### 3.8 The API philosophy is coherent and documented where users look

The "constraints express what the surrounding code *requires*, never what the test asserts" idea is
stated on the entry point, on every builder, in the package README, and in the user guide — the same
sentence, deliberately. The no-clock-relative-constraints stance (`AnyDateTime` has no
`InThePast()`) is documented at every point a user would look for it, with the reproducibility
rationale attached. Intention-revealing near-synonyms are honestly explained: `DifferentFrom(x)` is
documented as semantically `Except(x)` with a name that carries intent; `Containing` (a value known
now) vs `ContainingAny` (a generator drawn at build time) is a genuinely useful distinction.

## 4. Weaknesses

Ordered by severity. Items 4.1 and 4.3 are the ones that should gate a first release.

### 4.1 Reproduced behavioral defects

**(a) `AnyDecimal` never reaches the upper half of any range — critical.**

`DecimalIntervalSpec.cs:144-149`:

```csharp
// A uniform-enough fraction in [0, 1): 93 random bits over the full decimal mantissa scale.
decimal fraction = new decimal(random.Next(), random.Next(), random.Next(), false, 28) / MaxFraction;
decimal mid       = _min / 2 + _max / 2;
decimal half      = _max / 2 - _min / 2;
decimal candidate = Clamped(mid + (fraction * 2 - 1) * half);
```

`Random.Next()` returns a non-negative `int`, so the top bit of **each 32-bit limb** of the 96-bit
mantissa is always zero, while `MaxFraction` is the *full* 96-bit mantissa maximum
(`7.9228…`, `DecimalIntervalSpec.cs:14`). The fraction therefore lives in [0, ~0.49999986], not
[0, 1); `(fraction * 2 - 1)` lives in [−1, ~0); and every candidate lands in `[min, mid)`. The
inclusive maximum documented on `AnyDecimal.Between` (`AnyDecimal.cs:112`) is unreachable — as is
everything above the midpoint. Independently reproduced for this audit: the maximum of 200,000 draws
of `Any.Decimal().Between(0m, 100m)` was **49.99992…**.

Why it matters beyond the obvious: a test using `Any.Decimal().Between(0m, 100m)` to exercise "any
valid percentage" silently never exercises 50–100 — the library's core promise ("arbitrary yet
valid, so hidden assumptions surface") is inverted into a hidden assumption of its own. The fix is
small: build the fraction from 96 genuinely uniform bits, e.g.

```csharp
// after: 12 random bytes fill all three 32-bit limbs uniformly
// (the decimal ctor reads the int limbs as raw 32-bit patterns)
byte[] limbs = new byte[12];
random.NextBytes(limbs);
decimal fraction = new decimal(
    BitConverter.ToInt32(limbs, 0),
    BitConverter.ToInt32(limbs, 4),
    BitConverter.ToInt32(limbs, 8),
    false, 28) / MaxFraction;
```

(any construction that fills all 96 mantissa bits uniformly is fine — the current three `Next()`
calls fix the top bit of each limb at zero and can never draw a limb of `2^31−1`), then add the
reachability test from §11 item 2. Note the comment's own claim ("93 random bits over the full
mantissa scale") documents an intent the code does not meet — and even 93 well-placed bits would
not reach a 96-bit denominator's upper octant.

**(b) `AnySingle`/`AnyHalf` exclusion nudge stalls on satisfiable specs — major.**

`ContinuousIntervalSpec.cs:188-198`: when a drawn value collides with an excluded point, the walk
steps with the **static, double-space** `NextUp` (line 189) instead of the *type-aware* `_nextUp`
lambda that `AnySingle`/`AnyHalf` supply precisely for stepping in their own representable ladder
(`AnySingle.cs:20`, `AnyHalf.cs:22`) — and which the exclusive-bound paths already use correctly
(lines 120, 125). One double-ulp above a representable `float`/`Half` re-quantizes to the same
value, the `next > _max` escape at line 190 is unreachable (`Quantized` clamps to `_max` first,
lines 203-209), so the 128-step budget burns and a *satisfiable* spec throws. Independently
reproduced: `Any.Half().Between((Half)1f, (Half)1.001f).DifferentFrom((Half)1f).Generate()` threw
`AnyGenerationException` for **250 of 500 seeds**; the identical `AnyDouble` scenario never throws
(its quantize is identity). The fix is one token — `Quantized(_nextUp(candidate))` — plus a
regression test per continuous type.

This defect is worth a design note: it is exactly the failure class the library's own architecture
predicts. The engine was parameterized by `quantize`/`nextUp` lambdas *because* narrow types must
step in their own ladder; one call site inside the same file forgot the parameter. A
cross-engine, parameterized scenario suite (§9.3) is the structural answer.

**(c) A character-class range ending at `￿` hangs forever — major.**

`RegexParser.cs:398`:

```csharp
for (char character = low; character <= high; character++) { set.Add(character); }
```

When `high == '￿'` (reachable through the supported `\uHHHH` escape), the 16-bit `char` wraps
to `0x0000` and `character <= high` is always true. Independently reproduced:
`Any.StringMatching(@"[ -￿]")` did not return within five seconds (hard hang), while the
same pattern is valid .NET regex. A declaration-time hang is the worst failure mode this library
can exhibit — its identity is *failing fast with a named cause*. Fix: guard the wrap
(`if (character == high) break;` inside the loop, or iterate an `int`), and mirror the check in the
private twin loop `RegexAlphabet.Range` (`RegexAlphabet.cs:66-71`) for defense in depth.

**(d) Balancing groups and invalid group names are silently accepted — major, contract-breaking
direction.**

`SkipGroupName` (`RegexParser.cs:295-300`) scans to the terminator with no validation. Consequently
`(?<-a>x)` — a *balancing group*, non-regular, same family as the backreferences the library
proudly rejects — is treated as an ordinary named group: `Any.StringMatching(@"(?<a>y)?(?<-a>x)")`
generates `"x"`, which the real engine does **not** match (verified: the pattern's language is
exactly `{"yx"}`). Invalid group names (`(?<a b>x)`) are likewise accepted where .NET rejects them.
This is the single place the audit found where the library's signature promise — *"a clear error
beats a value which does not actually match"* (ADR-0025) — is broken. The fix is local: validate
the captured name (reject `-` as `Unsupported("a balancing group …")`, reject non-word characters
as `Malformed(...)`).

**(e) Minor defects in the same subsystem.** The generation-limit exception blames "a nested
unbounded quantifier" even when the true cause is a large *bounded* quantifier
(`(a{1000}){1000}` — message asserts a diagnosis that is false; `RegexNode.cs:31-37`); a few
patterns the real engine accepts are conservatively refused (`^*`, `abc$$` — while `^^abc` is
accepted, an avoidable asymmetry; a leading `-[` in a class is misread as subtraction); and a
well-formed negated class whose members lie outside the printable universe is misclassified as
*malformed* instead of *unsupported*. All of these fail in the safe direction (refusal, never
mis-generation) and are cosmetic next to (c) and (d).

### 4.2 The "printable ASCII" claim is overstated in three places

`RegexAlphabet.cs:3-9`, `AnyPattern.cs:15-16`, and `Any.cs:70` all claim every terminal resolves to
printable ASCII (0x20–0x7E). The code — correctly — emits exactly the characters the pattern
demands: `\t`, `\a`, `\cA`, `\0`, and `\uHHHH` literals can be non-printable or non-ASCII, and the
library's own test asserts it (`AnyPatternTests` — `\a` → U+0007). The restriction genuinely
applies only where the pattern leaves the character *free* (shorthands, the dot, negated classes).
Since ADR-0025 explicitly declares the character universe a behavior consumers may rely on, the
three doc sites should say precisely that (§11 item 6).

### 4.3 Hand-mirrored surfaces with no parity guard, and drift has already begun

Two mirror structures must agree method-for-method, and nothing checks either:

* **`Any` ↔ `AnyContext`**: every scalar entry point exists twice (21 on the netstandard2.0 leg, 26
  on net8.0, counting both `StringMatching` overloads) — `Any.cs:54-317` vs `AnyContext.cs:48-296`.
  The mirror is legitimate design (composition and collections are deliberately *not* mirrored —
  they inherit a context through operand sources, which is elegant), but a new scalar type added to
  `Any` and forgotten on `AnyContext` would compile, pass all 222 tests, and ship a hole in the
  deterministic surface. Wording drift is already visible inside `AnyContext` itself (two different
  determinism phrasings across its factories; its `Guid()` doc mentions `Any.Reproducibly`, which a
  fixed context ignores by design).
* **The fourteen numeric builders** are byte-identical clones modulo type substitution (~2,450
  lines; the signed quartet, unsigned quartet, continuous trio, and wide pair; the five temporal
  builders follow the same pattern for ~800 more). To the clone families' credit, a scripted scan
  found **zero copy-paste slips** in the code itself — but three XML summaries say "Same constraint
  algebra as `AnyInt32`" on builders where it is literally false (unsigned types lack
  `Positive`/`Negative`; temporal types rename the bound family), and three test DisplayNames still
  claim generators "convert implicitly to their value type"
  (`AnyContinuousTests.cs:108`, `AnySignedIntegerTests.cs:87`, `AnyUnsignedIntegerTests.cs:76`) —
  conversions ADR-0020 removed. A stale comment in `SeedReproducibilityTests.cs:17-18` explains
  code by those same removed conversions.

The absence of guards is the finding; the mitigation analysis and recommendation (reflection-based
parity tests, *not* a generic base class) is in §9.2.

### 4.4 Documentation reaches neither the discoverer nor the power user

* The **repository README never mentions Dummies** (verified: zero occurrences), while the package
  README points to the repository for "full documentation". A NuGet discoverer lands on a front
  page about a different library; the closest thing to a Dummies guide
  (`ArbitraryTestValues.en.md`) is a FirstClassErrors.Testing integration guide that defers back to
  "documented with Dummies itself" — a circular reference.
* **No user-facing reference documents the per-builder constraint surface.** Where does a user
  learn that `Except`/`OneOf`/`DifferentFrom` exist on numerics, that `WithLengthBetween` exists,
  that `ContainingAny` differs from `Containing`, or which regex dialect `StringMatching` supports?
  Today: only IntelliSense, one builder at a time. ADR-0025's own follow-up ("document the
  supported dialect") is still open.
* The **empty-by-default surprise** (an unconstrained collection can have 0 elements, an
  unconstrained string can be empty) is well-documented in XML remarks but absent from the package
  README, where a skimming user would most benefit from it — it is a deliberate,
  philosophy-bearing choice ("a test iterating an unconstrained collection zero times is a hidden
  assumption surfacing") and deserves to be advertised as such.

### 4.5 Determinism contract gaps (documentation, not implementation)

Detailed in §7.3: concurrent draws inside one seeded scope silently void replayability (and race a
non-thread-safe `System.Random`) — documented nowhere; seed reports can name a wrong or
inapplicable seed for fixed-context and mixed-source compositions; cross-version and cross-TFM
seed-sequence stability is neither promised nor disclaimed; and the whole contract lost its ADR
anchor when ADR-0006 was superseded.

### 4.6 The netstandard2.0 leg is never executed by Dummies' own suite

`Dummies.UnitTests` targets net10.0 only. The netstandard2.0 assembly — the one .NET Framework
consumers will load — is exercised only *transitively*: the FirstClassErrors floor job
(`ci.yml:98-115`) runs `FirstClassErrors.UnitTests` on net472, which arranges with `Dummies.Any`
via project reference and the Testing factories, so Dummies does load and generate on the real
.NET Framework CLR — but its own 222-test contract suite (regex oracle, conflict detection,
distinctness gating, seed reproducibility) never runs there, and same-seed-same-values across the
two packaged assets is asserted nowhere. The repository already owns the exact machinery needed
(`build/Net472TestFloor.props`, used by `FirstClassErrors.UnitTests`); extending it to
`Dummies.UnitTests` (with the net8-only tests conditioned out) is mechanical. See ADR-0022
compliance, §6.

### 4.7 Release-engineering guardrails not yet installed

No public-API baseline (`Microsoft.CodeAnalysis.PublicApiAnalyzers`), no
`EnablePackageValidation`/ApiCompat. The changelog commits Dummies to semantic versioning while the
audit itself demonstrates the API surface is hand-mirrored and already drifting in documentation;
breaking-change detection against a shipped baseline is the complementary mechanism parity tests
cannot replace (a removed overload or narrowed return type passes a mirror test). Pre-first-release
is the cheapest moment to install both. One stale comment found here: `Directory.Build.props:3-9`
says the repository ships "FirstClassErrors and FirstClassErrors.Testing" — it omits Dummies, the
very package those pack-time properties now also govern.

## 5. ADR Review

Eighteen of the twenty-six ADRs do not concern Dummies (they name the analyzers, the request
binder, GenDoc/CLI tooling, the Outcome API, or repository process). Eight apply, and their quality
was reviewed individually. The overall standard is high enough to say plainly: this ADR base is a
model of the form. Decisions carry honest constraints, genuinely-considered alternatives, priced
negatives, and follow-ups that were actually executed.

### ADR-0006 — Supply arbitrary test values from a single seedable source *(Superseded)*

**Quality: exemplary, historically.** The constraints were real (zero-dependency promise,
netstandard2.0 parallel-test safety without `Random.Shared`), the four alternatives were fairly
weighed, and its follow-ups (extract the engine when a second consumer appears; consider an xUnit
adapter) were honored or consciously deferred. Its collision-risk analysis of the unseeded default
is exactly the right depth. **Issue:** its supersession created a gap — see "structural gaps" below.

### ADR-0011 — Host Dummies as a standalone package *(Accepted)*

**Quality: good.** The name/identity/boundary reasoning is sound and the no-reference rule is
machine-checked. Two precision nits. First, the *enforced* invariant is stronger than the *recorded*
one: the architecture test forbids **any** non-BCL reference (`ArchitectureTests.cs:27-37`), and
ADR-0025 leans on a "zero-dependency identity … the boundary is machine-checked (ADR-0011)" — but
ADR-0011's decision text only forbids referencing *FirstClassErrors projects*. The
zero-*third-party*-dependency rule, load-bearing for ADR-0025's whole argument, is written down
nowhere as a decision. Second, the alternatives never weigh the risks of the ultra-generic NuGet ID
`Dummies` (squat/collision/searchability) — a package identity the ADR itself calls costly to
rename. Neither nit changes the decision; both deserve a line in the record.

### ADR-0013 — Gate distinct collections by cardinality, else bounded draw *(Accepted)*

**Quality: outstanding.** The soundness argument — count only the elements the generator must
supply, credit `Containing` values outside its domain, treat opaque `ContainingAny` draws
conservatively, let the bounded draw be the final safety net — is stated in the document and
provably mirrored in the code (`CollectionState.Validate`/`CardinalityCap`/`FixedOutsideCount`).
The risks section even anticipates budget mistuning and instructs "revise based on evidence rather
than describing failure as impossible." **Issue (shared with ADR-0015):** it defers "the exact hint
interface, collection state, draw budget, exception payload, and seed propagation" to the
implementation reference — but the reference's Dummies section
(`adr-implementation-reference.md:58-68`) records none of those specifics (no budget numbers, no
exception payload, no seed-propagation rule). The pointer promises more than the destination holds;
either enrich the reference or soften the pointer.

### ADR-0015 — Cap Any.Combine at arity eight *(Accepted)*

**Quality: good.** Honest about the ceiling being heuristic, with a defined escape hatch (add
arities compatibly via a new decision on evidence). The alternatives are real. The same
implementation-reference pointer nit as ADR-0013 applies.

### ADR-0020 — Materialize dummies only through Generate() *(Accepted)*

**Quality: exemplary — the best document in the base.** Concrete evidence (the syntactic shapes
where the conversion silently misbehaved, drawn from the suite itself), three fairly-weighed
alternatives including the analyzer route it deliberately declines, honest costs, and the pre-1.0
timing argument stated as such. It also demonstrably steered later work (ADR-0026 reuses both its
reasoning pattern and its risk framing). No changes recommended.

### ADR-0022 — Floor the library's .NET Framework support at 4.7.2 *(Accepted)*

**Quality: sound policy; scope wording aged.** "A compatibility promise that is not exercised
cannot provide a trustworthy support boundary" is the right principle. But the ADR predates Dummies
and speaks of "the shipped `netstandard2.0` libraries" without naming them; whether Dummies is
inside its scope is now a matter of inference, and the floor job does not include it (§6). When the
maintainer next touches this area, a one-line clarification of covered packages would close the
ambiguity — or the Dummies-specific floor decision can ride the new determinism ADR proposed below.

### ADR-0025 — Generate matching strings from a home-grown regular subset *(Proposed)*

**Quality: an unusually honest build-vs-buy record.** The rejection of Fare is argued on identity
and error-contract grounds (silent dropping of non-regular constructs vs first-class refusal), not
on FUD; the "non-regular constructs are impossible for *any* finite generator, so the subset is not
a convenience cut" framing is exactly right; the terminal-generator decision is well-argued.
**Issues:** (1) It is still **Proposed** while fully implemented, shipped in the package README,
and *load-bearing for the Accepted ADR-0026* (whose `ErrorCodeFactory` is built on
`StringMatching`) — until the status flips, an accepted decision formally rests on an undecided
one. The audit's role is to flag it; only `@reefact` flips a status. (2) The "terminals draw from
printable ASCII" rationale sentence is imprecise — `\s` includes tab (0x09) and explicit escapes
emit exactly the character they name (§4.2); the wording should be corrected *before* acceptance,
since the ADR itself declares the universe a compatibility-relevant behavior. (3) It cites "a
property test" against the real engine; what exists is a fixed-seed, fixed-corpus oracle test in
the unit-test project — excellent, but not property-based; the text should say what the safety net
is.

### ADR-0026 — Rebase the testing package's arbitrary values on Dummies *(Accepted)*

**Quality: a thorough consolidation record** — six real alternatives, the one-seed-story rationale,
honest interim-packaging risk. **Two precision drifts:** (1) the decision text says each factory
exposes "an `IAny<T>` generator through a distinct method where composition is needed" — no factory
exposes any such method today (verified: zero `IAny` occurrences in `FirstClassErrors.Testing`
sources). Defensible YAGNI, but the text reads as a decided API shape, and a compliance check a
year from now cannot tell deliberate deferral from unfinished migration. (2) Its risk clause says
the double-assembly hazard exists "precisely because Dummies types appear in Testing's public API"
— today none do; the premise is misstated (the hazard is real for other reasons while Dummies ships
inside the artifact). Since accepted ADRs are never edited in place, both belong as a short note in
the implementation reference.

### Structural gaps in the base (Create-recommendations)

1. **Dummies' determinism contract has no accepted ADR.** The `AsyncLocal` ambient source, opt-in
   `Reproducibly`, lazy pinning, seed-on-failure reporting — the crown-jewel guarantee — was decided
   in ADR-0006, which is now Superseded *and* was scoped to FirstClassErrors.Testing; ADR-0026's
   decision is about rebasing Testing, not about Dummies' own contract. A future maintainer asking
   "why `AsyncLocal` and not a parameter? why is raced `System.Random` acceptable?" finds the
   reasoning only in a superseded record. **Recommend drafting one Proposed ADR** ("Dummies supplies
   arbitrary values from an ambient, seedable, execution-context-local source with opt-in
   reproducibility") carrying ADR-0006's rationale forward and settling, in the same document, the
   open edges this audit surfaced: single-logical-flow concurrency semantics, the closed
   `IHasRandomSource` seam, and the cross-version seed-stability policy (§7.3).
2. **The ordinal-engine architecture has no ADR.** One shared 64-bit ordinal space with four
   arithmetic-substrate engines is a lasting, questionable-by-a-future-maintainer decision
   (why four engines? why is `decimal` not ordinal-mapped?) that currently lives only in internal
   XML docs. It passes the repository's own ADR test ("if the implementation changed but the
   decision stood…"). A short Proposed ADR would fix the asymmetry with far smaller decisions
   (arity caps) that did get records.

## 6. ADR Compliance

| ADR | Status | Compliance of the implementation |
|---|---|---|
| 0006 (historical) | Superseded | **Compliant and exceeded.** The inherited seeding contract (context-local, opt-in determinism, seed reporting) is implemented faithfully; Dummies adds the isolated `AnyContext` the original ADR only anticipated. |
| 0011 | Accepted | **Compliant.** No FirstClassErrors reference; boundary machine-checked (`ArchitectureTests`); standalone identity, release train, and docs in place. Note: enforcement is *stronger* than the recorded decision (§5). |
| 0013 | Accepted | **Compliant, verified in detail** — eager gate net of outside-domain `Containing` credits, conservative `ContainingAny` accounting, overflow-safe arithmetic, bounded budget, both failure channels. **One minor deviation:** the exhaustion message *unconditionally* promises `Any.Reproducibly({seed}, …)` replay (`CollectionState.cs:246-254`; the `seed is not null` guard is dead code — the seed can never be null there). For a **foreign** element generator whose draws ignore the ambient source, that promise is false; the ADR says failures are "explicit and reproducible". Qualify the message when the element generator carries no library source. |
| 0015 | Accepted | **Compliant exactly** — arities 2–8, no more; suppressions localized with ADR-referencing justifications (`Any.cs:622-623`); ceiling documented on the arity-8 overload. |
| 0020 | Accepted | **Fully compliant.** No implicit conversions anywhere; `Generate()` is the sole materialization; builders verified immutable (every fluent method returns a new instance). Residue: three test DisplayNames and one comment still *describe* the removed conversions (§4.3). |
| 0022 | Accepted | **Partial for Dummies.** The netstandard2.0 asset is loaded and driven on net472 only transitively through FirstClassErrors' floor job; Dummies' own suite never runs there, and the package README states no .NET Framework floor at all (FirstClassErrors' README does). Close before first publication (§11 item 5). |
| 0025 | Proposed | **Compliant on every major clause** (home-grown parser, first-class rejection, terminal generator, zero dependencies, printable-ASCII *default* universe, bounded unbounded-quantifier spread). The §4.1(c)/(d) defects are quality bugs *within* the decided scope, not deviations — with the caveat that (d) breaks the rejection *promise* the ADR records. One taxonomy edge: a well-formed negated class outside the printable universe raises `ArgumentException` ("malformed") instead of `UnsupportedRegexException`. |
| 0026 | Accepted | **Compliant on every executed clause** — single engine, single seed scope, `Testing.Any` removed, factories shipped, clock/ids on the ambient context, docs updated EN/FR. The unimplemented "distinct `IAny<T>` method" half and the misstated risk premise are recorded in §5. |

## 7. Architecture Review

### 7.1 Layering and shape

The library is three clean layers: **public fluent builders** (thin, per-type, sealed, immutable) →
**internal spec engines** (`OrdinalIntervalSpec`, `WideIntervalSpec`, `ContinuousIntervalSpec`,
`DecimalIntervalSpec`, `StringSpec`, `CountSpec`, `CollectionState<T>`) → **sampling primitives**
(`RandomSampling`). Public surface never leaks internal types; internal engines never touch the
ambient state directly (sources are passed down). The composition seams — `.As(factory)`,
`Any.Combine(...)`, the collection factories — are all defined over the one-member `IAny<out T>`,
which is as small as an interface can be (ISP by construction) and covariant, so derived and foreign
generators flow through every seam uniformly.

The **four-engine split is principled, not accidental**: 64-bit-mappable discrete types share
`OrdinalIntervalSpec`; 128-bit integers need `WideIntervalSpec` only because netstandard2.0 has no
`UInt128` (the two are verbatim siblings — the one regrettable, TFM-forced duplication); IEEE floats
need continuous sampling with type-aware quantization; `decimal` is neither ordinal-mappable (96-bit
mantissa × scale) nor IEEE. Each engine's existence is justified by its arithmetic substrate. What
is *missing* is the ADR recording this (§5), and — as §4.1(b) showed — a parameterized test suite
exercising each engine through each of its type facades.

The **collection hierarchy** is a textbook-clean CRTP:
`AnyCollection<TItem, TResult, TSelf>` holds the shared fluent count/containment surface returning
`TSelf` (without the classic unsafe `(TSelf)this` cast — concrete types implement a
`With(state)` factory), and the five concrete builders add only element shaping and the
`Build(List<TItem>)` conversion. The exception is `AnyDictionary`, which cannot inherit the base
(its element is a pair) and therefore **duplicates the entire count facade verbatim** (~60 lines,
`AnyDictionary.cs:51-113`) and offers no containment constraint at all — the one place in the
collection family where sharing failed. Extracting the count facade over `CollectionState` (or
adding `ContainingKey`, which would ride the existing key-state machinery for free) would close
both the duplication and the acknowledged test hole (`AnyCollectionTests.cs:161-163` comments on
it).

### 7.2 Extensibility

**For users, the design is closed, and that is a legitimate but undocumented choice.** `IAny<T>` is
public, so anyone can implement a generator and compose it through `As`/`Combine`/collections. But
`RandomSource`, `IHasRandomSource`, and `ICardinalityHint<T>` are all internal, so a foreign
generator (a) cannot draw from the ambient seeded source — under `Any.Reproducibly` its values do
not replay, and (b) cannot advertise a finite domain — a distinct collection over it always takes
the bounded-draw path (safe, and exactly what ADR-0013 promises). The degradation is graceful
everywhere (verified: `OrNull` falls back to the ambient source for the null coin; `Combine`
propagates `null` sources without failing). What is missing is one honest paragraph on `IAny<T>`'s
XML doc telling implementers where they stand — today the contract is discoverable only by reading
internal code. If the seam is ever to open, an `ISeedableAny` in a minor release is the natural
shape; nothing needs deciding now except the documentation.

**For maintainers**, adding one new scalar type touches 6–9 files (builder, `Any`, `AnyContext`,
tests, user docs EN/FR, package README, `dummies-check` if net8-only, possibly a spec engine). The
process is mechanical but real, and only partially guarded (§9.2).

### 7.3 The determinism machinery — deep dive

The implementation is correct at the level that is hard to get right (§3.3). The remaining risks
are all *contract-documentation* risks, and they cluster into four:

**(a) Concurrency inside one seeded scope silently voids replayability — undocumented.** An
`AsyncLocal` copies the *reference*: `Task.Run`/`Parallel.ForEach` children inside one
`Reproducibly` body all see the same `SeededRandom` instance. Two consequences. First, even with
benign interleaving, the draw *order* becomes scheduler-dependent, so the reported seed no longer
replays the run — the exact guarantee the feature exists for. Second, `System.Random` is not
thread-safe, and netstandard2.0 offers no thread-safe alternative; a racing draw can corrupt state
(on .NET Framework, a raced `Random` can degrade to returning zeros). The docs carefully explain
that the source "never leaks *across* tests running in parallel" (true — different logical flows)
but say nothing about parallelism *within* a body. The fix is one honest paragraph on
`Reproducibly` ("a seeded run is single-logical-flow; concurrent draws inside the body are neither
replayable nor safe") — plus, optionally, recording in the new determinism ADR why per-flow
forking (a child source per `Task.Run`) was not attempted (it would change every sequence and
complicate `WithSeed`; the honest restriction is the right V1).

**(b) Seed reports can name a wrong or inapplicable seed in mixed/fixed-source composition.**
`Combine` propagates the **first non-null** operand source for failure reporting
(`Any.cs:446` et al.). `Any.Combine(Any.WithSeed(1).Int32(), Any.WithSeed(2).Int32(), throwing)`
fails with "seeded with 1; reproduce with `Any.Reproducibly(1, …)`" — doubly wrong: seed 2 goes
unreported, and the instruction is inapplicable because `Reproducibly` pins the *ambient* source,
which `FixedRandomSource`-backed generators ignore by design. This is an edge case (mixing seeded
contexts inside one composition is unusual), but the failure mode is a *confidently misleading
diagnostic* in the library whose signature is diagnostic honesty. A small fix reaches it: let the
source kind produce the replay hint (ambient → "reproduce with `Any.Reproducibly({seed}, …)`";
fixed → "this generator draws from `Any.WithSeed({seed})`, which already replays by itself"), and
have `Combine` collect distinct sources rather than the first.

**(c) Cross-version and cross-runtime seed stability is neither promised nor disclaimed.** The
package description says "any run is reproducible from a reported seed" without qualification.
Within one process this holds. Across *library versions*, any change to draw order or count
silently changes every sequence — and ADR-0025 already acknowledges consumers may rely on generated
shapes. Across *runtimes*, seeded `new Random(seed)` retains the legacy algorithm on modern .NET
precisely for compatibility, so the common surface should agree between the netstandard2.0 and
net8.0 assets — but nothing tests it (§4.6), and `Random`'s documentation explicitly reserves the
right for implementations to differ across framework versions. The mature policy, before v1:
**promise stability within a package version, disclaim it across versions**, one sentence in the
README and the new determinism ADR. (For comparison: FsCheck and AutoFixture both learned to
disclaim this explicitly.)

**(d) Lazy ambient pinning makes an *unwrapped* failure only approximately replayable.** Outside
`Reproducibly`, the first draw in a logical flow pins a remembered seed. Draws that happened
*before* the failing block in the same flow (a fixture, an earlier arrange) consume from the same
sequence, so replaying "just the test body" with the reported seed can diverge. The design is
right (this is why `Reproducibly` exists); the user guide's replay narrative could carry one
sentence saying replay fidelity starts at the scope boundary.

Verified non-issues worth recording so they are not re-litigated: the async-overload
`ExecutionContext` semantics (correct — see §3.3); `NewSeed() = Guid.NewGuid().GetHashCode()`
(collision-tolerant use, analyzed in ADR-0006); xUnit seed-spanning (each test invocation is its
own async frame; a shared class constructor participates in its test's flow, which is the correct
scope); `SequenceOf` re-enumeration (materialized once, never re-draws).

### 7.4 SOLID, briefly and only where it earns its keep

SRP: builders carry fluent surface, engines carry semantics — clean. OCP: adding a *constraint* to
a discrete type is a one-method facade addition over an existing engine operation; adding a *type*
is deliberately closed (sealed builders, internal engines) — the right trade for an invariant-heavy
library. LSP: the CRTP collection base is sound (no self-cast trick, `TSelf` bound enforced). ISP:
`IAny<T>` single-member; `ICardinalityHint<T>`'s two members travel together by explicit,
documented design (cardinality without membership would be unsound — the interface doc argues it).
DIP is intentionally absent at the user seam (no injectable randomness abstraction) — that *is* the
closed-extensibility decision of §7.2, acceptable but deserving its paragraph of documentation.

## 8. API Review

### 8.1 The constraint algebra is uniform where it counts

The verified matrix: all five signed integer builders and all four continuous/decimal builders
expose exactly `Positive · Negative · Zero · NonZero · GreaterThan[OrEqualTo] · LessThan[OrEqualTo]
· Between · OneOf · Except · DifferentFrom`; the five unsigned builders drop exactly
`Positive`/`Negative` (meaningless there — `NonZero` covers the intent); the four instant-like
builders (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) rename the bound family to domain
vocabulary (`After`/`AfterOrEqualTo`/`Before`/`BeforeOrEqualTo`/`Between`) with identical
inclusive/exclusive semantics, while `AnyTimeSpan` — a magnitude, not an instant — correctly keeps
the full numeric algebra including `Positive`/`Negative`/`Zero`; `AnyChar` carries the character families
plus the exclusion trio; `AnyGuid` has `NonEmpty`/`Empty`/`OneOf`/`Except`/`DifferentFrom`;
`AnyEnum` has the exclusion trio with declared-members validation; collections share
`NonEmpty · Empty · WithCount · WithMinCount · WithMaxCount · WithCountBetween · Containing ·
ContainingAny` (+ `Distinct` variants where meaningful). Bounds are consistently inclusive for
`Between`/`…OrEqualTo` and exclusive for `GreaterThan`/`LessThan`/`After`/`Before` — no semantic
surprises were found anywhere in the matrix. This level of consistency across nineteen hand-written
interval builders — plus their string, char, guid, enum, bool and collection siblings — is an
achievement in itself.

### 8.2 The asymmetries worth fixing or recording

* **`AnyString` is the only scalar builder with no exclusion constraints** — no `OneOf`, no
  `Except`, no `DifferentFrom`. "A name different from the one I already hold" is one of the most
  common dummy-string needs (it is exactly why `DifferentFrom` exists everywhere else, per its own
  XML doc). The honest reason for the gap: strings are not ordinal-mapped, so exclusions cannot
  ride the interval engine; `DifferentFrom` would need either a bounded redraw (expected collisions
  ≈ 0 for any non-trivial spec — consistent with the library's other bounded escapes) or a
  spec-aware layout tweak. Recommended (§10 Must-Have):

  ```csharp
  // Today — no way to express this:
  string other = Any.String().NonEmpty().Generate();     // might equal existing!
  // Proposed:
  string other = Any.String().NonEmpty().DifferentFrom(existing).Generate();
  ```

* **`AnyDictionary` drops `Containing`/`ContainingAny`** and duplicates the count facade (§7.1).
  `ContainingKey(TKey)` would ride the existing key-state machinery unchanged.
* **`Any.Bool()` is the single deviation from the CLR-name factory convention**
  (`Int32`, `SByte`, `Single`, … are all CLR names; the CLR name here is `Boolean`). The short form
  is arguably the better ergonomics — but then the convention is "CLR names, except one", and after
  1.0 the rename is breaking in either direction. Decide deliberately and record one line, before
  release (the repository has ADRs for precisely this class of naming decision).
* **`PairOf`/`TripleOf` stop at arity 3** while `Combine` runs to 8. Defensible (tuples beyond 3
  read poorly; `Combine` covers them), but the stopping point is recorded nowhere — one doc
  sentence closes it.

### 8.3 Discoverability and ceremony

The static `Any.` entry point makes the whole scalar surface one keystroke discoverable, and each
builder's fluent methods enumerate its full constraint vocabulary in IntelliSense — good. Two
seams are less discoverable: `As` and `OrNull` are extension methods in separate static classes
(invisible until the `using` exists — though the namespace is shared, so in practice they appear),
and `As` is the library's `Select` under a domain-intent name; one doc line bridging from LINQ
vocabulary ("`As` is `Select` for generators — named for its dominant use: passing through a value
object's factory") would help LINQ-native readers. The `Generate()` terminal ceremony is the
ADR-0020 trade, consciously priced there; the audit confirms the cost is real but small (one call
per materialization), the benefit (no effectful hidden conversions) is structural, and the decision
should stand. `AnyContext` mirrors scalars only — composition inherits the context through operand
sources, which is *more* elegant than mirroring and correctly documented.

### 8.4 Naming

`StartingWith`/`EndingWith`/`Containing`, `After`/`Before`, `DifferentFrom` vs `Except`,
`Containing` vs `ContainingAny` — the vocabulary is intention-revealing and reads at the call site
the way the philosophy intends. CLR type-name factories (`Any.Int32()`, not `Any.Int()`) are
consistent with the builder type names (`AnyInt32`) and sidestep C# keyword restrictions;
this is defensible and, more importantly, uniform (§8.2's `Bool` aside).

## 9. Maintainability Review

### 9.1 Duplication, measured

Four clone families among the numeric builders (signed quartet, unsigned quartet, continuous trio,
wide pair — byte-identical modulo type substitution; ~2,450 lines), the five temporal builders on
the same pattern (~800 lines), the constraint-and-conflict logic quadruplicated across the four
engines (~910 lines), and the `Any`/`AnyContext` scalar mirror (~300 doc-heavy lines). A scripted
comparison found **zero behavioral copy-paste slips** across the clone families — evidence of real
discipline — while all drift found so far is *documentation* drift (§4.3), which is exactly the
kind guards don't exist for yet.

### 9.2 Mitigation: guards, not generics

The obvious refactor — a CRTP generic base (`AnyOrdinal<T, TSelf>`) — fails this project's
constraints: C# requires a public base class for a public sealed builder (CS0060), so the internal
engine seam would leak into the public API; netstandard2.0 has no generic math (`INumber<T>` is
net7+), so the per-type `Ord`/`Val`/display lambdas remain; and the library's stated bar is
simplicity of maintenance, which 14 flat, boring, greppable files serve better than one clever
base. Source generators/T4 buy deduplication at the cost of build machinery and debuggability —
also a poor trade here. **Recommended instead: executable parity guards**, ~3 short
reflection-based tests:

1. *Mirror parity:* every public static `Any` method returning a builder type has an
   `AnyContext` instance counterpart with identical name/signature/return type, per TFM (~20
   lines; kills the §4.3 drift class outright).
2. *Algebra parity:* each builder family exposes its exact expected method-name set (the §8.1
   matrix, encoded once as data) — a new builder missing `DifferentFrom`, or a renamed method,
   fails with a named diff.
3. *Cross-engine scenario suite:* one parameterized test file runs the same scenario battery
   (full-range draw touches both halves; `Between` endpoints reachable; `DifferentFrom` on a
   narrow domain; `OneOf`+`Except` interplay; conflict messages) against **every** builder via
   small per-type adapters. This is the suite that would have caught both §4.1(a) and §4.1(b)
   before any human review.

Complementarily, install the release-engineering guards of §4.7 (public-API baseline + package
validation) — they catch the breaking-change class parity tests cannot.

### 9.3 Testing strategy

What exists is well-shaped: behavior-first naming that reads as living documentation; exception
*messages* tested as first-class contracts; the real-engine regex oracle; regression tests that
encode bug history (the `AnyGuid` race test that races a deadline instead of hanging the suite);
flake-safe property-style assertions (unseeded draws asserted only against their declared domain);
and a strictly black-box posture — no `InternalsVisibleTo` exists, so all 222 tests exercise the
public surface only. That last fact cuts both ways and should be held as a deliberate choice: it
proves the public API is sufficient to specify the library (and makes engine refactors
test-transparent), *and* it is consistent with how both reachability defects survived — no test
looks at an engine's value-space coverage directly. The additions that close the gap, in order of
leverage: the cross-engine scenario suite above; **reachability assertions** (for each builder, a
seeded loop over `Between(lo, hi)` must observe values in both halves and hit both endpoints —
cheap, deterministic under `WithSeed`); a generation-limit test for `AnyPattern` (currently
untested); dedicated tests for the documented-but-untested contracts (empty enum, `AnyException`
base catchability, `DictionaryOf` key-comparer flow); and the cross-TFM same-seed assertion in
`dummies-check` (extend `SeedBatch` with a golden sequence compared across the net8.0 and net6.0
consumer legs, and extend the smoke to cover `OrNull`/`SequenceOf`/`PairOf`/`StringMatching`/enum
draws, which the packaged-asset guard currently never touches).

### 9.4 Organization and hygiene

The flat 54-file root is acceptable today because naming discipline does the foldering (`Any*` =
builders, `*Spec` = engines, `Regex*` = pattern subsystem); grouping into folders is optional
polish, worth doing only alongside another structural change. Hygiene nits found: dead member
`RegexCharacters.Count`; the dead null-guard in `CollectionState.Exhausted` (§6/ADR-0013 row); the
stale comments and DisplayNames of §4.3; the stale `Directory.Build.props` header (§4.7).

## 10. Feature Gap Analysis

Method: every proposal was screened against (i) the library's philosophy (constraints express
invariants; no realistic-fake-data, no object graphs, no clock coupling), (ii) the composition
test — *can `As`/`Combine`/`StringMatching` already express this in one readable line?* — and
(iii) the full cost of a new builder (builder + `Any` + `AnyContext` + parity data + tests + docs
EN/FR + package README + possibly `dummies-check`). The bar for **Must Have** is the mandate's:
absence genuinely surprising. The library's composition-first design keeps this list short — most
BCL types are already one `As` away, which is the design working as intended.

### Must Have

**1. A top-level choice combinator: `Any.OneOf<T>(params T[])` and `Any.ElementOf<T>(IReadOnlyList<T>)`.**
Picking an arbitrary element from a caller-supplied set is among the most common dummy needs in
real suites ("any of the three configured currencies", "one of the states in this table"). Today
`OneOf` exists only *inside* typed builders — there is no way to draw from a set of domain objects
or strings at all. Every user hand-rolls the same three lines (and forgets the seeded source,
silently breaking `Reproducibly` for that draw — a trap the library exists to prevent):

```csharp
// Today — hand-rolled, and not seed-aware:
var currencies = new[] { eur, usd, gbp };
var currency   = currencies[new Random().Next(currencies.Length)];   // ambient seed ignored!

// Proposed — seed-aware, philosophy-consistent, eagerly validated (empty set throws):
Currency currency = Any.OneOf(eur, usd, gbp).Generate();
Order    order    = Any.ElementOf(existingOrders).Generate();
```

Constructive (single draw), trivially implemented over the ambient source with an
`ICardinalityHint` (distinct count of the pool — it composes with distinct collections for free),
mirrored on `AnyContext`. Who benefits: every consumer, weekly. Cost: one small builder. This is
the highest-leverage addition available.

**2. `AnyString.DifferentFrom(string)` / `Except(params string[])`.**
The §8.2 asymmetry: the most-used builder is the only scalar one that cannot exclude values. Honest
cost: a bounded redraw (the library's established escape pattern) or fragment-aware exclusion;
either fits in the existing `StringSpec` validation model. Who benefits: anyone testing
equality/inequality paths with string identifiers — a very common case. (`OneOf` on strings is then
free via proposal 1.)

### Nice to Have

* **`Uri` builder** (`Any.Uri().UsingHttps().WithHost("example.com")`) — the one BCL value-like
  type that is both commonly needed in tests and genuinely awkward to compose by hand (scheme/host/
  path/query validity rules). In-box on both TFMs. Moderate cost (its own mini constraint algebra);
  demand-driven timing is fine.
* **`WithChars(string pool)` / custom alphabet on `AnyString`** — today non-ASCII text (accents,
  i18n) is reachable only through `StringMatching` literals; a custom pool is a small, composable
  extension of the existing charset mechanism, and unlocks the i18n-sensitive-code use case without
  any Unicode-table machinery.
* **`MultipleOf(int)` on integers / `WithScale(int)` on decimal** — "a valid amount in cents", "a
  quantity in dozens": genuine invariants (not assertions) that today force `As(x => x * 100)`
  workarounds that distort the declared range. Constructive to implement (draw in the quotient
  space).
* **`ContainingKey(TKey)` on `AnyDictionary`** (§7.1/§8.2) — closes an API hole, a duplication, and
  a test hole at once.
* **[Flags] enum combinations, opt-in** (`Any.Enum<Permissions>().AllowingCombinations()`) — today
  undeclared combined values are unreachable *by design* (declared-members-only is the right
  default); an explicit opt-in respects the default while serving flag-heavy domains. Requires a
  documented stance on what "valid" means for flags (union of declared members).
* **`WithOffset`/offset control on `AnyDateTimeOffset`** — the offset dimension is currently
  degenerate (always zero, documented); tests exercising offset math cannot vary it. A bounded
  offset draw (±14 h in minutes, per the type's own rules) keeps validity.
* **Temporal granularity** (`WholeSeconds()`/`WholeDays()` or `WithGranularity(TimeSpan)`) — tick-
  precision instants are almost never round, which surprises tests that serialize timestamps;
  constructive via the ordinal engine (draw in the granule space, multiply). Also closes the
  documentation gap ("values are tick-precision") in the meantime.
* **`GenerateMany(int)` terminal** — sugar for "N values without `ListOf` ceremony"; a *named
  method* returning `IReadOnlyList<T>`, so it stays inside ADR-0020's letter and spirit.
* **A test-framework seed adapter** (`[ReproducibleFact]`) — anticipated by ADR-0006's follow-ups,
  dropped in the rebase, replaced by nothing. Zero-dependency Dummies cannot reference xUnit, so
  this is a *companion package* decision (`Dummies.Xunit`) — worth an explicit yes/no ADR rather
  than silence, because every consumer currently re-derives the `Reproducibly`-wrapping habit
  by hand.

### Optional Ideas

`Version` (composable today: `Combine(Any.Int32().Between(0,99), …, (ma,mi,pa) => new Version(ma,mi,pa))`;
low frequency); `IPAddress`/`IPEndPoint` (in-box, niche; a doc recipe first); `Encoding` and
`CultureInfo` (feasible **only** from a fixed embedded pool — the installed-culture set is a
cross-machine reproducibility hazard the library must not inherit; both are subsumed by proposal 1
+ a documented pool); `MailAddress`, file-system paths, `Stream`, `byte[]` blobs (all one-line
recipes over existing surface — `ArrayOf(Any.Byte())` already is the blob builder; document them
in the user guide's recipe section instead of shipping builders); `KeyValuePair` sugar;
`Queue`/`Stack`/`LinkedList` and `Sorted*` collections (one-line `As` conversions; a first-class
`Sorted()` needs a comparability gate analogous to the cardinality hint — design exists if demand
appears); `BigInteger` (in-box on both TFMs but breaks the "full range unless constrained"
symmetry — there is no full range; needs its own bounded-default stance); `Rune` (net8 leg;
conflicts with the deliberate ASCII-centric text model unless `WithChars` lands first);
`ContainingAll(params T[])` sugar.

### Out of Scope (recommended to stay absent, with reasons)

* **`Where(predicate)` filtering** — generate-and-filter is the exact opposite of the library's
  constructive model; unsatisfiable predicates reintroduce the unbounded-retry class the whole
  design exists to exclude. The existing answer (express the invariant as constraints, or build via
  `As` from a constrained draw) is the philosophy.
* **Generator registration / AutoFixture-style object graphs** — reflection-driven auto-filling is
  the adjacent product the README explicitly disclaims; plain C# helpers are the reuse mechanism.
* **Immutable collections** — `System.Collections.Immutable` is an external package on the
  netstandard2.0 leg, so a builder would break the zero-dependency identity there; consumer-side
  `.As(ImmutableList.CreateRange)` is one line. (A net8-leg-only surface would fracture the API
  across TFMs for marginal gain — not worth it.)
* **`Index`/`Range`** — validity is contextual (depends on the sequence length), so "arbitrary yet
  valid" cannot hold standalone.
* **`RegionInfo`**, **`Complex`** — environment-dependent resp. scientific-niche; both fail the
  frequency test.
* **Realistic fake data** (names, emails, addresses) — explicitly disclaimed; Bogus exists.

## 11. Recommended Improvements

In priority order; items 1–7 are the recommended pre-release gate.

1. **Fix the three reproduced defects** — decimal fraction construction
   (`DecimalIntervalSpec.cs:145`), type-aware nudge (`ContinuousIntervalSpec.cs:189` →
   `_nextUp`), char-overflow guard (`RegexParser.cs:398` + `RegexAlphabet.Range`); and the
   balancing-group/name validation in `SkipGroupName` (§4.1 d). Each with a regression test.
2. **Add reachability tests and the cross-engine scenario suite** (§9.3) — the structural answer to
   the defect class, not just the instances.
3. **Add the parity guards** (§9.2): `Any`↔`AnyContext` mirror test, algebra-matrix test.
4. **Close the determinism contract** (§7.3): document single-logical-flow seeding on
   `Reproducibly`; source-kind-aware replay hints (and multi-source `Combine` reporting); the
   cross-version stability policy sentence; the foreign-generator qualification in the exhaustion
   message (dead null-guard removed). Draft the **determinism ADR** and the **ordinal-engine ADR**
   (§5, structural gaps) as `Proposed` for `@reefact`.
5. **Run Dummies on its floors**: import `build/Net472TestFloor.props` into `Dummies.UnitTests`
   (net8-only tests conditioned out), add it to the ci.yml floor loop; add the cross-TFM golden-
   sequence assertion to `dummies-check`; state the .NET Framework floor in the package README
   (ADR-0022 follow-up).
6. **Documentation pass**: surface Dummies in the repository README (packages table + TOC); write
   the Dummies user guide with the per-builder constraint reference and the `StringMatching`
   dialect (closing ADR-0025's follow-up); correct the three "printable ASCII" sites (§4.2);
   advertise the empty-by-default behavior in the package README; fix the stale
   comments/DisplayNames (§4.3) and the `Directory.Build.props` header.
7. **Release-engineering guards**: public-API baseline (`PublicApiAnalyzers`) and
   `EnablePackageValidation`; decide `Bool()` vs `Boolean()` and record it; ask `@reefact` to
   resolve ADR-0025's status (after its wording fix); record the two ADR-0026 clarifications in the
   implementation reference; enrich or soften the ADR-0013/0015 implementation-reference pointers.
8. **Ship the two Must-Have features** (§10): `Any.OneOf<T>`/`Any.ElementOf<T>`, and string
   exclusions (`DifferentFrom`/`Except` on `AnyString`).
9. **`AnyDictionary`**: extract the shared count facade; add `ContainingKey`.
10. **Then, demand-driven**: the Nice-to-Have list (§10), each on evidence of need, with the
    parity-guard data updated as part of each addition's definition of done.

## 12. Suggested Roadmap

**Phase 0 — before the first `dum-v*` release (correctness and contract).** Items 1–7 above. The
rationale is ADR-0020's own: every one of these is cheap now and expensive after adoption — the
decimal fix changes every seeded sequence (a non-event today, a compatibility event after v1); the
determinism policy, the `Bool` naming, the API baseline, and the ADR statuses are all
one-line-or-one-file decisions that become migrations later. Exit criterion: the §4 weaknesses
table is empty except items explicitly deferred by recorded decision.

**Phase 1 — first stable cycle (completeness within the philosophy).** Item 8 (the two Must-Haves,
which are additive and low-risk), item 9, the user-guide recipe section (blobs, paths, Version,
Uri-via-Combine — turning Optional-list types into documentation instead of surface), and the
`Dummies.Xunit` companion-package decision (yes or no, as an ADR).

**Phase 2 — demand-driven growth.** Nice-to-Haves as real requests arrive (`Uri` and `WithChars`
first, on current evidence), each addition carrying its parity-matrix entry, tests, and EN/FR docs
as one unit. Revisit the Optional list yearly; resist the Out-of-Scope list permanently — it is
what keeps this library what it is.

## 13. Conclusion

Dummies is what a focused library looks like when the authors know exactly what it is for and —
just as importantly — what it is not for. The ordinal-space engine, the constraint-provenance
diagnostics, the bounded-escape discipline, and the ADR trail are all better than the norm for this
category, and the composition-first design keeps the future feature surface honest: most "missing
types" are correctly one `As` away, not one builder away.

The audit's findings concentrate in one place: the space between *declared* behavior and *reachable*
behavior. Two of the three reproduced defects live exactly there, invisible to a membership-only
test suite; the mirrored surfaces drift exactly where no guard looks; the determinism promise is
sound precisely up to the edges no document describes. All of it is fixable this side of the first
release, most of it in days, and the highest-value items are not the fixes but the guards — the
reachability suite, the parity tests, the API baseline — that make the next defect of each class
impossible to ship silently.

With Phase 0 done, this is a library that can credibly promise what its README says: arbitrary yet
valid, conflicts named at the line that caused them, and any run replayable from one reported seed
— on every target it ships for.

## 14. Issue tracking

The §11 recommendations were opened as GitHub issues on 2026-07-20, mirroring the repository's Dummies
issue template. This table is a **static snapshot**: the live state of each issue (open, closed, in
progress) lives in the issue tracker, not here — do not maintain status in this document.

| §11 item | Issue(s) | Phase (§12) |
|---|---|---|
| 1 — Fix the reproduced defects | [#206](https://github.com/Reefact/first-class-errors/issues/206) AnyDecimal upper half · [#207](https://github.com/Reefact/first-class-errors/issues/207) Single/Half nudge · [#208](https://github.com/Reefact/first-class-errors/issues/208) U+FFFF hang · [#209](https://github.com/Reefact/first-class-errors/issues/209) balancing groups · [#210](https://github.com/Reefact/first-class-errors/issues/210) minor regex edges | 0 |
| 2 — Reachability + cross-engine suite | [#213](https://github.com/Reefact/first-class-errors/issues/213) | 0 |
| 3 — Parity guards | [#214](https://github.com/Reefact/first-class-errors/issues/214) | 0 |
| 4 — Close the determinism contract | [#216](https://github.com/Reefact/first-class-errors/issues/216) contract docs + ADR · [#217](https://github.com/Reefact/first-class-errors/issues/217) ordinal-engine ADR · [#211](https://github.com/Reefact/first-class-errors/issues/211) seed report · [#212](https://github.com/Reefact/first-class-errors/issues/212) exhaustion message | 0 |
| 5 — Run on the floors | [#215](https://github.com/Reefact/first-class-errors/issues/215) | 0 |
| 6 — Documentation pass | [#218](https://github.com/Reefact/first-class-errors/issues/218) README + user guide · [#219](https://github.com/Reefact/first-class-errors/issues/219) printable-ASCII & stale docs | 0 |
| 7 — Release-engineering guards | [#221](https://github.com/Reefact/first-class-errors/issues/221) API baseline · [#222](https://github.com/Reefact/first-class-errors/issues/222) Bool naming · [#220](https://github.com/Reefact/first-class-errors/issues/220) ADR hygiene | 0 |
| 8 — Ship the Must-Have features | [#223](https://github.com/Reefact/first-class-errors/issues/223) Any.OneOf/ElementOf · [#224](https://github.com/Reefact/first-class-errors/issues/224) AnyString exclusions | 1 |
| 9 — AnyDictionary | [#225](https://github.com/Reefact/first-class-errors/issues/225) | 1 |
| 10 — Demand-driven Nice-to-Haves | [#226](https://github.com/Reefact/first-class-errors/issues/226) backlog | 2 |

---

*Produced by an agent-run audit (multi-agent review with adversarial verification; all reported
defects independently reproduced against the built library; full test suite executed). Advisory
per ADR-0004: recommendations and drafts only — every decision remains with the maintainer.*
