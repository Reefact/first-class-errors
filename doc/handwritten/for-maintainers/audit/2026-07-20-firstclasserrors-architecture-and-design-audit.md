# FirstClassErrors — Architecture, Design & Ecosystem Audit

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./2026-07-20-firstclasserrors-architecture-and-design-audit.fr.md)

**Date:** 2026-07-20
**Audited revision:** `3bf89e3fb568beb69329b12b2ec2be14553bb8d4` (`main` at audit time)
**Scope:** the whole FirstClassErrors ecosystem — core library, Analyzers, GenDoc (+ Worker), CLI, RequestBinder, Testing, Dummies (as an ecosystem member; its dedicated audit is the [Dummies architecture & design audit](./2026-07-20-dummies-architecture-and-design-audit.md)), samples, tests, documentation (EN + FR), ADR base, CI/CD and release engineering.
**Status:** advisory. Per the repository's own convention (ADR-0004), this audit produces recommendations, never blockers; every proposed ADR change is a draft for `@reefact` to accept or reject.
**Commissioned question:** *“Is this a coherent, professional, maintainable open-source project that could reasonably become a reference in its domain?”*

---

## About this audit

This report is the product of a full-repository review conducted as an architecture-board exercise, not a line-by-line code review. The method combined:

* **A real build and test run.** `dotnet build FirstClassErrors.sln` completed with **0 warnings, 0 errors** (confirming the repository's zero-warning claim), and `dotnet test FirstClassErrors.sln --no-build` passed **1,143 tests across 10 test projects with 0 failures and 0 skips** (405 core, 222 Dummies, 169 GenDoc, 152 RequestBinder, 85 Analyzers, 64 CLI, 21 core property tests, 14 binder-usage, 8 Testing, 3 binder property tests).
* **Thirteen parallel subsystem surveys** (core API, analyzers, GenDoc pipeline, CLI, RequestBinder, Testing/Dummies, user documentation, French/English synchronization, CI and maintainer tooling, test quality, usage examples, ecosystem structure, vision coherence), each producing evidence-cited findings.
* **A dedicated audit of every one of the 26 ADRs** — decision soundness, implementation compliance verified against the current code, freshness, and translation sync — plus a corpus-level review for duplicates, contradictions, and missing decisions.
* **Feature-gap analyses** against the competitive .NET error-handling landscape and against the integration surface a boundary-focused library is expected to offer.
* **An adversarial verification pass** in which every critical- and high-severity claim was independently re-examined against the code before being admitted into this report (result: 6 confirmed, 3 partially confirmed and restated in their verified form, 0 refuted), followed by **a completeness-critic pass** whose follow-up probes (release reality across the three trains, community health and bus factor, security posture, core-path performance, the AI-instruction file set) are incorporated throughout.

Every finding cites concrete evidence (`file:line`, type names, ADR ids, doc pages). Severity/value labels follow the classification requested by the board: **Critical**, **High Value**, **Medium Value**, **Low Priority**, **Out of Scope**.

## Table of contents

1. [Executive Summary](#1-executive-summary)
2. [Overall Assessment](#2-overall-assessment)
3. [Project Vision Assessment](#3-project-vision-assessment)
4. [Strengths](#4-strengths)
5. [Weaknesses](#5-weaknesses)
6. [ADR Review](#6-adr-review)
7. [ADR Compliance](#7-adr-compliance)
8. [Architecture Review](#8-architecture-review)
9. [Public API Review](#9-public-api-review)
10. [Ecosystem Review](#10-ecosystem-review)
11. [Documentation Review](#11-documentation-review)
12. [Developer Experience Review](#12-developer-experience-review)
13. [Feature Gap Analysis](#13-feature-gap-analysis)
14. [Recommended Improvements](#14-recommended-improvements)
15. [Suggested Roadmap](#15-suggested-roadmap)
16. [Conclusion](#16-conclusion)

---
## 1. Executive Summary

**Board question:** *Is this a coherent, professional, maintainable open-source project that could reasonably become a reference in its domain?*

**Answer: Yes — with unusual confidence on coherence, professionalism, and maintainability; conditionally on “reference,” where the conditions are specific, tractable, and mostly about shipping the last mile rather than fixing what exists.**

FirstClassErrors is the rare repository whose stated philosophy is mechanically enforced rather than aspirationally described. Its five design principles each terminate in a type-system or toolchain mechanism: an error cannot exist without its public message (staged builder); the documented taxonomy is carried by constructor types; the generated catalog is regenerated on every PR and release-gated as a versioned contract; 18 bundled Roslyn analyzers close the compile-time loop. The audit's independent build run confirmed the quality claims: **0 warnings, 0 errors, 1,143/1,143 tests passing**. All 26 ADRs were individually audited: **26 sound, 25 fully implementation-compliant (one partial), zero contradictions** — governance discipline well above industrial norms. Competitively, the project holds a four-part moat (versioned generated catalog, contract-diff gating, shipped analyzers, factory-reusing boundary binder) that no mainstream .NET error library occupies.

The weaknesses are as notable for what they are *not*: the audit found no architectural defect requiring rework. Instead they cluster in four groups. **(1) The missing HTTP last mile** — the catalog promises API clients RFC 9457 responses that no runtime code produces; every adopter hand-writes the one mapping where the public/internal separation can be violated. **(2) An inverted ADR corpus** — 17 of 26 ADRs govern peripheral packages while the foundational decisions (the Outcome model itself, netstandard2.0, class-not-struct, the analyzer scheme) are unrecorded, so the project's own per-PR ADR check cannot fire on its most important invariants. **(3) Release-reality gaps** — one preview shipped under a superseded pre-train process; the documented three-train pipeline has never completed a production run; the `Dummies`/CLI/binder packages are unpublished while the README's install section points at two of them (dead install instructions, verified against nuget.org); changelogs are empty despite the shipped preview; an unpublished `Dummies.dll` is meanwhile embedded inside `FirstClassErrors.Testing`. **(4) Hand-maintained edges of a machine-checked system** — two inaccurate nuget.org READMEs, six-guide gaps in both documentation hubs, two confirmed French-mirror drifts, three undocumented workflows, and a CONTRIBUTING that contradicts the release tooling on what commit scopes mean. A completeness pass added a fifth, community-facing group: bus factor of 1 with no continuity provisions, missing community-health files (code of conduct, issue templates), and one unhardened prompt-injection surface in the AI-review layer.

Six items are classified Critical: backfill the foundational ADRs; execute the Dummies publication sequence; fix the storefront READMEs; fix the four confirmed doc/contract drifts; ship the Error→ProblemDetails projection; add the discarded-`Outcome` analyzer. Three of the six are measured in hours, not weeks.

## 2. Overall Assessment

**Coherence: exceptional.** One vision sentence at every entry point, decomposed into falsifiable principles, implemented by enforcement mechanisms, extended consistently into satellites (the binder applies principles 1 and 3 to the boundary; the tooling dogfoods the model on itself per ADR-0009). The only scope tension — a generic test-data library living in the error repository — is explicitly governed by ADR-0011 with recorded extraction triggers, which is what governed drift looks like.

**Professionalism: exceptional, with two caveats.** Supply-chain posture (SHA-pinned actions, OIDC publishing, provenance, asserted SBOMs), a positive-proof CI philosophy, evidence-driven API decisions with lock-in tests, and reference-quality bilingual documentation. The caveats: the storefront/doc edges enumerated above (real, cheap, currently user-visible — including install instructions for packages not yet on NuGet), and the release engineering being rehearsed but unproven: the one shipped preview predates the current pipeline, which has never completed a production run. Git history hygiene, by contrast, is a verified positive: 226 commits of consistent Conventional-Commits discipline, hook- and CI-enforced.

**Maintainability: strong.** An acyclic, layered dependency graph with a zero-dependency core; boundaries enforced by tests and pack-time guards rather than convention; a single source of truth for release topology; total XML-doc coverage; a disciplined test estate (~1,024 tests, genuine property-based coverage, near-zero mocking). The honest risks are concentrated: a family of twin-duplication surfaces (renderers, resolvers, mirrors, converter clones) that a fix can silently miss; two missing test seams (GenDoc process orchestration, CLI config commands); and a bilingual, multi-surface documentation obligation that is heavy for a solo maintainer and currently guarded by discipline alone. All have cheap, pattern-consistent fixes recommended in §14.

**Reference potential: real, conditional on the last mile.** The differentiation is deep and defensible — this is the only .NET library where the error catalog is a versioned, release-gated, localized contract. What stands between the current state and reference status is not quality but *completion of the adoption path*: the ProblemDetails projection, the shipped log model, the boundary-capture helper, code fixes, first full-pipeline releases, and the community-facing surface (code of conduct, issue templates, a continuity statement for a bus-factor-1 project). Every one of these fits the existing architecture and packaging patterns; none requires revisiting a recorded decision.
## 3. Project Vision Assessment

### 3.1 The vision, as stated

FirstClassErrors states exactly one vision, and states it identically everywhere a reader can enter the project: *errors as first-class, documented, diagnosable concepts* — “Turn your errors into structured, living knowledge about your system” (`README.md`). The same sentence, in near-identical wording, appears in the NuGet package description (`FirstClassErrors/FirstClassErrors.csproj`), the package README (`FirstClassErrors/README.nuget.md`), `CLAUDE.md`, `AGENTS.md`, `CONTRIBUTING.md`, and even inside the context section of ADR-0009. This matters more than it may seem: multi-entry-point OSS projects usually accumulate two or three slightly divergent self-descriptions over time, and divergent self-description is the first symptom of scope drift. Here there is one sentence, and it has held.

The vision is then decomposed into five explicit design principles (`doc/handwritten/for-users/DesignPrinciples.en.md`), each closed by a **“Consequence”** clause that names a concrete, checkable API behavior:

1. *An error is a recognized situation* → one factory per precise situation, identified by a stable `ErrorCode`.
2. *The error is the model; the exception is a transport* → the same `Error` travels as `throw error.ToException()` or as `Outcome<T>.Failure(error)`.
3. *Public and internal information must remain separate* → the three-message model, enforced by a staged builder.
4. *Documentation belongs beside behavior* → `[DocumentedBy]` links each factory to structured documentation in the same class; the catalog is generated from code.
5. *Diagnostics are hypotheses, not verdicts* → diagnostic entries describe observable states with an `ErrorOrigin`, not accusations.

Because every principle ends in a falsifiable consequence, the philosophy is *testable* rather than aspirational — and this audit tested it.

### 3.2 Does the implementation deliver the vision? Largely, yes — mechanically

The striking property of this repository is that the principles are not enforced by convention or review vigilance; they are enforced by the type system and the toolchain:

* **Principle 3 is structurally unskippable.** Every concrete error category exposes a static `Create(...)` that captures the *internal* information (code + diagnostic message) and returns a `PublicMessageStage<TError>` — a type that deliberately is **not** an `Error` and cannot be used where one is expected. The only way to obtain the finished error is `WithPublicMessage(shortMessage, detailedMessage?)`. An error therefore cannot exist without both audiences having been addressed (`PublicMessageStage.cs`, `DomainError.cs:37-66`). There is no `Build()` step to forget.
* **The taxonomy in the docs is the taxonomy in the types.** `ErrorTaxonomy.en.md`'s composition table (a `DomainError` nests only `DomainError`s; a `PrimaryPortError` nests `DomainError`/`PrimaryPortError`; base `InfrastructureError` accepts any `Error`) is carried by constructor parameter types and the typed `PrimaryPortInnerErrors`/`SecondaryPortInnerErrors` collections — a mislabeled or ill-composed error is a compile error, not a review comment (`DomainError.cs:20-24`, `PrimaryPortInnerErrors.cs:39-68`). Port errors fix their `InteractionDirection` in their constructors and *compute* aggregate `Transience` from their inner errors through a single authoritative `TransienceCalculator`.
* **Principle 4 — “living documentation” — is delivered end-to-end and dogfooded.** GenDoc documents its *own* failures using the library's DSL (ADR-0009 candidly records that, before this, the repository “had no real internal consumer of the model”). The generated catalog under `doc/generated/gendoc/` is wiped and regenerated by CI on every pull request (`gendoc-docs.yml`) and committed through normal review, and the catalog is a *versioned contract*: `release.yml` computes `fce catalog diff` against `errors-baseline.json` and refuses to publish a release containing a breaking catalog change without a major version bump (ADR-0010). Few projects make “docs never drift” an enforced release gate rather than a slogan.
* **The compile-time loop is closed by 18 Roslyn analyzers (FCE001–FCE018)** that enforce precisely the vision's conventions — duplicate/unstable error codes, documentation wiring (`[DocumentedBy]`/`[ProvidesErrorsFor]`), documentation content quality, sensitive or oversized context data — and they ship *inside* the main package (`analyzers/dotnet/cs`), so the README's “checked at build time” claim is true with zero extra install.
* **A sixth, unstated principle is implemented with unusual care: “manufacturing an error never throws.”** Because errors are built on failure paths (inside a `catch`, while logging), construction never raises a secondary exception that would mask the original problem: a `null` code degrades to `#UNSPECIFIED`; missing mandatory messages become visible sentinels (`#MISSING_DIAGNOSTIC_MESSAGE`, `#MISSING_SHORT_MESSAGE`) *and* the omission is recorded as queryable context; a throwing `configureContext` delegate is captured as data under `#CANNOT_BUILD_ERROR_CONTEXT` while preserving the entries it added before failing (`Error.cs:36-142`). This is operations-grade thinking that mainstream result libraries simply do not have.

The project is also honest about its own boundaries — a trait worth naming because it is rare. `WhenNotToUseFirstClassErrors.en.md` names concrete anti-fits (prototypes, small utilities, low-level libraries, hot loops, unowned systems) and ends with “The goal of this library is not to make every exception richer.” `ComparisonWithOtherLibraries.en.md` is dated, refuses to rank, concedes scenarios that favor ErrorOr or FluentResults, and admits its motivating scenario “naturally plays to its strengths.” The FAQ asks itself “Is this too heavy for a simple application?”. For a project with reference ambitions, this earned-trust posture is exactly right.

### 3.3 Where the say/do gap actually is

The gaps found are few, small, and — tellingly — mostly *meta*: the project's rare drift is in hand-written text about the system, never in the system itself.

1. **The storefront contradicts the anti-drift promise (Medium).** `README.nuget.md` advertises “16 Roslyn analyzers in the box (FCE001–FCE016)” while the codebase ships 18 (FCE001–FCE018, including FCE017 *SensitiveDataInErrorContext* and FCE018 *OversizedErrorContextValue*, both documented under `doc/handwritten/for-users/analyzers/`). The same file promises the catalog “never drifts from the deployed system.” The irony is instructive: the one artifact outside the generated pipeline is the one that drifted. §14 recommends both the one-line fix and a `tools/`-style count guard so this class of drift joins the machine-checked set.
2. **The never-throws doctrine is invisible in handwritten documentation (Medium).** The doctrine and its sentinels have observable production symptoms — an operator can meet `#MISSING_SHORT_MESSAGE` in an API response or `#CANNOT_BUILD_ERROR_CONTEXT` in a log — yet no page under `doc/handwritten/for-users/` mentions them (they live only in XML docs). A user who searches the docs for the sentinel string they just saw finds nothing. This is effectively a sixth design principle that the Design Principles page omits.
3. **The changelog scope contradicts the release train (Low).** `CHANGELOG.md` declares it covers “FirstClassErrors and FirstClassErrors.Testing,” but the `lib` train packs three packages in lockstep — the two named plus `FirstClassErrors.RequestBinder` (`tools/packaging/pack.sh`, `tools/trains.sh`) — so binder-facing changes currently have no declared changelog home, while `RequestBinder.en.md` says the binder ships “on the same release train … at the same version.”
4. **A deliberate, governed scope drift: Dummies.** A generic test-data library with an audience explicitly beyond this project lives in the error-library repository. Crucially, the drift is *ADR-bounded* (ADR-0011 records the decision, its costs, and extraction triggers), machine-enforced (an architecture test plus a pack-time nuspec guard prove Dummies references no FirstClassErrors assembly), and honest. It still dilutes the single-product identity of a pre-1.0 repository; §10 and §15 return to it.
5. **Ambition currently outruns demonstrated maturity (informational, not a defect).** The repository presents release provenance, SBOMs, and a comparison against established libraries in the present tense, yet its release reality is one preview: `0.1.0-preview.1` of the core and Testing on nuget.org under a single remote tag (`lib-v0.1.0-preview.1`) that predates the current three-train pipeline, with all three changelogs still showing only an empty `[Unreleased]` section and the README's install section pointing at two packages (`FirstClassErrors.RequestBinder`, the `fce` tool) that are not yet on NuGet. The release machinery exists and is dry-run tested — but for the board's question, the vision-to-adoption gap is the honest current answer: coherence is proven; *reference status* is a bid, not yet a fact.

### 3.4 Verdict

This is one of the most internally coherent vision-to-implementation mappings a small library can show. The philosophy is not marketing copy; it is a set of enforced mechanics — staged builders, typed composition rules, analyzer rules, a versioned catalog gate — each traceable to a stated principle. The residual say/do gaps are documentation-surface defects measured in sentences, not architecture. The one strategic tension (a second product, Dummies, inside the flagship's repository) is explicitly governed by the project's own decision process rather than drifting silently.
## 4. Strengths

The detailed evidence lives in the per-domain sections; this list names the strengths that define the project, roughly in order of how much they contribute to the board's question.

1. **Vision-to-implementation coherence that is mechanical, not rhetorical (§3).** One vision sentence, five principles each with a falsifiable consequence, and a type system + toolchain that enforce them: the staged builder makes the public/internal split unskippable; typed inner-error collections make the taxonomy's composition rules compile-time facts; the catalog is regenerated by CI and gated at release; 18 analyzers close the compile-time loop. Very few projects of any size can trace every stated principle to an enforcement mechanism.

2. **An unoccupied four-part moat (§13.1).** Versioned/localized generated error catalog, error-contract breaking-change detection, shipped analyzers, and a factory-reusing boundary binder — none of ErrorOr, FluentResults, OneOf, LanguageExt, CSharpFunctionalExtensions, Ardalis.Result, or DotNext offers any of the four. The differentiation is deep and deliberate, not feature-list padding.

3. **The “manufacturing an error never throws” doctrine (§9.2).** Original, coherent, implemented end-to-end with visible sentinels and context-recorded degradation — operations-grade design for the failure path of the failure path, which mainstream result libraries simply do not address.

4. **ADR governance of genuinely unusual quality (§6).** 26 ADRs, all judged sound; zero duplicates or contradictions, with near-miss relationships explicitly disambiguated in text; a bounded, traceable editorial-migration exception (ADR-0024) executed by the book; evidence-driven decisions (ADR-0023's benchmark-backed deferral is a model of pre-v1 reasoning); and 25/26 verified implementation compliance. The process discipline — one-sentence decisions, facts-only context, decision-vs-specification separation with a falsifiable test — exceeds most industrial ADR practice.

5. **Machine-enforced boundaries everywhere (§8.1, §10.2).** Architecture tests + pack-time nuspec guards for package boundaries; overload-resolution lock-in tests for API decisions; packed-artifact dogfood consumers proving the analyzer loads on the floor SDK and the right TFM asset ships; a positive-proof CI philosophy (grep the evidence, don't trust exit codes). This closes the “green build, broken artifact” gap almost every OSS project leaves open — the repository's single most transferable practice.

6. **A verified zero-defect quality baseline.** The audit's own build run: 0 warnings, 0 errors; 1,143/1,143 tests passing across 10 projects. Test quality matches quantity (§8.2): ~1,024 test methods in a disciplined house style, genuine property-based tests encoding the monad laws and binder invariants, snapshot testing paired with a structural HTML gate, near-zero mocking, and tests that dogfood the project's own Testing/Dummies packages. Git history hygiene matches too: 226 commits of consistent, hook- and CI-enforced Conventional Commits.

7. **Documentation practicing the thesis it sells (§11).** ~40 bilingual guides with exactly one stale API sentence found across the entire English set; a 185-file French mirror with two drifts in ~9,500 mirrored lines; generated catalog pages that are regenerated and diffed on every PR. Honest scoping documents (WhenNotToUse, a non-ranking dated comparison, a FAQ that opens with “You can [just use exceptions]”) build exactly the credibility a reference project needs.

8. **Supply-chain and release engineering above the project's weight class (§11.5, §10.2).** 41/41 SHA-pinned actions, least-privilege permissions, OIDC trusted publishing, SLSA provenance, embedded and asserted SBOMs, dependency-confusion defenses, a two-tier release dry-run strategy sharing the real pack script, and injection-conscious LLM automation that is human-gated and degrades gracefully.

9. **Compile-time safety engineering in the binder (§8.2).** The token + ref-struct-scope design making unvalidated values *unreachable at compile time* is a genuinely novel application of C# semantics to API safety, and the surrounding performance work is measured, honestly reported, and ADR-recorded.

10. **Honest self-limitation.** The project documents when not to use it, concedes competitor strengths by name, dates its comparison reviews, and prefers deferring features (delegate selectors, gRPC, publishers) with recorded reasoning over accreting them. Restraint is a strength when the goal is reference status.

11. **A governed model for AI-assisted development.** ~60% of commits are agent-co-authored, under an explicit human-decides rule (agents draft ADRs as Proposed and never merge; the maintainer holds sole status/merge authority), injection-conscious LLM automation that is advisory and degrades gracefully, and a five-file agent-instruction layer verified to be in near-perfect cross-file sync. Most projects have no governance model for this at all; this one is articulate enough to be worth publishing about (its gaps — a missing user-facing disclosure and one unhardened prompt — are listed in §5.6).
## 5. Weaknesses

Every item below survived an adversarial verification pass or carries direct file-level evidence; none is speculative. They are grouped by theme and ranked by how much they matter to the board's question. The striking pattern: **almost nothing here is a design defect** — the weaknesses are missing last-mile surfaces, sequencing risks, and hand-maintained edges of an otherwise machine-checked system.

### 5.1 Strategic / product

1. **The HTTP last mile is missing while the product promises it (§13.2).** The catalog shows API clients concrete RFC 9457 responses with `urn:problem:{service}:{code}` types; no runtime code produces them. This is a philosophical inconsistency, not just a convenience gap: the documented wire examples and actual responses can drift freely — the exact failure mode the project exists to eliminate.
2. **The foundational decisions are unrecorded (§6.5).** 17 of 26 ADRs govern the peripheral packages; the core model, the netstandard2.0 target, the class-not-struct rule, the analyzer/ID scheme, GenDoc's reflection approach, and the bilingual policy have no decision record — so ADR-0004's per-PR check *cannot fire* on the project's most important invariants.
3. **Release reality diverges from the documented release story.** One preview has shipped (core + Testing at `0.1.0-preview.1`, remote tag `lib-v0.1.0-preview.1`) — but it predates the current three-train pipeline (the only GitHub Release is a *draft* under the old `v*` scheme, with no published, consumer-verifiable attestation), the documented release workflow has never completed a production run, and all three changelogs still contain only an empty `[Unreleased]` section despite the shipped preview (the “Keep a Changelog” claim has zero recorded history, and nothing in `release.yml` promotes `[Unreleased]` at cut time). Most user-visible: **the README's install section instructs `dotnet add package FirstClassErrors.RequestBinder` and `dotnet tool install --global FirstClassErrors.Cli` — neither package exists on NuGet** (verified: BlobNotFound). Reference status is a bid, not yet a fact, and several risks below (unclaimed IDs, embedded Dummies) are open *because* the remaining trains haven't shipped.

### 5.2 Sequencing risks in the ecosystem (§10.3)

4. **`FirstClassErrors.Testing` embeds an unpublished `Dummies.dll` in its `lib/`** — version invisible to NuGet, future same-identity collision when Dummies publishes, and net8+ consumers silently receiving the downlevel netstandard2.0 Dummies surface. Documented as temporary; risky while the window is open.
5. **The generic `Dummies` package ID is unclaimed on nuget.org** while its release train, guards, and changelog already exist — an open squatting window on an identity two ADRs treat as durable.
6. **A binder-only install silently loses the analyzers** (nuspec dependencies exclude `Analyzers` by default), and the docs don't say so — the consumer documenting a boundary error surface is the one without the rules enforcing it.
7. **Two lasting decisions live only in comments:** the lib-train lockstep contract, and the core→Testing `InternalsVisibleTo` that makes lockstep *permanently mandatory*. Both pass the project's own ADR significance test.

### 5.3 Enforcement gaps in the project's own thesis

8. **A discarded `Outcome` produces no diagnostic (§12.3)** — the library's central failure mode (a lost error) is unenforced while the rarer discarded-`ToException()` case has a rule.
9. **All 18 diagnostics are report-only** — no code-fix providers, so the documentation boilerplate that is the library's main adoption tax gets no scaffolding help.
10. **Resource-localized documentation text escapes all content-quality rules** (literal-based analyzers can't see it; no `fce lint` closes the hole), and nothing documents the limitation.
11. **The analyzer test harness never asserts snippets compile** — negative tests can pass vacuously if the core API drifts; assertions rarely pin locations or message arguments.
12. **`DocumentationContractVersionAttribute` documents a generator-side version check that does not exist** — a maintainer would rely on a guard that isn't there.

### 5.4 Storefronts and documentation edges (§11, §12.1)

13. **Both nuget.org READMEs are wrong in user-visible ways:** the core claims 16 analyzers (ships 18); the CLI documents a nonexistent command tree and omits the catalog commands entirely.
14. **Hub-page drift:** the README and DocumentationMap each omit six guides; the compiled, snapshot-tested sample projects are linked from almost nowhere; `DeterministicTesting` (EN+FR) documents a removed seeding API — the one place users are told to write code that doesn't compile.
15. **The French mirror has two confirmed drifts** (missing `binder`/`dummies` commit-scope rows; 20 dead footer anchors), and the stated bilingual policy covers 1 file while practice covers ~120, with no sync tooling.
16. **Maintainer docs lag the pipeline:** 3 of 15 workflows undocumented; `CONTRIBUTING.md` still says the commit scope “carries no versioning weight” while the release tooling *silently drops unscoped commits from the release record* — the audit's most consequential contributor-facing contradiction.

### 5.5 Architecture-level debts (§8.3)

17. **Twin-duplication risks:** RFC 9457 example rendering duplicated across two renderers; CLI source-resolution logic duplicated in two divergent styles with byte-identical strings; the `Any`/`AnyContext` 26-entry mirror; triplicated `Error` constructors; near-clone converter/inner-error pairs.
18. **Two missing test seams:** GenDoc's process orchestration (timeout/kill/corrupt-output branches — the source of half the `GENDOC_` catalog — validated only by happy-path CI) and the CLI's console-coupled, zero-test config/renderer commands. Nothing anywhere drives the CLI's real argv surface, which is its declared compatibility contract.
19. **A per-request lock and shared write on the binder's hottest path** (`RequestBinderOptions.Default` getter) — inconsistent with the sub-microsecond optimization work documented around it; a double-checked fast path preserves the ADR-0017 semantics exactly.
20. **API friction items (§9.3):** the half-open taxonomy extensibility contract; missing symmetry members (non-generic `Then` value-map; single-inner-error port `Create`); the process-global context-key registry's harsh collision mode with three competing naming conventions in canonical sources; missing `[NotNullWhen]` on `TryGet`.

### 5.6 Community, lifecycle, and process surface (completeness-pass findings)

These emerged from a dedicated completeness pass over repository aspects no subsystem owns; each was verified against the tree or live registries.

* **Bus factor of 1 with no continuity provisions.** All 226 commits are by the sole maintainer or by AI agents under their direction (git shortlog: 90 human / 136 agent-co-authored); there is no CODEOWNERS, GOVERNANCE, or succession/access-continuity statement. For a “reference” bid this is a standard board criterion — and the repository's excellent process documentation makes it *more* survivable than most solo projects, but nothing says so.
* **GitHub community profile is incomplete:** no CODE_OF_CONDUCT, no issue templates, no SUPPORT/FUNDING; CONTRIBUTING never references a code of conduct; and the public issues page appears restricted while the README invites users to open issues — a mismatch worth resolving one way or the other. The AI-assisted development model, elaborately governed *internally* (AGENTS.md, Co-Authored-By trailers, claude/* branches), has no user-facing disclosure in README or CONTRIBUTING.
* **The AI-review layer has one unhardened surface.** The five-file agent-instruction set (CLAUDE.md, AGENTS.md, `code_review.md`, two `.github/` prompts) is in remarkably good cross-file sync, but `code_review.md` — a well-crafted 414-line Conventional-Comments review spec — lacks the treat-analyzed-content-as-data prompt-injection hardening its two sibling prompts carry, nothing in the repository documents what actually consumes/binds it, its lowercase root-level name breaks the repo's own conventions, and the deliberately triplicated rule text (CLAUDE/AGENTS/code_review) has no sync check despite `tools/` housing five other consistency checkers.
* **The core `Outcome`/`Error` path is unmeasured.** The binder has a model benchmark harness feeding ADR-0023; nothing equivalent targets the core, although the class-not-struct policy rests on the empirical claim that “error/result paths are not hot loops,” the comparison page has no performance/allocation row against struct-based ErrorOr, and `WhenNotToUse`'s hot-loop warning names the wrong cost (exception creation, which the Outcome path doesn't incur — the real per-call costs are the `Outcome`/`Error` allocations).
* **Packaging nit:** the 919 KB `icon.png` is packed into every NuGet package — an easy size win on first download impressions.

### 5.7 Practice lagging design

21. **The flagship determinism property has no real-world mileage:** `Reproducibly` is used nowhere in the repository's own large test suites, and the anticipated xUnit adapter was never built — a coincidental-value failure in the main suite is unreplayable today.
22. **Error consumption is under-demonstrated:** no sample uses `Recover`, the async pipeline, non-generic `Outcome`, or `InfrastructureError`; sample tests hand-roll assertions instead of dogfooding `FirstClassErrors.Testing`; and the `UsagePatterns` guide contradicts the shipped sample `Amount` on the project's own flagship derivation pattern.
## 6. ADR Review

The board asked that ADRs be treated as one of the most important aspects of this review. They were: every one of the 26 ADRs was audited individually — decision summarized, soundness challenged, implementation compliance verified against the current tree, freshness judged, French twin spot-checked — and the corpus was reviewed as a whole for duplicates, contradictions, obsolescence, and missing decisions.

### 6.1 The ADR process itself

The corpus lives under `doc/handwritten/for-maintainers/adr/`: 26 English ADRs, 26 French twins, an index README, and a heavily annotated template. Statuses: 23 Accepted, 2 Superseded (0006→0026, 0016→0018), 1 Proposed (0025). All 26 were authored between **2026-07-10 and 2026-07-19** by a single decision maker (“Reefact”) — a very young, dense corpus, produced largely in response to design-review issues on the RequestBinder and Dummies/Testing packages.

The process definition is unusually rigorous, and — rarer — the corpus actually follows it:

* **Decision-vs-specification separation with a falsifiable test.** The README rules that an ADR records a decision, not a specification, with the litmus “if the implementation changed but the decision stood, the ADR should not need editing,” and the template embeds per-section guardrails. Post-migration ADRs visibly comply, link-delegating mechanics to `specifications/adr-implementation-reference.md` and the workflow reference.
* **Zero duplicated and zero contradictory decisions.** Every near-miss pair states its own relationship in the text: ADR-0017 “deliberately revisits one alternative rejected by ADR-0012 … does not change ADR-0012's decision”; ADR-0018 supersedes 0016 with links in both directions; ADR-0022 “refines” 0002 with reciprocal disclaimers; ADR-0021 annotates 0007/0012/0017 as “decision remains valid; illustrative API shape updated.” Most ADR corpora accumulate silent overlaps; this one names the relationship type each time.
* **A governance exception done right.** ADR-0024 authorized a *one-time, bounded* editorial migration that moved implementation mechanics out of 13 accepted ADRs into a bilingual implementation reference — and it was executed traceably: each affected ADR carries the authorization footer, all seven referenced anchors resolve, and git history even records a numbering collision between two parallel branches (both claimed ADR-0023) resolved by a documented renumbering commit (`7d74b82`).
* **Index/status hygiene is exact**, including honest semantics: the lone Proposed ADR (0025) is shown as Proposed rather than quietly promoted, and superseded ADRs link their successors from the status line.

### 6.2 Per-ADR verdicts

Every ADR was judged on soundness (is the decision right, well-argued, precise?), current implementation compliance, and freshness. The full-corpus result is remarkable and worth stating plainly: **26/26 sound, 25/26 fully compliant (one partial), 24/26 current** — the two “obsolete” entries being precisely the two Superseded ADRs, which is the *healthy* kind of obsolescence (replaced by a successor, never edited in place).

| ADR | Title (abridged) | Soundness | Compliance | Freshness |
|---|---|---|---|---|
| 0001 | Lock the analyzer's Roslyn floor (4.8.0) | Sound | Compliant | Current |
| 0002 | Floor the tooling runtime at oldest LTS (net8.0) | Sound | Compliant | Current *(supersession due ~Nov 2026)* |
| 0003 | Unify Outcome value mapping under `Then` | Sound | Compliant | Current |
| 0004 | Check every PR against the ADR base | Sound | Compliant | Current |
| 0005 | Plain factory name = Outcome-returning; `OrThrow` = throwing | Sound | Compliant | Current |
| 0006 | Seedable arbitrary-value source *(superseded by 0026)* | Sound (for its moment) | Compliant (supersession by-the-book) | Obsolete |
| 0007 | Binder terminals named `New` and `Create` | Sound | Compliant | Current |
| 0008 | Nullable value types via struct-constrained overloads | Sound | Compliant | Current |
| 0009 | Tooling failures as first-class errors | Sound | Compliant | Current |
| 0010 | GenDoc catalog as a versioned contract | Sound | Compliant | Current |
| 0011 | Dummies as a standalone package in-repo | Sound | Compliant | Current |
| 0012 | Fix binder options before binding begins | Sound | Compliant | Current |
| 0013 | Distinct collections: cardinality gate else bounded draw | Sound | Compliant | Current |
| 0014 | Required list = presence, not cardinality | Sound | Compliant | Current |
| 0015 | Cap `Any.Combine` at arity eight | Sound | Compliant | Current |
| 0016 | Configurable structural error codes *(superseded by 0018)* | Sound | Compliant | Obsolete |
| 0017 | App-wide default binder options, freeze-on-first-use | Sound | Compliant | Current |
| 0018 | Bundle structural error code + messages | Sound | Compliant | Current |
| 0019 | Document overridden binder errors in the consumer's catalog | Sound | Compliant | Current |
| 0020 | Materialize dummies only through `Generate()` | Sound | Compliant | Current |
| 0021 | Out-of-DTO arguments as peers, source-agnostic entry | Sound | Compliant | Current |
| 0022 | Library .NET Framework floor at 4.7.2 | Sound | Compliant | Current |
| 0023 | Keep expression-tree selectors for v1 binder | Sound | Compliant | Current |
| 0024 | One-time editorial refactoring of accepted ADRs | Sound | Compliant | Current (spent) |
| 0025 | Regex-subset string generation *(Proposed)* | Sound | Compliant | Current — **status lags code** |
| 0026 | Rebase Testing's arbitrary values on Dummies | Sound | **Partial** | Current |

### 6.3 Highlights: the strongest decisions

A few ADRs deserve naming as models, because they show what the process produces at its best:

* **ADR-0003 (unify map/bind under `Then`)** is the strongest-written core-API record: the C# overload-betterness mechanics that guarantee flattening are stated precisely in Context, the pre-release fact that makes the break free is recorded, the Negative section honestly concedes what is lost (a chain no longer telegraphs which steps can fail), and the speculative risk (a future language version altering resolution) is hedged by *lock-in tests* rather than hope.
* **ADR-0005 (`OrThrow` naming)** contains a genuinely insightful reframing: the BCL convention worth keeping is not the word `Try` but the rule “the variant departing from the prevailing default carries the marker” — applied to a library whose default is the `Outcome`. The audit's one challenge stands, though: the ADR is silent on *lone* throwing factories with no Outcome sibling (e.g. `ErrorCode.Create` throws), leaving the convention's scope implicit.
* **ADR-0023 (keep expression-tree selectors)** is evidence-driven end to end: the irreducible per-call-site expression cost is quantified by a checked-in BenchmarkDotNet harness (~488 B / ~416 ns per property; cached-delegate ceiling 1 ns / 0 B), the mitigation (hoisting selectors to static fields removes ~85–90% of time) is measured, and the delegate fast path is correctly deferred as an *additive* post-v1 decision instead of doubling the surface now. This is exactly the right pre-v1-freeze reasoning.
* **ADR-0020 (no implicit conversions in Dummies)** rests on accurate C# semantics (user-defined implicit conversions don't participate under `var`/`object`/generic inference — the operator was a partial abstraction) and the classic framework-design criterion (implicit conversions must be cheap, total, referentially transparent — 28 effectful, throwing, randomness-drawing operators were none of these).
* **ADR-0010/0009 (catalog as versioned contract; dogfooding)** turn the project's thesis on itself: GenDoc's failures use the library's own model, and a breaking catalog change cannot ship without a major bump, enforced mechanically at release time — deliberately *not* per-PR, because breaking is legitimate during development. The reasoning about *where* to place the gate is a small masterclass.
* **ADR-0012→0017→0018 (binder options)** shows disciplined decision evolution: make the invalid state unrepresentable (0012), then revisit a rejected alternative under strictly stronger constraints and say so (0017), then supersede the code-only override with the bundled definition because “code and message are one concept” and freeze-on-first-use *forces* a builder-evaluated-at-emission for per-request localization (0018).

### 6.4 Individual weaknesses worth acting on

The per-ADR audits surfaced no unsound decision, but a consistent set of text-level defects. In priority order:

1. **ADR-0026's risk premise is imprecise and one follow-up was missed (the corpus's only compliance gap).** The recorded top risk says the embedded-assembly hazard arises “precisely because Dummies types appear in Testing's public API” — but no public member of `FirstClassErrors.Testing` exposes a Dummies type today; the real hazard is that consumers compile against the embedded `Dummies.dll` in `lib/` (the README instructs `Dummies.Any.*`). Same hazard, different mechanism — worth recording correctly in the implementation reference. More concretely: the doc-lockstep follow-up was missed on `DeterministicTesting.en.md`/`.fr.md` (line ~163), which still describes the *superseded* seeding API (`UseAny()` takes no seed; reproducibility now comes from `Dummies.Any.Reproducibly(...)`) — a user-facing contradiction with the shipped API that the project's own CLAUDE.md sync rule makes mandatory to fix. Also, the Decision promises factory methods exposing `IAny<T>` generators “where composition is needed,” and none exists yet — dormant by design, but the Decision reads as delivered shape.
2. **ADR-0017's Decision sentence under-specifies what was decided.** It says options freeze “after first binding use,” but the implementation freezes on *any first read* of the getter, and “configurable once” is actually “no reassignment after first read.” The implementation reference is more precise than the decision it implements — inverting the intended authority order. A sharpened decision sentence resolves it.
3. **ADR-0022 names referenced documents that don't carry the promised content.** The ADR says the Windows job, polyfills, exclusions, and preview coverage are “documented in the ADR implementation reference and the CI workflow reference” — neither has them (the real documentation lives in excellent in-code comments). Make the referenced documents true rather than touching the accepted ADR.
4. **ADR-0024's boundary is not enumerated where it claims to be.** The risk mitigation says it “authorizes only the migration identified in its references,” but the references name two *living* documents, not the 13 affected ADRs or the executing commit range. One “migration record” note in the implementation reference closes it without editing the accepted text.
5. **ADR-0025 is Proposed while its implementation is merged and already consumed by an Accepted decision** (ADR-0026's `ErrorCodeFactory` calls `Any.StringMatching`). Per the project's own protocol only the maintainer may flip status — this is precisely the state ADR-0004's model exists to prevent from persisting. Its test claim also overstates: the guard is a corpus-driven oracle theory (46 patterns against the real .NET engine), not a property test.
6. **The single `Date` field erases history on supersession.** ADR-0006 and 0016 now show only their supersession date; the original acceptance date is unrecoverable from the record — a real cost for a corpus that defines itself as “a historical log.” A template evolution (record both dates, or a one-line status history) fixes this for future supersessions.
7. **“Refines” is a relationship the status vocabulary cannot express.** ADR-0022/0002 pioneered a refinement link (including deleting an incidental factual claim from an accepted ADR's text — at the outer edge of ADR-0024's editorial authorization); both display as plain “Accepted” in the index. ADR-0024's own follow-up called for defining such links; the index should surface them.
8. **Recurring minor format strain: compound Decision sentences** (0004, 0006, 0009, 0016 pack two or three clauses into the “one single sentence” the template demands) and occasional design heuristics stated in facts-only Context (0015's “wide constructors indicate missing domain concepts”).

### 6.5 Corpus-level findings: the inversion problem

The corpus's one structural weakness is **topical inversion**: 17 of 26 ADRs govern the *peripheral* packages — RequestBinder (10: 0007, 0008, 0012, 0014, 0016, 0017, 0018, 0019, 0021, 0023) and Dummies/Testing (7: 0006, 0011, 0013, 0015, 0020, 0025, 0026) — while the core library gets exactly two (0003, 0005), both API-naming refinements. Platform floors take three (0001, 0002, 0022), GenDoc two (0009, 0010), process two (0004, 0024).

**Verified missing decisions** — foundational choices a future maintainer would most question, none of which any ADR records:

* the **Outcome/Error model itself** and the exception-vs-outcome duality (why a result type; why `ToException()` as opt-in transport) — the rationale exists in `DesignPrinciples.en.md` and the FAQ, which ADR-0003 cites as authority, but no *decision record* exists;
* the **netstandard2.0 target** for shipped libraries (appears only as context in 0002/0022, which both presuppose it);
* the **class-never-struct value-object rule** — CLAUDE.md contains a complete, ADR-grade rationale that only needs the format;
* the **FCE diagnostic ID scheme and analyzer-bundled-in-main-package choice** (ADR-0019 cites “analyzer FCE009” as a given);
* **GenDoc extraction by runtime reflection** (`Assembly.LoadFrom` + executing documentation factories) — the rationale currently lives in a `SuppressMessage` justification inside the Worker, i.e. argumentation trapped in a code comment;
* the **bilingual EN-canonical/FR-translation documentation policy** (stated as convention in the ADR README, never decided);
* the **lib-train lockstep contract and the core→Testing `InternalsVisibleTo`** (§10.3).

Why this matters is the project's own logic turned on itself: **ADR-0004 makes the ADR base the artifact every PR is checked against.** A PR that converts `ErrorCode` to a struct, adds a runtime dependency to the core, or splits the lockstep train would contradict *no accepted ADR* — the check the project relies on cannot fire on its most important invariants. The distribution currently reads as “decisions of the last two weeks” rather than “decisions of the project.”

**Recommendation (Critical — the only one in this report):** backfill ~6 foundational ADRs as `Proposed`, each with a Context sentence stating explicitly that it records a decision made before the ADR practice began, so history is not falsified. This is cheap (most rationale already exists in CLAUDE.md, DesignPrinciples, the FAQ, and code comments — it needs relocation, not invention) and directly strengthens the governance loop the project already runs. A sketch:

```markdown
# ADR-0027 | Model failures as Outcome values with opt-in exception transport
**Status:** Proposed
## Context
This ADR records a decision that predates the ADR practice (first ADR: 2026-07-10).
Its rationale previously lived in DesignPrinciples.en.md (“the error is the model;
the exception is a transport”) and FAQ.en.md (the Result<T,E> discussion). …
## Decision
An operation's failure path is a first-class Outcome/Outcome<T> value; exceptions are
an opt-in transport derived from the error, never the primary channel.
```

Two smaller corpus-level items: the ADR rulebook itself violates the bilingual convention it states (no `README.fr.md`/`template.fr.md` under `adr/` while all 26 ADRs are translated — either translate or record the exception), and sequential numbering has no mechanical uniqueness guard despite a collision having already occurred in this repository's concurrent-branch workflow (a trivial CI check in the existing `tools/` style closes it).
## 7. ADR Compliance

Compliance was verified ADR-by-ADR against the tree at the audited revision — not by trusting the implementation reference, but by locating the governed code/config/process artifacts and reading them. Summary: **25 of 26 fully compliant; 1 partial (ADR-0026); 0 violated.** For the two superseded ADRs, compliance was assessed as “supersession executed by the book + code follows the successor,” and both hold.

Selected verification evidence (the full set is in §6.2's verdicts; this table shows the load-bearing checks):

| ADR | What was verified in the current tree |
|---|---|
| 0001 | `Directory.Build.props` single-sources `RoslynFloorVersion=4.8.0` with rationale; the Analyzers csproj pins `Microsoft.CodeAnalysis.CSharp` via `VersionOverride` with a “LOAD CONTRACT — do not bump” comment and surfaces the floor as `AssemblyMetadata` so the guard test cannot diverge; `tools/floor-check` exercises the packed `.nupkg` on the pinned floor SDK with `CS8032;AD0001` as errors; Dependabot ignore present. Four independent guards, all live. |
| 0002 | CLI: `net8.0` + `RollForward=Major`; Worker: `net8.0` + `RollForward=LatestMajor` (the distinction is understood, not copied — a worker on `Major` would bind .NET 8 on a machine that has 8 and 10, then fail to `Assembly.LoadFrom` a net10 target); GenDoc deliberately has no RollForward (binds via the CLI's runtimeconfig); CI floor job executes the net8 tooling on the floor runtime. |
| 0003 | No public `To` remains anywhere in the library; unified `Then` surface with sync/async map+bind overloads; async parity on `Task` receivers; `OutcomeThenOverloadResolutionTests` locks the flattening guarantee. |
| 0004 | The process exists in every named artifact: `AGENTS.md` (three outcomes, draft-as-Proposed, maintainer-only status authority), `CLAUDE.md` (essentials inlined), PR template's “Architecture decisions” checkbox section, `adr-check.yml` + `.github/adr-check-prompt.md` (which even implements the alert-fatigue mitigation: “Bias hard toward silence”). |
| 0005 | `FromKelvin`/`FromCelsius` return `Outcome<Temperature>`; `FromKelvinOrThrow`/`FromCelsiusOrThrow` are the marked variants implemented as `FromKelvin(k).GetResultOrThrow()`; old “Attempts to create” wording gone. |
| 0007 | `RequestBinder.New` wraps a total constructor's result in success; `Create` returns the validating factory's `Outcome` as-is (flattened); the named delegates exist because `BindingScope` is a `readonly ref struct` and cannot be a `Func<>` type argument. |
| 0008 | Unconstrained vs `where TArgument : struct` selector pairs on `PropertySource`; struct path unwraps via `GetValueOrDefault()`; null list elements record `REQUEST_ARGUMENT_REQUIRED` under indexed paths. |
| 0009 | `SolutionDocumentationGenerationException : DiagnosableException` with Error-only constructors; 16 `GENDOC_*` codes across request-validation (`PrimaryPortError`) and toolchain (`SecondaryPortError`) factories, each `[DocumentedBy]`-wired with pure example factories; CI generates and asserts the tool's own catalog. |
| 0010 | `release.yml` runs `fce catalog diff --baseline errors-baseline.json --fail-on breaking` on the cli train and refuses publication unless the major was bumped; baseline advances only inside the release procedure; informational PR diff via `gendoc-docs.yml`. |
| 0011 | The no-reference boundary is enforced at three layers: csproj (zero `ProjectReference`, with comment), architecture tests (no referenced assembly starts with `FirstClassErrors`; standard-library-only), pack-time nuspec guard. |
| 0012/0017/0018 | `Bind.Request` captures `RequestBinderOptions.Default` at construction; `ConfiguredBind` is sealed/readonly; options immutable; `Default` freezes under a gate on first read and throws on late assignment; `BinderErrorDefinition` bundles code + message builder evaluated at emission, defaults preserving shipped codes/messages. |
| 0013 | Eager cardinality gate net of pinned-outside values; bounded draw with exhaustion budget and replay-seed-naming exception. (`ICardinalityHint` is `internal` — stricter than the ADR's wording, a latent future decision the audit flagged.) |
| 0014 | Missing derives solely from `null`; empty lists bind empty; all three list converters carry the pinning XML doc verbatim. |
| 0015 | Exactly seven `Combine` overloads (arity 2–8); the S107 suppressions on the widest overloads cite “ADR-0015” by id in their justification strings — exemplary decision-to-code traceability. |
| 0019 | Exactly four public documentation seams (`SampleArgumentRequired/Invalid`, `DescribeArgumentRequired/Invalid`), each with a justified FCE009 suppression; the binder's own internal factories delegate to the same describe-builders, so prose stays faithful by construction. |
| 0020 | Repo-wide grep for `implicit operator` matches exactly one file — `ErrorCode.cs` (out of scope); `IAny<T>` has the single member `Generate()` and its XML docs state the contract. |
| 0021 | Envelope-first untyped entry (`Bind.Request(Func<PrimaryPortInnerErrors, PrimaryPortError>)`); `PropertiesOf<TDto>`/`Argument`/`ArgumentList` feed one envelope; terminals infer `TCommand` from the assembler delegate. |
| 0022 | Dedicated `framework-floor` CI job runs the core, core-property, and binder-property tests with `-f net472 -p:EnableNet472Floor=true` on a real .NET Framework CLR; `build/Net472TestFloor.props` gates the inner build and supplies the polyfill. |
| 0023 | Exactly six `Expression<Func<...>>` selector methods; no delegate+name overload family exists; the Benchmarks project with its README carries every number the ADR cites. |
| 0024 | The implementation reference exists bilingually with maintenance rules encoding the ongoing contract (“Do not move rationale … out of ADRs”); 13 ADRs carry the authorization footer; mechanics landed where they belong. |
| 0025 | `RegexParser` implements exactly the documented regular subset; every out-of-scope construct refused via `UnsupportedRegexException` naming construct and position; oracle test validates generated strings against the real .NET engine. *(Compliant as implementation; the **status** lags — see §6.4.5.)* |

### The one partial: ADR-0026

Code-side, ADR-0026 is fully realized: the Testing package's private engine and `Any` facade are gone; the factories draw from Dummies (`ErrorCodeFactory` uses `Dummies.Any.StringMatching("ANY_CODE_[A-Z0-9]{6}").As(ErrorCode.Create).Generate()`); the clock and instance-id seams participate in Dummies' ambient reproducible context, so **one `Reproducibly` seed replays a whole test run** — the decision's stated payoff.

The partial verdict comes from the decision's *periphery*:

1. **A missed doc-lockstep follow-up:** `DeterministicTesting.en.md` and `.fr.md` (~line 163) still describe the superseded seeding API. This is the only place in the repository where user documentation contradicts shipped API, and the project's own CLAUDE.md sync rule makes fixing it mandatory.
2. **A dormant Decision clause:** no factory yet exposes the promised `IAny<T>`-returning method (“where composition is needed” — nowhere yet). Defensible, but the Decision reads as delivered shape.
3. **An imprecise recorded risk:** the embedded-`Dummies.dll` hazard is real but flows through consumers compiling against the embedded assembly, not (as recorded) through Dummies types in Testing's public API — worth correcting in the implementation reference, since accepted ADRs are immutable.

### Compliance-adjacent observations

Two places where the *implementation is stricter than the recorded decision* (fine today, but each is a latent decision point that should not be resolved by accident): ADR-0013's cardinality capability is `internal`, so foreign `IAny<T>` implementations cannot opt in even when they know their domain; and ADR-0011's second architecture test enforces “standard library only,” which is more than the recorded “no FirstClassErrors reference” rule. One implementation corner case worth recording: ADR-0010's previous-tag lookup compares against the *highest* cli tag, so a hotfix on an older major (`cli-v1.x` after `cli-v2` exists) could never satisfy the major-bump gate — old-major hotfixes are effectively unsupported, which should be either documented as a non-goal or fixed by nearest-lower-tag resolution.
## 8. Architecture Review

This section assesses the structure of the system — decomposition, boundaries, cohesion, coupling, abstraction levels, extensibility, and the patterns holding it together. (Package topology and dependency graph are covered in §10; the public API surface in §9.)

### 8.1 The architectural idea

The architecture rests on three load-bearing ideas, each carried consistently through every subsystem:

1. **Make invalid states unrepresentable, at every altitude.** The core's staged error builder (an error cannot exist without its public message), the taxonomy's typed inner-error collections (a `DomainError` cannot nest an infrastructure error), the binder's token/ref-struct design (an unvalidated value cannot be *read* — `BindingScope` is a `readonly ref struct` constructed exclusively on the zero-failure branch, so misuse fails at compile time), and the binder options frozen before binding begins (ADR-0012: a mixed-naming-policy failure envelope is not detected, it is unconstructible). This is the same principle applied four times at four different layers, which is what architectural coherence actually looks like.
2. **Failure paths are first-class code paths.** “Manufacturing an error never throws” in the core; the GenDoc extractor converting every per-type/per-factory failure into an `ErrorDocumentationExtractionFailure` datum instead of aborting; the CLI's three-tier catch chain reporting coded `GENDOC_*` failures with a test-pinned `CODE: message` line format; Dummies' `ConflictingAnyConstraintException` naming both conflicting constraints at declaration time and bounding every dedup draw with a seed-naming exhaustion error.
3. **Every stated invariant gets a machine check.** Analyzers for code conventions; architecture tests + pack-time nuspec guards for package boundaries; lock-in tests for overload-resolution decisions; snapshot + AngleSharp structural gates for renderer output; packed-artifact dogfood consumers (`floor-check`, `dummies-check`) for load contracts; a release-time catalog diff for the documentation contract. The repository's most transferable practice is that its *comments describe intent and its CI enforces it* — the intent never floats free.

### 8.2 Subsystem-by-subsystem verdicts

**Core library — reference-grade.** ~47 public types with total XML-doc coverage, zero runtime dependencies, thread-safe statics (a lock-protected key registry; internal `AsyncLocal` clock/id seams exposed only to the friend Testing assembly). The one architectural ambiguity is the half-open extensibility contract dissected in §9.3(a). Internal duplication is minor but real: `Error`'s three constructors triplicate a seven-assignment body, and the twin `PrimaryPortInnerErrors`/`SecondaryPortInnerErrors` are structural near-clones.

**Analyzers — reference-grade engineering, uniform hygiene.** One single-responsibility analyzer class per rule (18), shared fact helpers (`ErrorCodeFacts`, `KnownSymbols`, `SymbolFacts`, `OperationFacts`), centralized descriptors/IDs/categories/help-links. Every analyzer enables concurrent execution, skips generated code, registers targeted symbol/operation actions from `CompilationStart` (zero syntax-tree walks anywhere), resolves library types by metadata name and stays silent when the core isn't referenced; the two whole-compilation rules correctly carry `CompilationEnd` tags with per-compilation concurrent state — a subtlety many production analyzers get wrong. The Roslyn 4.8.0 load-contract engineering (four independent guards, one of which packs the real artifact and proves the analyzer *loaded* by grepping the `ReportAnalyzer` table, because a never-loaded analyzer leaves a green build) is rare even in flagship OSS analyzers. Gaps: a hand-rolled test harness that never asserts snippets compile (negative tests can pass vacuously), zero code-fix providers, and five files that re-declare metadata-name constants the shared facts classes exist to centralize.

**GenDoc — the flagship, and it holds.** Five cleanly separated stages that match the handwritten architecture doc almost line for line: attribute+DSL knowledge definition → reflection-*plus-execution* extraction (an error's `Code` and `Context` are derived from actually running the real example factories; a documentation method whose examples disagree on codes throws) → per-assembly process isolation via a 140-line worker launched with `dotnet exec --depsfile` so extraction binds to the *target's own* FirstClassErrors version → aggregation with SDK-driven discovery, timeouts, kill-tree cancellation, and refuse-to-guess opt-in parsing → rendering through a minimal `IErrorDocumentationRenderer` contract deliberately placed in the netstandard2.0 core so plugins need only the main package. Determinism is treated as a contract at every layer (ordered dedup, no timestamps, byte-stable canonical JSON with the .NET-8-floor rationale written down), which is what lets the committed baseline and CI-regenerated docs diff cleanly — the property the whole ADR-0010 workflow depends on. Architectural debts: the RFC 9457 example-rendering logic is duplicated verbatim between the Markdown and HTML renderers (a user-visible schema maintained twice); the 661-line static orchestrator has **no process-runner seam**, so timeout/kill/corrupted-output branches — the source of half the `GENDOC_` catalog — are validated only by happy-path CI; and `DocumentationContractVersionAttribute`'s XML docs claim a generator-side version check **that does not exist** (nothing reads the attribute) — a maintainer would reasonably rely on a guard that isn't there.

**CLI — textbook ports-and-adapters at the right granularity, applied to half the surface.** Extraction commands depend on ports (`IErrorDocumentationGenerator`, `ICatalogSnapshotSource`, `IOutputSink`, a logger factory) with dual production/test-seam constructors and a `CommandContext`-free `Run()` seam; stdout is reserved for the document, stderr for diagnostics; the exit-code contract (0/1/2/130) is documented in code, in user docs, and pinned by tests. But the five config/renderer commands write directly to `Console.Out/Error` with zero tests — a two-tier standard inside one small project, and the likely reason a stale `fce init` reference survives in an error message. Nothing anywhere drives Spectre's real argv surface (no `CommandAppTester`), so the command tree, parsing, and help output — the tool's *declared compatibility contract* — are unpinned. Source-resolution/precedence logic is duplicated in two divergent styles (`GenerateOptionsResolver` returns exit codes; `CatalogSourceResolver` throws) with byte-identical message strings maintained twice.

**RequestBinder — the strongest-engineered area.** ~2,490 LOC, 25 public types, and the audit's most striking single design: compile-time unreachability of unvalidated values via opaque field tokens readable only through a ref-struct scope. Complete ADR traceability (nine accepted ADRs, implementation matching each exactly), evidence-driven performance work (a BenchmarkDotNet project decomposing per-property cost and honestly reporting that ~70–75% is caller-side expression-tree allocation no library change can remove), and precise aggregation semantics (nested envelopes disambiguated *by reference identity*, not type, so a converter that happens to return a `PrimaryPortError` still keeps its path). Architectural debts: the `RequestBinderOptions.Default` getter takes a lock and performs a shared write **on every read** — i.e., on every `Bind.Request` — on precisely the hot path the project elsewhere optimized to sub-microsecond precision (a double-checked volatile fast path preserves ADR-0017's semantics exactly); terminal reuse is silently tolerated, contradicting the binder's otherwise loud bug-channel doctrine; and the converter family (reference/struct × scalar/list/complex) yields ~95%-clone parallel classes that any new presence variant must touch in up to four places.

**Testing/Dummies — professionally bounded.** The split is enforced at three independent layers and post-ADR-0026 cross-package duplication is essentially zero (Testing's 530 lines all delegate to Dummies). Dummies' internals are careful in the hard cases: built-to-satisfy generation (never generate-then-filter), coupon-collector-bounded dedup draws, comparer-aware cardinality accounting, and a 457-line recursive-descent regex-subset parser validated against the real .NET engine as an oracle. Debts: the `Any`/`AnyContext` 26-entry hand-maintained mirror doubles the cost of every new generator with no completeness test, and the flagship determinism property (`Reproducibly`) is adopted nowhere in the repository's own large consuming suites — architecture delivered, practice lagging.

### 8.3 Cross-cutting observations

**Cohesion and coupling are genuinely good.** Each project has one reason to change; the only cross-package couplings are deliberate and recorded (core→Testing `InternalsVisibleTo` for the clock seam; the GenDoc contract types in the core per ADR-0010). No cycles exist even at build time. Abstraction levels are consistent: ports at process boundaries (CLI, GenDoc worker), type-system enforcement at API boundaries, plain code in between — the project does not over-abstract (there is no DI container, no mediator, no speculative interface layer), which for a library ecosystem is the correct restraint.

**The recurring architectural debt is *duplication at pairs*, not misdesign.** Twin renderers duplicating example rendering; twin inner-error collections; twin resolvers in the CLI; the `Any`/`AnyContext` mirror; triplicated `Error` constructors; six-fold binder converter parallelism. Each instance is individually defensible (often the type distinction *is* the point), but collectively they form the project's main maintenance-scaling risk: a fix applied to one twin can silently miss the other, and only snapshot tests would notice after the fact. A small number of internal helper extractions (one `ExampleRendering` type, one shared CLI resolver, one `Error` constructor core, a mirror-completeness reflection test) would retire most of it without touching public API.

**Testability seams are excellent where they exist and conspicuously absent in exactly two places:** GenDoc's process orchestration and the CLI's config/renderer commands. Both gaps have the same shape — code that talks to the outside world (processes, console) without the port the neighboring code models so carefully — and both have cheap, pattern-consistent fixes.

**Long-term evolution is actively managed rather than hoped for.** Platform floors are triple-guarded and ADR-governed with named supersession triggers (the net8.0 tooling floor's LTS expiry in November 2026 is already anticipated by ADR-0002's own risk section); the catalog is a versioned contract with a release gate; pre-v1 API decisions are being settled deliberately (ADR-0018's message-builder shape, ADR-0023's deferred delegate path) while breaking is still free. The one evolution risk the ADRs have *not* caught is recorded in §10.3: the lockstep/IVT contract that permanently binds Testing's version to the core's.

### 8.4 Verdict

Architecturally, this repository is well above the bar the board question implies. The decomposition is right, the boundaries are enforced rather than described, the patterns are applied consistently across five very different subsystems, and evolution is governed. The debts are concentrated, named, and cheap relative to what exists: two missing test seams, a family of twin-duplication risks, one hot-path locking oversight, and one documented-but-nonexistent version check.
## 9. Public API Review

This section evaluates the API as a developer discovering the library today would meet it: through IntelliSense, the README's first example, and the type system's own suggestions. The question asked throughout is the one the board posed: *would this API feel natural and inevitable?*

### 9.1 Shape of the surface

The core package exposes roughly 47 public types that fall into five clusters:

| Cluster | Types |
|---|---|
| Error model | `Error` (abstract), `DomainError`, `InfrastructureError`, `PrimaryPortError`, `SecondaryPortError`, `ErrorCode`, `ErrorContext`, `ErrorContextBuilder`, `ErrorContextKey`/`ErrorContextKey<T>`, `PrimaryPortInnerErrors`, `SecondaryPortInnerErrors`, `PublicMessageStage<TError>`, enums `Transience`, `InteractionDirection`, `ErrorOrigin` |
| Exception transport | `DiagnosableException` (abstract) + one exception per category (`DomainException`, `InfrastructureException`, `PrimaryPortException`, `SecondaryPortException`) |
| Result model | `Outcome`, `Outcome<T>`, `OutcomeTaskExtensions` (24 extension methods) |
| Documentation DSL | `DescribeError` + six staged interfaces (`IErrorTitleStage` … `IErrorExamplesStage`), `ErrorDocumentation`, `ErrorDescription`, `ErrorDiagnostic`, `ErrorContextEntryDocumentation`, attributes `DocumentedByAttribute`, `ProvidesErrorsForAttribute` |
| GenDoc contract | `FirstClassErrors.GenDoc` namespace: `AssemblyErrorDocumentationReader`, extraction result/failure types, `Rendering` contract (`IErrorDocumentationRenderer`, `RenderRequest`, `RenderedDocument`, `RenderLayouts`) |

That is a *large* surface for a library marketed as lightweight, but the clusters are cleanly separated by namespace and by usage moment (modeling time vs. handling time vs. documentation time), which keeps the per-moment cognitive load low. IntelliSense discoverability is excellent because XML documentation coverage is total and unusually substantive: doctrine rationale on `Error`, RFC 9457 field-mapping guidance on the message properties (`ShortMessage` → `title`, `DetailedMessage` → `detail`), and authoring guidance (verb lists, anti-patterns) inside the DSL interfaces. Even `SuppressMessage` justifications are multi-line design notes (`ErrorCode.cs:76-82`). `GenerateDocumentationFile` is on, so all of it reaches consumers.

### 9.2 What is genuinely excellent

**The staged builder as invariant enforcement.** `DomainError.Create(code, diagnosticMessage)` returning a `PublicMessageStage<DomainError>` — a type that is *not* an `Error` — is a textbook example of making the invariant the path of least resistance. The compiler, not a runtime check, guarantees no error exists without its public message. First-time users cannot hold the API wrong.

**One intent-named `Then` instead of `Map`/`Bind` (ADR-0003).** Where functional result libraries force newcomers to learn the map/bind distinction, `Outcome<T>` exposes a single continuation name with three overload families: outcome-returning (`Func<T, Outcome<TResult>>`), value-returning (`Func<T, TResult>`), and their async twins. The subtle risk — C# overload resolution silently producing `Outcome<Outcome<T>>` — was recognized, decided in an ADR, and *locked with a dedicated overload-resolution test class* (`OutcomeThenOverloadResolutionTests.cs`, which turns “a silent `Outcome<Outcome<T>>` nesting into a red test”). This is how a reference library should manage API-shape risk.

**Naming symmetry as a learnable rule (ADR-0005).** The plain name returns an `Outcome`; the `OrThrow` suffix throws (`GetResultOrThrow`; `FromKelvinOrThrow` in the usage samples). One rule, applied everywhere, means a user who has seen one factory pair can predict every other.

**A disciplined async story.** Sync and async variants of `Then`/`Recover`/`Finally` exist across `Outcome`, `Outcome<T>`, `Task<Outcome>`, and `Task<Outcome<T>>`; arguments are validated eagerly *before* the first await (`Core()` local-function pattern, documented as a class-level contract); `ConfigureAwait(false)` is applied throughout (correct for the net472 floor); callbacks that return `null` tasks and tasks that resolve to `null` outcomes are converted into explicit `InvalidOperationException`s at the point of violation instead of surfacing later as opaque NREs (`AsyncCallbackGuard`, `OutcomeTaskExtensions.EnsureNotNull`).

**Thread-safety and test seams without public ambient state.** The only mutable statics are a lock-protected `ErrorContextKey` registry and the `AsyncLocal`-based ambient clock/instance-id overrides — and the latter are `internal`, surfaced only through the friend `FirstClassErrors.Testing` assembly, so the core exposes no public ambient mutable state and overrides flow with `ExecutionContext` (parallel tests cannot leak into each other).

### 9.3 Friction a new developer will actually hit

None of the following is rework-grade; all are refinements. But each one is a place where the API's otherwise-strong “inevitability” breaks.

**(a) The extensibility contract is ambiguous — the API sends contradictory signals.** `Error` and `DiagnosableException` have `protected` constructors and an abstract `ToException()`, which *invites* consumer-defined categories. But `PublicMessageStage<TError>`'s constructor is `internal`, so a third-party category cannot reproduce the staged-construction discipline that is the library's headline feature; and the four concrete categories are `public` and unsealed yet have only `internal` constructors — de-facto sealed without saying so. No user document answers “can I add a category?” (`ErrorTaxonomy`, `CoreConcepts`, and the FAQ were grepped for it). A first-time user who tries `class ConfigurationError : Error` gets a confusing partial success. The contract should be an explicit yes or an explicit no:

* *Close it* (cheaper, matches current analyzer/GenDoc assumptions): document that the four categories are the complete taxonomy, extension happens through factories returning them, and seal or constructor-privatize consistently; or
* *Open it*: give `PublicMessageStage` a `protected`/public creation path so `ConfigurationError.Create(...).WithPublicMessage(...)` is possible for consumers.

**(b) Symmetry gaps that force ceremony the ADRs elsewhere removed.**

```csharp
// 1. Non-generic Outcome lacks the value-mapping Then that ADR-0003 gave Outcome<T>.
// Before — producing a value after a void command:
outcome.Then(() => Outcome<Receipt>.Success(receipt));
// After (recommended addition, same overload-betterness proof as ADR-0003):
outcome.Then(() => receipt);

// 2. Port errors lack the single-inner-error Create that DomainError and
//    InfrastructureError both have — yet wrapping ONE domain error is the
//    single most common boundary case.
// Before:
PrimaryPortError.Create(code, msg, new PrimaryPortInnerErrors().Add(domainError));
// After (recommended addition):
PrimaryPortError.Create(code, msg, domainError);
```

Additionally `PrimaryPortInnerErrors`/`SecondaryPortInnerErrors` implement neither `IEnumerable` nor a params-style factory, so no collection initializer works either. These asymmetries are exactly the kind of thing a first-week user trips on, because the library has taught them to expect symmetry.

**(c) The process-global `ErrorContextKey` registry has a harsh cross-assembly failure mode and no namespacing guidance.** Key identity is a process-global name. Two independent packages registering the same name with *different* value types produce an `InvalidOperationException` at the loser's type initialization — typically surfacing as an opaque `TypeInitializationException` in code its author never touched (`ErrorContextKey.cs:133-154`). Same-name/same-type registrations silently merge with the first description winning. The XML docs describe this correctly, but the user guide's key section (`ErrorContext.en.md`) offers no prefixing convention, and the `#` prefix used by framework keys is not documented as reserved. Worse, canonical sources model **three different naming conventions**: XML-doc examples use PascalCase (`DealId`, `CorrelationId`), the user guide uses SCREAMING_SNAKE (`ORDER_ID`, `STATEMENT_ID`), and framework keys use `#`-prefixed SCREAMING_SNAKE. Since key names are the serialization/log contract, two teams following the two official examples will produce inconsistent observability vocabularies — the precise drift the library exists to prevent.

**(d) Missing nullable flow annotations at the most-used lookup API.** `ErrorContext.TryGet<T>(key, out T? value)` lacks a `[NotNullWhen(true)]` annotation, so NRT-enabled consumers must null-forgive after a `true` return. The team demonstrably knows the netstandard2.0 gap (an internal comment in `ErrorDescription` explains it), but the standard mitigation — internally declaring the compiler-recognized attributes and embedding them — is applied nowhere. One attribute declaration file fixes this at zero runtime cost:

```csharp
public bool TryGet<T>(ErrorContextKey<T> key, [NotNullWhen(true)] out T? value)
```

**(e) The exception→Outcome direction of “the exception is a transport” has no helper and no documented pattern.** The forward direction is first-class (`ToException()`, `ThrowIfFailure()`, `GetResultOrThrow()`). The reverse — at a boundary that consumes throwing code — is the unassisted idiom `catch (DiagnosableException ex) { return Outcome<T>.Failure(ex.Error); }`, which appears in *no* guide and no sample (grep across all `.cs` and `.md`: zero hits). Newcomers are left to discover that `ex.Error` round-trips losslessly. Documenting the idiom (and optionally adding `Outcome.From(DiagnosableException)`) completes Principle 2's round-trip; a general exception-capturing `Outcome.Try(Action)` should *not* be added, as it would contradict the library's stance that arbitrary exceptions are not modeled errors.

**(f) Small polish items.** `ErrorCode.Unspecified` is `internal`, so consumers who want to alert on degraded errors (the operational point of the `#UNSPECIFIED` sentinel) must compare magic strings — the sentinel is public doctrine, its canonical instance should be public too. XML-doc drift: `Error.ToException()` documents an `InvalidOperationException` that no override can throw; an `S4136` justification still references the `To` method removed by ADR-0003; `Outcome`'s summary says “fail without throwing an error” where *exception* is meant. Internally, `Error`'s three protected constructors triplicate an identical seven-assignment body (a future field must be added three times), and the twin `InnerErrors` collections are structural near-clones.

### 9.4 Verdict

Reference-grade API design. The staged builder, the ADR-governed `Then` unification with lock-in tests, the never-throws doctrine, and the total XML-doc coverage put this API above every mainstream .NET result library in *design discipline*. The friction list is real but consists of symmetry completions, one contract clarification (extensibility), and documentation of already-correct behavior — refinements, not rework.
## 10. Ecosystem Review

### 10.1 The actual topology

The repository is a five-package, three-release-train mono-repo of 24 `.csproj` files (22 in the solution, plus two deliberately out-of-solution dogfood consumers under `tools/`). What ships, and how:

| Unit | TFM | Train | Role |
|---|---|---|---|
| `FirstClassErrors` | netstandard2.0 | `lib` (tag `lib-v*`) | Core; **zero runtime NuGet dependencies**; bundles the analyzers at `analyzers/dotnet/cs` |
| `FirstClassErrors.Testing` | netstandard2.0 | `lib` (lockstep) | Test seams (frozen clock/ids), outcome assertions; friend assembly of the core |
| `FirstClassErrors.RequestBinder` | netstandard2.0 | `lib` (lockstep) | Primary-adapter boundary binder |
| `FirstClassErrors.Cli` (`fce`) | net8.0 | `cli` (tag `cli-v*`) | dotnet tool; bundles the GenDoc worker inside the tool package |
| `Dummies` | netstandard2.0; net8.0 | `dum` (tag `dum-v*`) | Standalone test-data library (ADR-0011) |

Everything else — Analyzers, GenDoc, GenDoc.Worker, two Usage samples, ten test projects, a Benchmarks project, a docs-browsing csproj — is `IsPackable=false` internal structure. The dependency graph is strictly acyclic and layered: the core depends on nothing; Testing/RequestBinder/GenDoc/Worker sit one layer up; the CLI on top. The only downward-looking edges (core→Analyzers for pack ordering, Cli→Worker for bundling) are `ReferenceOutputAssembly=false` build-order edges, so no cycle exists even at build time — and the Analyzers project references *no* repository project at all, so it can load in any Roslyn host.

**nuget.org state (verified during the audit):** `FirstClassErrors` and `FirstClassErrors.Testing` are published at `0.1.0-preview.1`; `FirstClassErrors.RequestBinder`, `FirstClassErrors.Cli`, and `Dummies` are unclaimed IDs.

### 10.2 What elevates this ecosystem above typical OSS practice

**Boundaries are machine-enforced, not aspirational.** Every structural rule the ecosystem depends on has an executable guard:

* Dummies' standalone rule (ADR-0011) is checked at build time by an architecture test (`Dummies.UnitTests/ArchitectureTests.cs`) *and* again on the shipped artifact by a pack-time nuspec assertion (`tools/packaging/pack.sh`).
* The `lib` train's lockstep versioning is asserted on the packed `.nupkg`: any intra-train dependency not pinned to the co-published version fails the pack.
* The `fce` tool's worker bundling is asserted by grep on the `.nupkg`.
* `tools/floor-check` and `tools/dummies-check` consume the **actual packed artifacts** as real downstream consumers — floor-check proves the bundled analyzer loads under the pinned floor SDK's Roslyn (with `CS8032;AD0001` escalated to errors), dummies-check proves per-TFM asset selection from net8.0 and net6.0 consumers — both deliberately outside the solution, with the reasoning written into the csproj files.

This closes exactly the “green build but broken package” gap most projects leave open, and it is the single most transferable practice in the repository.

**Justified decomposition, including the one that looks odd.** `GenDoc.Worker` as a separate ~140-line project is architecturally necessary, not accidental: it must run in its own process with `RollForward=LatestMajor` so it can bind to whatever runtime the *inspected consumer assembly* needs (`dotnet exec --depsfile` against the target's closure) — a policy the CLI itself (`RollForward=Major`) cannot carry. The split between the shipped extraction contract (in the core, version-locked with the consumer per ADR-0010) and tooling-side generation/rendering is equally deliberate.

**A single source of truth for release topology.** The train→tag-prefix→scopes→changelog→package mapping lives once in `tools/trains.sh` and is sourced by the packaging and changelog tooling; `pack.sh` is shared verbatim between the real release and the dry-run workflow; adding a train has a documented checklist (`AddingAReleaseTrain.en.md`).

**Complete, consumer-oriented NuGet metadata across all five packages**: substantive descriptions, curated tags, Apache-2.0 expression, shared icon, per-package `README.nuget.md`, snupkg symbols with SourceLink (deliberately disabled for the tool package, with the reason stated), embedded SPDX SBOM, and a documented decision *not* to hardcode stale-prone `PackageReleaseNotes`. Consumer composition is the simplest possible: `dotnet add package FirstClassErrors` pulls the analyzers automatically — no separate analyzer package to forget.

### 10.3 Structural debt — small, concentrated, and mostly sequencing

**(1) `FirstClassErrors.Testing` smuggles an unpublished `Dummies.dll` inside its own `lib/` folder (High).** Because Dummies is not yet on NuGet, Testing references it with `PrivateAssets=all` and embeds the DLL via a custom pack target (`IncludeDummiesInPackage`). Four consequences: the Dummies version a consumer runs is invisible to and unmanageable by NuGet; once Dummies publishes, a consumer referencing both Testing and Dummies gets two same-identity assemblies with no resolution mechanism; since Testing targets only netstandard2.0, the embedded copy is the *downlevel* Dummies asset, so net8+ consumers silently lose the modern-type generators (`DateOnly`/`TimeOnly`/`Int128`/`Half`) the real package's net8.0 leg would give them; and per ADR-0026, Dummies types (`IAny<T>`) are part of Testing's *public API*, so this is a hidden dependency on a public-surface type provider. The csproj documents the exit plan (“switch to a PackageReference once Dummies is published”), which mitigates but does not remove the risk while the window is open. This is the ecosystem's most consequential defect, and it is resolved by executing an already-made decision.

**(2) The generic `Dummies` package ID is unclaimed while its release train already exists (Medium).** ADR-0011's own premise is that “a package identity is costly to rename after adoption.” The ID is a common English word, verified unclaimed at audit time; until first push, any third party can take it, and NuGet prefix reservation cannot protect a single generic word the way `FirstClassErrors.*` is de-facto anchored by its two published packages. Every guard needed for a safe first publish already exists in `pack.sh`. This is pure execution risk with a cheap close.

**(3) A binder-only consumer silently loses the analyzers (Medium).** `dotnet pack` writes `exclude="Build,Analyzers"` on nuspec dependencies by default, so `FirstClassErrors.RequestBinder`'s dependency on the core does **not** flow the bundled FCE analyzers to a consumer who installs only the binder — while the binder's whole pitch is “coded, documented `PrimaryPortError` trees” whose documentation discipline those analyzers enforce. The repository itself knows a plain reference does not carry analyzers (it wires them explicitly for its own dogfood, per the comment referencing issue #153), but `RequestBinder.en.md`'s install section presents the binder as a standalone `dotnet add package` with no companion-reference note. The stronger fix is to bundle the same analyzers into the binder package (Roslyn deduplicates by assembly identity, and lockstep guarantees version match); the cheaper fix is one documented sentence.

**(4) Two lasting decisions live only in comments, not ADRs (Medium).** (a) The `lib`-train lockstep contract — *why* the binder must not have its own train — exists only in `pack.sh` comments and one line of user doc. (b) The core grants `InternalsVisibleTo` to the shipped `FirstClassErrors.Testing` package (the ambient-clock seam), which makes exact-version lockstep *permanently mandatory* — internals are not a compatibility surface, so Testing can never version independently while that IVT exists. Both pass the project's own ADR significance test verbatim, and the asymmetry with ADR-0011 (which records the equivalent hosting decision for Dummies) is stark. If `pack.sh` were ever refactored, the only record of the reasoning would vanish with it.

**(5) Minor solution-taxonomy inconsistencies (Low).** The docs-browsing project claims to be excluded from solution build configurations, but only the AnyCPU configurations omit it (`x64`/`x86` configs still build it). The Usage samples and the Benchmarks project are nested under the `tests` solution folder, blurring an otherwise clean src/tests taxonomy. Two `InternalsVisibleTo` idioms coexist. And the `FirstClassErrors.GenDoc` *namespace* spans two assemblies (the shipped contract in the core, the tooling in the non-packable project) — sound placement rationale per ADR-0010, but a discoverability smell worth a doc note.

### 10.4 Should anything merge, split, or move?

The audit examined the standard mono-repo questions and found the current decomposition defensible in every case: **GenDoc.Worker** must stay separate (runtime roll-forward policy); **Analyzers** must stay dependency-free (Roslyn host loading) and are correctly delivered inside the core package rather than as a fourth install; **RequestBinder** belongs in-repo while it ships in lockstep and reuses the core's error taxonomy as its public contract; **Dummies** is the only genuine candidate for extraction, and its ADR already defines the triggers — the recommendation (§14) is to *actively evaluate* those triggers at the 1.0 milestone rather than waiting for them to fire. No project needs to be created, merged, or split today.

### 10.5 Verdict

The ecosystem structure is coherent, professional, and — in its enforcement discipline (pack-time guards, packed-artifact dogfooding, single-sourced train topology) — genuinely reference-quality. The weaknesses are early-stage *sequencing* issues (an unpublished dependency, an unclaimed ID, two unrecorded decisions), not architectural flaws.
## 11. Documentation Review

Documentation is part of the product here — the project's thesis is literally about documentation that does not drift — so this audit treated it as product: ~5,900 lines across 30 English user guides plus a 19-page analyzer reference (all bilingual), a maintainer layer (12 workflow reference pages, two runbooks, an ADR implementation reference, 26 bilingual ADRs), and the generated catalog under `doc/generated/`.

### 11.1 Content quality and accuracy: near-reference grade

**Accuracy against the implementation is the standout result.** The audit spot-checked over 40 API references across 12 guides — including the entire 724-line RequestBinder guide (verified type-by-type against the binder source), the async Outcome pipeline, the Testing/Dummies helpers, the DescribeError DSL, the programmatic GenDoc pipeline, and every CLI command, option, and exit code cited. **Exactly one factual API error exists in the whole English user-doc set**: `DeterministicTesting.en.md` (~line 163) claims `Clock.UseAny()`/`InstanceIds.UseAny()` “both take an optional seed” — both are parameterless since the ADR-0026 rebase; reproducibility now flows through `Dummies.Any.Reproducibly(...)`, which the sibling guide documents correctly. (This is also ADR-0026's one missed follow-up; the same sentence is stale in the French twin.) A scripted link check found zero broken relative links in the EN user docs. `ComparisonWithOtherLibraries.en.md` carries an explicit review date six days before this audit — evidence of active maintenance, not archaeology.

**Pedagogy is genuinely layered and deliberately anti-duplicative.** The reading path runs pitch → GettingStarted (zero to generated catalog in six concrete steps) → principles → model (CoreConcepts/ErrorTaxonomy/ErrorContext) → authoring → consumption → testing trio → operations → extension docs, with prev/next footers forming a guided chain across 19 guides, per-guide scope statements, mermaid “model in one minute” diagrams, and closing review checklists framed for pull-request review. Overlapping guides delegate rather than restate (“Detailed explanations belong in the focused guides”), and no semantic contradictions were found between guides on the three-message model, composition rules, transience, or the throw-vs-return doctrine. The honest-scoping documents (WhenNotToUse, the FAQ's opening “Why not just use normal exceptions? You can.”, the non-ranking comparison) build exactly the credibility a reference project needs.

### 11.2 The systemic weakness: discoverability, not content

The defects cluster on *hub pages and linkage*, not prose:

* **Both discovery hubs are incomplete.** The README's Documentation section omits six English guides (including `OutcomeGuide` and `DocumentationMap` itself); `DocumentationMap` — the intent-based navigation page — omits six others, most notably the 724-line RequestBinder guide, and is itself reachable from exactly one page footer. `DocumentationMap.en.md:8` asserts the README “lists every document by area,” which is currently false.
* **The sample projects are nearly invisible (High).** Neither README, GettingStarted, nor DocumentationMap links either Usage project. `FirstClassErrors.Usage` — the compiled, snapshot-tested realization of every guide's pattern — is linked from *zero* user documents. The project's best teaching asset is undiscoverable from its documented reading path.
* **Minor chain defects**: three guides have non-reciprocal prev/next links (RequestBinder sits in nobody's chain); `WhenNotToUseFirstClassErrors` is markedly thinner than its siblings despite being a primary “Discover” entry point; and context-key naming is modeled inconsistently across guides (UPPER_SNAKE in ErrorContext vs PascalCase in Testing/Logging/XML docs — the same inconsistency §9.3(c) found in the API docs).

### 11.3 Documentation/sample drift (the only substantive contradictions found)

Three places where docs and compiled samples disagree — significant because the samples are the copy-from material:

1. **`UsagePatterns` vs the shipped `Amount`.** The guide shows an Outcome-returning `Amount.Add` and prescribes “centralize validation in the Outcome-returning version and derive the throwing one from it — not the reverse.” The shipped sample `Amount` has *only* `AddOrThrow`/`SubtractOrThrow`, throwing directly — realizing the anti-pattern the doc warns against (while `Temperature` follows the rule). The doc snippet also uses `Currency != other.Currency` though the sample `Currency` defines no operators.
2. **RequestBinder guide overclaims sample coverage.** It says the sample showcases “every remaining overload and option,” but the documented out-of-DTO argument path (`bind.Argument(...).FromRoute/FromQuery`, provenance keys) appears nowhere in the sample or its tests — a first-class binder feature with no compiled example (also flagged by the ADR-0021 audit).
3. **Sample tests don't dogfood `FirstClassErrors.Testing`** although the guide says “the binder returns an Outcome, so the testing helpers apply directly” — the sample tests hand-roll exactly the context-digging boilerplate the Testing package exists to remove.

### 11.4 French/English synchronization: close to exemplary, with two confirmed drifts

The mirror covers **185 files: 92 EN/FR pairs with zero French orphans**; only the ADR rulebook (`adr/README.md`, `template.md`) is untranslated. Deep comparison of eight representative pairs (root README, OutcomeGuide, RequestBinder, ADR-0026, workflows/ci, Internationalization, LoggingIntegration, CONTRIBUTING) found perfect heading-for-heading and code-block-for-code-block parity, with a principled localization convention (identifiers/API English; string literals, comments, and mermaid labels translated) and per-language supersession links in the ADR mirror. Across ~9,500 mirrored lines, exactly two drifts exist — both traceable to EN-only commits:

1. **`CONTRIBUTING.fr.md` lists 5 commit scopes where English (and the enforcing linter) has 7** — the `binder` and `dummies` rows are missing, so a French-reading contributor sees a narrower contract than CI enforces.
2. **20 of 25 French user guides footer-link to a dead anchor** (`README.fr.md#-étapes-suivantes`, a heading that no longer exists) — the French mirror of an English bugfix whose commit message *incorrectly asserted* the French pages were already correct.

The structural finding is governance: **the stated policy covers 1 file while the practice covers ~120.** `AGENTS.md` says “French lives only in `doc/handwritten/for-users/README.fr.md`”; CLAUDE.md and the PR template likewise name only the README. Only the ADR rulebook states the real policy (English canonical, French follows). And no tool checks pairing, freshness, or FR link integrity — despite the repo owning five bespoke consistency tools. Both observed drifts are mechanically detectable; both occurred within days of EN-only commits. The mirror is sustainable *today* because it is maintained with unusual discipline; codifying the policy and adding an advisory sync check (pair completeness + link resolver + warn-on-EN-only-change) is what makes it sustainable as contributors multiply.

### 11.5 Maintainer documentation: high quality, lagging a fast-moving pipeline

The workflow reference layer (12 pages with “Handle with care” sections recording intent a well-meaning cleanup would destroy, plus the stated precedence rule “when the page and the YAML disagree, the YAML wins — and the page should be corrected”) is a rare and genuinely valuable asset. But the July 17–19 pipeline churn outran it: **3 of 15 workflows (canary, dummies, gendoc-docs) have no reference page at all**; `ci.en.md` documents two of ci.yml's three jobs (the net472 framework-floor job — ADR-0022's enforcement point — is invisible in the doc layer); `release-dryrun.en.md` still says “the two published projects” while the yml packs three trains. Most consequentially, **`CONTRIBUTING.md` contradicts the release machinery**: it states the commit scope “carries no versioning weight, only readability,” while the train tooling now *silently drops unscoped commits from release notes and changelogs* — a contributor following CONTRIBUTING to the letter can make a user-facing change invisible to the release record. A constellation of smaller stale pointers (the changelog preamble naming two of three lockstep packages, “ADRs are English only” in the maintainer README, a stale tag-format example, an outdated train allowlist quote) individually trivial, collectively blur the doc layer's authority.

### 11.6 Verdict

Content quality, accuracy, pedagogy, and bilingual discipline are all at or near reference grade — the handwritten docs demonstrably practice the no-drift thesis the product sells. The failure mode this documentation set actually has is *inventory drift at the edges*: hub pages, workflow references, CONTRIBUTING, and the FR mirror all decayed slightly in the same 72-hour window of rapid pipeline change, because every one of these surfaces is maintained by hand in a repo that machine-checks everything else. The remedy is the project's own medicine: three small advisory checks (doc-hub inventory, workflow-doc coverage, FR-pair/link sync) in the existing `tools/` pattern.
## 12. Developer Experience Review

This section walks the adoption journey end to end: discovery → install → first error → first catalog → daily development → testing → operations, and evaluates what the developer actually experiences at each step.

### 12.1 Discovery and first contact

The README is a strong first contact: a concrete problem statement (“a production error is rarely useful as only a type and a string”), one complete motivating example, an honest “when not to use this” link, and a badge row (CI, SonarCloud quality gate and coverage, CodeQL, OpenSSF Best Practices and Scorecard) that signals engineering seriousness to exactly the audience a reference library needs. Two storefront defects undercut it:

* **The core package README on nuget.org claims “16 Roslyn analyzers (FCE001–FCE016)” — the product ships 18.** Small, but it is drift in the flagship anti-drift claim, on the page every evaluator reads first.
* **The CLI package README documents commands that don't exist** (`fce renderer add|list|remove` instead of the real `fce config renderer …`), misdescribes `config show`, and *omits the catalog-versioning commands entirely* — the very feature the release pipeline is built around. A new user following the package README gets a command-not-found error and never learns the flagship feature exists. This is the highest user-impact-per-line fix in the entire audit.
* **The repository README's install section points at packages that are not on NuGet.** `dotnet add package FirstClassErrors.RequestBinder` (line 118) and `dotnet tool install --global FirstClassErrors.Cli` (line 124) both fail today — verified against nuget.org (BlobNotFound for both IDs). A first-day evaluator following the README hits a dead end at step one for two of the three advertised installs.

### 12.2 Installation and the first error

`dotnet add package FirstClassErrors` is genuinely all it takes: zero runtime dependencies arrive, and the 18 analyzers come bundled — no second package to know about. The staged builder then makes the first error hard to get wrong: `DomainError.Create(code, diagnosticMessage)` won't produce an `Error` until `WithPublicMessage(...)` is called, and IntelliSense carries substantive guidance (RFC 9457 mapping on the message properties, authoring advice in the DSL, doctrine rationale on `Error` itself). XML documentation coverage is total, and it shows: this is a library a developer can learn largely from tooltips.

Known gaps at this step: no user-facing statement of the **minimum SDK/IDE needed for the bundled analyzers to load** (the 4.8.0 Roslyn floor is documented only in maintainer docs — a consumer on an old VS sees analyzers silently absent with no doc to explain it), and the never-throws sentinels (`#UNSPECIFIED`, `#MISSING_SHORT_MESSAGE`…) that an operator may actually meet in production are searchable nowhere in the handwritten docs.

### 12.3 Build-time feedback: the analyzers

The analyzer experience is carefully engineered to be trustworthy: default-on rules are confined to hard defects and likely mistakes; every heuristic rule (naming, denylists, sensitive-data detection) is opt-in with candid documentation of its false-positive modes and the exact suppression to use. Diagnostics link to per-rule bilingual doc pages that actually resolve. Two DX gaps keep this below the bar set by the analyzer suites this project will be compared against (Roslyn, xUnit, Meziantou):

* **Zero code-fix providers.** Seven rules have deterministic fixes; the strategic one is FCE009, whose fix would *scaffold the DescribeError documentation skeleton* — actively teaching the convention the suite enforces, at the moment the developer is most receptive.
* **The library's central invariant is unenforced: a silently discarded `Outcome`.** `error.ToException()` discarded gets FCE016, but `validator.Check(request);` — dropping an `Outcome` on the floor, precisely the failure the library exists to eliminate — produces no diagnostic. This is the single largest gap between thesis and enforcement (recommended as FCE019 in §14). A second cheap, high-alignment rule: `ErrorContextKey` name/type conflicts are a documented runtime `InvalidOperationException`, statically detectable exactly like FCE001 detects code collisions.

### 12.4 The catalog moment

`fce generate` is the product's “aha” moment, and mostly delivers: GettingStarted reaches a generated catalog in six honest steps, the CLI's stdout/stderr discipline and exit-code contract make CI composition natural, and failures are reported as stable `GENDOC_*` codes (the tool practicing what the library preaches). Friction points: the HTML renderer's doc page omits the mandatory `--service-name` from its copy-paste examples (the command fails as written); `fce generate` never prunes stale output, so adopters who wire up the documented CI publishing loop accumulate ghost pages for removed errors unless they rediscover the `rm -rf` workaround buried in the repo's own workflow comment; and the CLI's *own* operational failures (renderer not found, missing baseline) are uncoded free-form strings — the first-class-error story currently stops at the GenDoc boundary.

### 12.5 Daily development, debugging, and testing

Day-to-day ergonomics are strong: the `Then`/`Recover`/`Finally` vocabulary is small and predictable, sync/async parity means the pipeline shape survives an async refactor, `DebuggerDisplay` attributes and the three-message split make inspection sensible, and `InstanceId`/`OccurredAt`/`Context` give logs correlation handles by default. The testing story is a differentiator on paper — frozen clocks/ids, outcome assertions, seedable arbitrary values with whole-run replay — with two adoption-side caveats the audit verified: the repository's own large suites never opt into `Reproducibly` (so the flagship replay property has no real-world mileage — a failure caused by a coincidental arbitrary value in the main suite is unreplayable today), and the anticipated xUnit adapter (`[ReproducibleFact]`) was never built. `ErrorAssertion` also stops one step short of the guide's own recommended assertions (no fluent error-type/transience/inner-error checks; users fall back to the `Subject` escape hatch).

### 12.6 The adoption cliff: from library to running service

The most consequential DX finding is the **missing last mile**. The binder deliberately returns framework-agnostic outcomes, but:

* No sample or doc anywhere shows mapping an `Outcome` failure onto an HTTP response (status, RFC 9457 problem type, public messages, per-argument errors) — even though the generated catalog *already emits* problem-type URIs and RFC 9457 examples.
* Out of the box, binder error paths are C# property names, not wire keys: every real consumer of a camelCase/snake_case JSON API must hand-write an `IArgumentNameProvider` before `REQUEST_ARGUMENT_*` paths match their JSON — and the binder's own generated documentation lists this mismatch as a known failure diagnosis. The code comment anticipating “a host integration package may add richer helpers” has no package behind it.
* A binder-only install silently loses the analyzers (§10.3), so the consumer most likely to be documenting a boundary error surface is the one least likely to have the rules enforcing it.

None of this blocks a determined adopter, but it front-loads exactly the integration work a first evaluation penalizes. §13 develops the integration-package answer.

### 12.7 Verdict

The developed surface is polished to an unusual degree — IntelliSense, analyzers, CLI ergonomics, and test seams all show deliberate DX craft. The experience degrades at the *edges the project hasn't built yet*: two inaccurate storefronts, a missing HTTP last-mile, wire-name friction in the binder, no code fixes, and a discarded-Outcome blind spot. All are addressable without touching the core design, and several (storefront fixes, doc links, `--service-name` examples) cost minutes.
## 13. Feature Gap Analysis

This analysis assumes the project's stated ambition — to become one of the best libraries in its domain — and asks what is missing *by the project's own standards*. It draws on a competitive comparison (ErrorOr, FluentResults, OneOf, LanguageExt, CSharpFunctionalExtensions, Ardalis.Result, DotNext.Result, and the real incumbent: plain exceptions + ASP.NET Core ProblemDetails), an integration-surface review, and a tooling review. Fashion-driven ideas are explicitly parked in §13.6.

### 13.1 Where FirstClassErrors already wins: a four-part moat nobody else occupies

Before gaps, the position: FirstClassErrors is **not a me-too result library**. Every mainstream competitor centers on the result/transport abstraction; FirstClassErrors centers on the *error definition* — stable code, triple-audience messages, typed context, taxonomy — and treats `Outcome` and exceptions as interchangeable transports of one definition. Four capabilities exist in no competitor:

1. a **code-generated, versioned, localized error catalog** (HTML/Markdown/JSON, five cultures, RFC 9457 examples);
2. **error-contract breaking-change detection** (`fce catalog diff` against a committed baseline, release-gated per ADR-0010);
3. **18 shipped Roslyn analyzers** enforcing the model at compile time, bundled in the main package;
4. a **boundary binder** that reuses value-object factories as validation and aggregates all failures under a documented envelope error.

Additional model-level capabilities that exceed competitor equivalents: the staged-builder-enforced public/internal message separation, the typed registered context-key vocabulary (vs `Dictionary<string,object>` metadata), operational semantics in the taxonomy (`Transience`, `InteractionDirection`), and the never-throws construction doctrine. The comparison document itself is of reference quality — dated, non-ranking, self-critical — though it covers only two of the seven-plus relevant alternatives.

### 13.2 The single highest-leverage gap: the HTTP last mile (Critical)

**Problem.** The core model was *designed* for RFC 9457 — `ShortMessage` is documented as the problem `title`, `DetailedMessage` as `detail`, and the generated catalog mints `urn:problem:{service}:{code}` problem types (the CLI mandates `--service-name` precisely to render them). Yet **no runtime code maps an `Error` to a ProblemDetails shape anywhere**; the URN builder lives only in the net8.0 GenDoc tooling, unreachable from a deployed netstandard2.0 app. Every adopter hand-writes the one mapping where Design Principle 3 (public/internal separation) can be silently violated, and the catalog's documented HTTP examples can drift freely from actual API responses — *the exact drift the library's thesis exists to eliminate, relocated to the HTTP boundary*. The comparison page even concedes this axis (“Choose ErrorOr when … error types are mapped to HTTP responses”).

**Proposal.** Two layers, following the repo's own satellite-package precedent (RequestBinder, Testing, Dummies):

* In the core (dependency-free, netstandard2.0): a neutral projection —

```csharp
ProblemDetailsModel problem = error.ToProblemDetails(new ProblemDetailsOptions {
    ServiceName = "checkout-api",          // type = urn:problem:checkout-api:payment-declined
    CatalogBaseUri = "https://docs.co/errors/2.4.0/",
    IncludeDetailedMessage = true          // explicit opt-in, mirroring the DetailedMessage contract
});
// title = ShortMessage, detail = DetailedMessage, extensions: code, instanceId, occurredAt.
// DiagnosticMessage is unreachable from this API — by construction.
```

This requires extracting the `ProblemType` URN builder from GenDoc into the core (or a shared source file) so runtime and catalog *provably* compose the same URN.

* A `FirstClassErrors.AspNetCore` package (net8.0+): `Outcome<T>` → `IResult`/`IActionResult` terminals, an `IExceptionHandler` for `DiagnosableException`, a default taxonomy-driven status policy (DomainError/PrimaryPortError → 4xx; SecondaryPortError by `Transience` → 502/503/500) with per-code overrides, and `services.AddFirstClassErrors(serviceName, …)` that also bridges `RequestBinderOptions.Default` at composition time.

**Who benefits:** API teams (the largest adopter segment) and support/operations (a wire `type` that resolves to the published catalog page). **Why it fits:** it makes the safe mapping the default and completes the catalog promise; it is a *transport* in the project's own vocabulary. **Value: Critical** — this is the gap evaluators will bounce off first.

### 13.3 High-value gaps

**(a) Ship the canonical log projection (High).** `LoggingIntegration.en.md` instructs “project an Error to a log model once, in one place” — then makes every adopter copy ~25 lines whose subtleties (recursive `InnerErrors`, `InfrastructureError`-only fields, the `InnerException` distinction) are exactly where a hand copy degrades. The projection derives entirely from the model's shape, not application policy, so its home is the library: `Error.ToLogModel()` returning `IReadOnlyDictionary<string, object?>`, netstandard2.0, zero dependencies; the doc shrinks to one line. Defining the resulting field names as a stable documented schema simultaneously fixes the cross-service correlation gap (two services on this library currently emit structurally different error events unless teams coordinate out-of-band).

**(b) Boundary exception capture, on the project's terms (High).** The FAQ prescribes the adapter pattern (catch third-party exception → model via a documented factory → keep the runtime exception as technical cause) and `DiagnosableException(error, innerException)` exists for it — but there is no `Outcome.Try`, so the most common infrastructure-adapter pattern is per-call-site boilerplate. Unlike FluentResults' auto-wrapping `Result.Try` (which would violate Principle 1), the signature should *force* a factory-produced `Error`:

```csharp
Outcome<Receipt> outcome = Outcome.Try(
    () => providerClient.Charge(order),
    ex => PaymentProviderError.Unavailable(ex));   // must return Error — your documented factory
```

**(c) Code-fix providers, with FCE009 scaffolding as the flagship (High).** All 18 diagnostics are report-only. The library's only real adoption tax is the ~25-line documentation-method boilerplate per error — precisely scaffolding-shaped. An FCE009 fix generating the `[DocumentedBy]` wiring plus a `DescribeError` skeleton converts the analyzer suite from a critic into a teacher, at the moment of maximal receptivity. Mechanical fixes for FCE004/006/008/016 come almost free alongside.

**(d) A discarded-`Outcome` analyzer — FCE019 (High; arguably the most philosophy-aligned single item in this report).** `validator.Check(request);` silently dropping an `Outcome` is the library's central failure mode, and no rule flags it (FCE016 covers only discarded `ToException()`). Warning severity, on by default, mirroring FCE016's discard logic keyed on return type.

**(e) `fce lint` — content-quality rules on the extracted catalog (High).** Documentation text may legally come from localized resources, which literal-based Roslyn rules (FCE005/014/015) structurally cannot see — a team that localizes loses *all* text-quality enforcement, silently. A CLI verb applying the BestPractices/WritingErrorsGuide checklists to the post-extraction, post-localization catalog (per locale, with the existing `--report`/`--fail-on`/exit-code conventions) closes the blind spot and makes the prose checklists executable.

**(f) Widen the comparison page (High, docs-only).** Add the incumbent baseline — exceptions + built-in ProblemDetails — worked through the same payment scenario, plus a short positioning table for the union/functional families (OneOf/LanguageExt, CSharpFunctionalExtensions, Ardalis.Result). The FAQ already contains the arguments; they are not surfaced where evaluation happens.

### 13.4 Medium-value gaps

* **Domain-side multi-error aggregation.** The comparison table concedes “aggregating independent errors is left to the application.” The principled shape exists (failures group under a *documented envelope error*, never an anonymous list — exactly how `Bind.Request` works): an `Outcome.Combine(envelopeFactory, outcome1, …, assembler)` gated on a documented aggregate factory, arity-capped in the spirit of ADR-0015, would turn the concession into “built in, on our terms.”
* **OpenTelemetry semantic conventions, doc-first.** The documented investigation path (“alert → traceId → error InstanceId → catalog entry”) presupposes tracing, yet no attribute conventions exist (`firstclasserrors.code`, `.instance_id`, `.transience`, …) — consumers will reproduce at the telemetry layer the vocabulary drift the library forbids at the context-key layer. Publish the convention page first; a minimal `Activity` helper only if demand confirms.
* **Transience-driven retry recipe (docs-only).** `Transience` exists to answer “is retrying meaningful?” and no page shows wiring it to a retry policy (e.g. a Polly predicate on `InfrastructureError { Transience: Transient }`).
* **A serialize-only JSON contract for runtime errors** (aligned with the log model; rehydration explicitly out of scope — deserialization would bypass the validating constructors, violating the CLAUDE.md doctrine; a *received-error* representation is the correct future design if cross-service propagation is ever wanted — recommend an ADR before any code).
* **JSON Schemas for the JSON catalog and baseline** (the “stable technical pivot” claim is currently untestable by integrators; pairs naturally with `CatalogSnapshot.CurrentSchema`).
* **AOT/trimming support statement for the binder** (the interpreter/reflection fallback chain already exists in code; promote it to a user-facing compatibility note; fold the source generator into ADR-0023's recorded successor path — do not accelerate it).
* **gRPC mapping table (docs-only)** — CoreConcepts names gRPC as a transport; HTTP got a mapping table, gRPC got nothing; `ErrorCode`+context map onto `google.rpc.ErrorInfo` almost verbatim. No gRPC package is justified by current evidence.
* **Two structural analyzers for documented-but-unenforced checklist rules:** inline error construction outside `[ProvidesErrorsFor]` types, and interpolated strings passed as public messages (the anti-pattern `WritingErrorMessages` prints verbatim). Opt-in Info, like their FCE003–005 siblings.
* **Doc-hygiene automation** (link checker incl. the hard-coded IDE help-link URLs; EN/FR pair+freshness check; workflow-doc coverage check; doc-hub inventory check) — §11's systemic fix, all in the existing `tools/` pattern.
* **`fce config init` as a real bootstrap** (detect the `.sln`, seed `serviceName`/baseline) and **snippet-compilation verification** for the API-heavy guides.
* **A core `Outcome`/`Error` benchmark project** (mirroring the binder's exemplary harness) plus a performance/allocation row in the comparison table. The class-not-struct policy rests on the empirical claim that error paths are not hot loops; measuring the actual per-call cost turns that claim from assertion into evidence, arms the comparison against struct-based ErrorOr, and lets `WhenNotToUse`'s hot-loop section name the *actual* cost (allocations, not exception creation).
* **Community-health completion:** CODE_OF_CONDUCT, issue templates, a SUPPORT pointer, and — most valuable for a bus-factor-1, AI-assisted project — a short continuity/governance note plus a user-facing sentence about the AI-assisted development model the repo already governs so carefully internally.

### 13.5 Deliberate strengths to leave alone

The framework-agnostic, zero-dependency core; the ambient (DI-free) composition model; expression-tree selectors for v1 (ADR-0023's measured deferral); the class-not-struct rule; the `Then`/`Recover`/`Finally` vocabulary. Each is ADR- or CLAUDE.md-governed with sound rationale; every gap above is fillable *without* touching them.

### 13.6 Out of scope — attractive, and correctly rejected

| Idea | Why it should stay out |
|---|---|
| Map/Bind/Match/LINQ combinator aliases | Explicitly rejected (ADR-0003, FAQ): intent-naming *is* the API's identity; aliases would reintroduce the duality the decision removed. |
| Struct results / implicit `T`→`Outcome` conversions | Prohibited by the documented class-invariant doctrine; ErrorOr's headline ergonomic would erode the explicit Success/Failure vocabulary the staged design is built on. Error paths are not hot loops. |
| Source-generated factories/documentation | Would remove the “construction and documentation reviewed together” property Principle 4 depends on. (Distinct from the binder-selector source generator, which ADR-0023 correctly records as a possible post-v1 successor.) |
| First-party Confluence/Backstage/static-site publishers | Contradicts the documented position (“publishing is left to your CI/CD”); the JSON/Markdown pivots + CI recipes + renderer plugin contract are the intended integration surface. Document recipes instead. |
| HTML theme packs, VS Code extension, `dotnet new` templates, SARIF export, AI-assisted doc generation | Fashion or redundant: the renderer already has light/dark+i18n; code fixes deliver scaffolding inside the toolchain; Roslyn diagnostics already flow to CI natively; generated prose would undermine “documentation is reviewed knowledge.” |
| A DI abstraction in the core | The core needs no container; adding `M.E.DependencyInjection.Abstractions` would break the zero-dependency stance for no modeled benefit. The ASP.NET adapter bridges the one composition seam. |
| Auto-discovery of referenced packages' catalogs | Already decided against with an airtight argument (ADR-0019): a referenced binary can only expose its defaults; the consumer's catalog is the only faithful documentation site. |
## 14. Recommended Improvements

This is the consolidated, classified backlog. Each item names the problem, the fix, and where the full discussion lives. “Critical” is used sparingly, per the board's definition: *issues that should be addressed before future evolution*.

### Critical

| # | Recommendation | Why critical | Where |
|---|---|---|---|
| C1 | **Backfill ~6 foundational ADRs** (Outcome/exception duality; netstandard2.0 target; class-not-struct rule; FCE ID scheme + analyzer bundling; GenDoc reflection extraction; bilingual policy) as `Proposed`, each stating it records a pre-practice decision | ADR-0004 makes the ADR base the per-PR checkpoint; unrecorded foundations are unenforceable at that checkpoint. Mostly relocation of existing rationale, not invention | §6.5 |
| C2 | **Close the publication-sequencing window:** publish (or at minimum claim) the `Dummies` ID; at the first lib release after, replace the embedded `Dummies.dll` in Testing with a real `PackageReference` | Both risks are open *only* until first publication; every guard needed already exists | §10.3 |
| C3 | **Fix the storefronts:** core README analyzer count (16→18); CLI README command tree (`fce config renderer …`), `config show` wording, add the `catalog` commands; and fix the repository README's install section, which instructs installing `FirstClassErrors.RequestBinder` and the `fce` tool — neither on NuGet (mark “coming with the first lib/cli release” or publish them) | Minutes of work; first-contact surfaces actively misleading every evaluator today, including two dead install commands | §12.1 |
| C4 | **Fix the four confirmed doc/API drifts:** `DeterministicTesting` EN+FR stale seeding sentence; FR CONTRIBUTING missing `binder`/`dummies` scope rows; 20 dead FR footer anchors; `CONTRIBUTING.md`'s “scope carries no versioning weight” vs the release tooling that drops unscoped commits | Each is a place where following the docs produces wrong results; the CONTRIBUTING one can silently erase user-facing changes from the release record | §11.4, §11.5 |
| C5 | **Ship the Error→ProblemDetails projection** (neutral model in core reusing the GenDoc `ProblemType` URN builder; `FirstClassErrors.AspNetCore` satellite for `IResult`/status policy/exception handler) | The highest-leverage competitive gap; closes the catalog-runtime drift loop the thesis demands | §13.2 |
| C6 | **Add FCE019: discarded `Outcome`** (warning, on by default) | The library's central invariant — an error must not be silently lost — is currently unenforced | §12.3, §13.3(d) |

### High Value

1. **Link the sample projects from README/GettingStarted/DocumentationMap** and reconcile both doc hubs (add the six missing guides each) — the project's best teaching assets are undiscoverable (§11.2).
2. **`Error.ToLogModel()`** — ship the canonical log projection; define its field names as the stable runtime schema (§13.3a).
3. **`Outcome.Try(action, ex => FactoryError(ex))`** — boundary exception capture that *requires* a documented factory error (§13.3b).
4. **Code-fix providers** for FCE009 (DescribeError scaffolding — the flagship), FCE004/006/008/016 (§13.3c).
5. **`fce lint`** — content-quality rules on the extracted, post-localization catalog (§13.3e).
6. **Harden the analyzer test harness** (fail on compiler errors in snippets; assert locations/message args on primary positives) and add an ErrorContextKey name/type-conflict analyzer (the FCE001 pattern applied to a documented runtime crash) plus a descriptor↔documentation consistency test (§8.2-Analyzers).
7. **Doc-hygiene automation in the `tools/` pattern:** EN/FR pair+freshness check, relative link/anchor checker (also guarding the hard-coded IDE help-link URLs), workflow-doc coverage check, doc-hub inventory check (§11.4, §11.6).
8. **Codify the real bilingual policy** in CLAUDE.md/AGENTS.md/PR template (English canonical; every `doc/handwritten/` file mirrored; FR updated in the same PR) (§11.4).
9. **Close the three workflow-reference gaps** (canary, dummies, gendoc-docs pages) and refresh ci/release-dryrun pages (§11.5).
10. **API symmetry completions:** `Outcome.Then<TResult>(Func<TResult>)`; single-inner-error `Create` on port errors; decide and declare the taxonomy extensibility contract (close it or open `PublicMessageStage`) (§9.3).
11. **Remove the per-request lock/write from `RequestBinderOptions.Default`** (double-checked volatile fast path) and guard terminal reuse loudly (§8.2-Binder).
12. **Ship the binder's host-integration companion** (serializer-aware `IArgumentNameProvider` reading System.Text.Json naming policy; `HttpRequest` extraction helpers) — folds naturally into the C5 AspNetCore package (§12.6).
13. **Make the analyzers reach binder-only consumers** (bundle into the binder package, or document the companion-reference requirement) (§10.3).
14. **Wire the documentation-contract version check** the attribute's XML docs already promise, or retract the claim (§8.2-GenDoc).
15. **Add stale-output pruning to `fce generate`** (or document the `rm -rf` requirement for adopters) (§12.4).
16. **Widen the comparison page** with the exceptions+ProblemDetails baseline and the union/functional families (§13.3f).
17. **Close the reproducibility adoption gap:** build the `[ReproducibleFact]`-style xUnit adapter (or wrap the arbitrary-value-heavy suites in `Reproducibly`); give Dummies a landing section in the README and a user guide before `dum-v0.1.0` (§12.5, §10.3).
18. **Have the maintainer resolve ADR-0025's status** (implementation is merged and consumed; the record is still Proposed) (§6.4.5).
19. **Complete the community-health surface:** CODE_OF_CONDUCT, issue templates, SUPPORT pointer, a short continuity/governance note (who can act if the sole maintainer cannot), a user-facing sentence on the AI-assisted development model, and reconcile the README's open-issues invitation with the repository's issue settings (§5.6).
20. **Reconcile the release record with the shipped preview:** backfill the three changelogs with `0.1.0-preview.1`, publish (or supersede) the draft GitHub Release under the train-prefixed scheme, and add an `[Unreleased]`-promotion step to `release.yml` so the Keep-a-Changelog claim becomes true at the next cut (§5.1.3).
21. **Harden `code_review.md`** with the treat-analyzed-content-as-data rule its sibling prompts already carry, and document what binds/consumes it (§5.6).

### Medium Value

Draft the lockstep/IVT ADR (§10.3); domain-side `Outcome.Combine` gated on a documented envelope factory (§13.4); OTel semantic-convention page (doc-first) and Transience/Polly recipe (§13.4); serialize-only JSON error schema + JSON Schemas for catalog/baseline (§13.4); AOT/trimming statement for the binder (§13.4); gRPC mapping table (§13.4); unify the CLI's duplicated resolvers and extract the shared RFC 9457 example-rendering helper (§8.3); give the CLI's config/renderer commands the ports pattern + tests, and add a `CommandAppTester` argv tier (§8.2-CLI); extend `ErrorAssertion` to the guide's own recommended assertions (§12.5); context-key namespacing guidance + one naming convention across all docs (§9.3c); embed `[NotNullWhen]` flow attributes (§9.3d); document the exception→Outcome bridge idiom (§9.3e); out-of-DTO argument sample + reconcile the RequestBinder guide's coverage claim (§11.3); reconcile sample `Amount` with `UsagePatterns` (§11.3); dogfood `FirstClassErrors.Testing` in the sample tests (§11.3); a transport-mapping example (Outcome → problem response record) (§12.6); two structural analyzers (inline construction; interpolated public messages) (§13.4); route `github.head_ref` through env vars and document the gendoc-docs stranded-checks effect (§11.5); ADR housekeeping batch (enumerate ADR-0024's affected set; sharpen ADR-0017's decision sentence; make ADR-0022's referenced docs true; index annotations for “refines”; dual dates on supersession; number-uniqueness CI guard) (§6.4); write down the test conventions and extend property coverage to context/inner-error invariants (§8.2-Tests); execute the first-release checklist (dry-run dispatch + tool-install smoke test) before the first real train tag (§11.5); add a core Outcome/Error benchmark project and a performance/allocation row to the comparison page (§13.4); add a sync check for the triplicated CLAUDE/AGENTS/code_review rule text and normalize `code_review.md`'s name/placement (§5.6); rename the ADR-0022 implementation-reference anchor and remaining ADR housekeeping as listed; a maintainer-facing supply-chain overview page consolidating the per-file policies (§11.5).

### Low Priority

Make `ErrorCode.Unspecified` public; chain the three `Error` constructors; fix the XML-doc drift items (documented-but-never-thrown exception, stale `To` reference); `PrimaryPortInnerErrors` params factory/`IEnumerable`; `BindingScope` default-construction message; cached empty list on absent optional lists; solution-taxonomy tidy (Documentation csproj build configs, samples out of the `tests` folder, one `InternalsVisibleTo` idiom); `fce config init` bootstrap; atomic baseline/snapshot writes; normalize/document the Spectre parse-error exit code; translate or exempt the ADR rulebook; broaden thin analyzer test matrices; label FsCheck conjunctions; GenDoc.Worker smoke seam; converge the duplicated `DocumentationFormatter`; sweep the stale-pointer constellation (changelog preamble, “English only” note, tag-format example, train allowlist quote, sample code-prefix inconsistency); publication recipes (TechDocs/MkDocs/Pages) as docs; OpenAPI renderer as a documented sample plugin; bring `WhenNotToUse` up to the house standard (and correct its hot-loop section to name allocation, not exception creation, as the Outcome path's cost); repair prev/next chain reciprocity; shrink the 919 KB packed `icon.png`.

### Out of Scope

See §13.6 — functional combinator aliases, struct results, implicit conversions, source-generated factories, first-party publishers, theme packs, IDE extensions/templates, SARIF, AI-generated docs, a core DI abstraction, and cross-package catalog auto-discovery are all attractive-looking and correctly rejected by the project's own recorded reasoning. This report recommends *keeping* those rejections.
## 15. Suggested Roadmap

Sequenced so that each phase de-risks the next, respecting the project's own constraint that breaking changes are cheap only until v1. Phases are scoped to be realistic for a single maintainer with agent assistance — the workflow this repository demonstrably runs well.

### Phase 0 — Hygiene sprint (days; do before anything else)

Everything here is minutes-to-hours each, and several items are actively misleading users today: the two nuget.org READMEs and the repository README's dead install commands (C3), the four confirmed doc drifts (C4), the FCE019 discarded-Outcome analyzer (C6, a day including tests and doc pages), ADR-0025's status resolution, the doc-hub/sample links, the `--service-name` example fix, backfilling the changelogs with the shipped `0.1.0-preview.1`, and the community-health files (code of conduct, issue templates, a continuity note). Ending this sprint with the three advisory checks (FR sync, link checker, workflow-doc coverage) converts this whole class of drift from recurring manual cleanup into red checks — the project's own medicine, applied to its last hand-maintained surfaces.

### Phase 1 — Foundation closure (1–2 weeks)

* **C1: backfill the foundational ADRs** (plus the lockstep/IVT ADR). This is the governance loop's missing half and gets cheaper never.
* **C2: publish Dummies** (claiming the ID), then swap the embedded DLL for a `PackageReference` at the next lib pack; give Dummies its landing docs.
* Analyzer trust hardening: harness compile-assertion, the ErrorContextKey-conflict rule, the descriptor↔docs consistency test.
* Pre-v1 API decisions while breaking is free: the symmetry completions, the extensibility-contract decision, the `Default` getter fast path, terminal-reuse guard, `[NotNullWhen]` attributes. Each is small; together they retire every §9.3 friction item.

### Phase 2 — The last mile (2–4 weeks; the strategic phase)

* **C5: the ProblemDetails story** — `ProblemType` extraction into the core, `error.ToProblemDetails(...)`, then the `FirstClassErrors.AspNetCore` package (Outcome→`IResult` terminals, taxonomy status policy, exception handler, `AddFirstClassErrors(...)` bridging the binder options), folding in the serializer-aware name provider (the binder's biggest first-run friction).
* `Error.ToLogModel()` + the documented runtime field schema; `Outcome.Try` with mandatory factory mapping.
* Extend the samples to consumption (an async `Recover` flow, `InfrastructureError`, Testing-package dogfooding, the out-of-DTO binding, and a transport-mapping example) — the AspNetCore package makes these examples natural rather than contrived.
* Widen the comparison page. After this phase, the answer to “why not ErrorOr?” is complete on ErrorOr's home turf.

### Phase 3 — First releases (when Phases 1–2 land)

Execute the already-written first-release checklist (manual dispatch dry run validating trusted publishing; the tool-install smoke test before the first `cli-v` tag). Ship `lib-v0.x`, `cli-v0.x`, `dum-v0.x` — the first releases through the *current* pipeline (the existing `0.1.0-preview.1` shipped under the superseded pre-train process, so these tags are what actually prove the machinery: trusted publishing, attestation, the catalog gate, and changelog promotion). Resolve the draft `v0.1.0-preview.1` GitHub Release into the train scheme while doing so. Nothing in this report blocks releasing earlier if the maintainer prefers — but releasing after Phase 2 means the first impression includes the HTTP story, and the changelog machinery gets exercised with real content while the CONTRIBUTING scope rules are already corrected.

### Phase 4 — Reference-grade tooling (ongoing, demand-driven)

Code-fix providers (FCE009 scaffolding first); `fce lint`; JSON Schemas; the OTel convention page and retry recipes; the reproducibility adapter; the two structural analyzers; CLI seam/test completion and the argv test tier; the GenDoc process-runner seam; snippet-compilation verification. Each item is independent — good agent-sized work parcels — and none blocks the others.

### Standing guardrails

Re-evaluate ADR-0011's Dummies-extraction triggers at v1.0; plan ADR-0002's supersession before .NET 8 LTS ends (November 2026); re-run the binder benchmarks if the selector surface changes (ADR-0023's own trigger); and keep §13.6's rejections rejected.
## 16. Conclusion

This audit set out to answer whether FirstClassErrors is a coherent, professional, maintainable open-source project that could reasonably become a reference in its domain. After a full build-and-test run, thirteen subsystem surveys, individual audits of all 26 ADRs with implementation-compliance verification, competitive and integration gap analyses, and adversarial verification of every significant claim, the answer is a confident yes on the first three counts and a well-founded conditional on the fourth.

What distinguishes this repository is not any single mechanism but a consistently applied stance: **every claim the project makes about itself is either enforced by a machine or honestly labeled as unenforced.** The design principles are compiled into the type system. The documentation is generated, diffed, and release-gated. The package boundaries are architecture-tested and pack-asserted. The API decisions are recorded with alternatives and locked with tests. When the audit went looking for the gap between what the project says and what it is — the standard failure mode of ambitious open-source — it found the gap almost entirely confined to hand-written *storefronts and hubs*, the few surfaces the machine does not yet check. That inversion (drift in the meta-text, fidelity in the system) is rare, and it is the strongest single signal that the reference ambition is credible.

The path from here is unusually legible because the project's own instruments define it. The foundational decisions need their ADRs so the governance loop can protect them. The Dummies sequencing window needs closing by executing an already-made decision. The catalog's HTTP promise needs the runtime projection that makes it true. The discarded-`Outcome` blind spot needs the one analyzer the thesis most obviously demands. And the last hand-maintained surfaces need the same advisory checks the project already builds for everything else. None of this is rework; all of it is completion.

Two closing observations for the maintainer. First, the restraint documented throughout — the deferred features, the non-ranking comparison, the “when not to use this” page, the out-of-scope rejections this report recommends preserving — is a competitive asset; reference libraries are defined as much by what they refuse as by what they ship. Second, the repository's most exportable achievement may not be the error model at all, but the demonstration of how a solo-maintained, agent-assisted project can hold industrial-grade engineering discipline: ADR-governed decisions, positive-proof CI, packed-artifact dogfooding, and bilingual living documentation, all at once. That system is what will carry FirstClassErrors through the releases, integrations, and contributors ahead of it.

---

*Audit conducted 2026-07-20 against revision `3bf89e3` by an orchestrated multi-agent review (13 subsystem surveys, 26 per-ADR audits, corpus review, 3 gap analyses, build/test execution, adversarial verification of all critical/high findings, and a completeness-critic pass), synthesized and edited into this report. Every finding cites repository evidence; verification verdicts are reflected in the classifications.*
