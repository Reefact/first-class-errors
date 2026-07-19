# ADR-0021 | Bind out-of-DTO arguments as peers through a source-agnostic untyped entry

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.fr.md)

**Status:** Proposed
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

* The binder built a command from a single request DTO. Its entry point was
  DTO-first — a binder was *started over* a DTO, and the failure envelope was
  declared on the DTO as a second step. There was no seam for an input that does
  not live in the DTO.
* Real primary adapters routinely assemble a command from more than the body: a
  route identifier, a query parameter, a request header, a claim. A host has
  already extracted these individual values; they need the same collect-all,
  coded, path-carrying binding the DTO's properties get, into the **same**
  envelope — so a bad route segment and a bad body field are reported together.
* The binder is framework-agnostic (HTTP controllers, message consumers, CLIs,
  gRPC handlers). Extracting a value from an incoming HTTP request is host
  knowledge; the library sees already-extracted values.
* A DTO property's error path is derived by reflection from the C# property name
  (through the configured `IArgumentNameProvider`); an out-of-DTO value has no
  property to reflect over, so its path must be stated by the caller.
* A DTO property's provenance is uniform and implicit — every property comes from
  the one request body. An out-of-DTO value's provenance is not: "route", "query"
  and "header" are distinct origins the caller had to state, and which are useful
  in diagnostics to tell one failing input from another.
* Naming the command type at the entry point and inferring it everywhere else and
  keeping a single binder type cannot all three hold at once: with the command
  type fixed on a generic entry, a nested complex binding puts the nested type in
  a delegate *parameter* position, where C# method-group inference cannot recover
  it — forcing an explicit type argument at every nested call site.
* A complex DTO property is, by construction, a path into a DTO; an out-of-DTO
  argument has no DTO to path into.
* Errors carry a typed context; a public accessor key lets a consumer read a
  context entry without depending on its internal name.
* The library is pre-release, unpublished on NuGet with no external consumers, so
  the entry-point surface can still change without a migration.
* ADR-0007 named the build terminals `New` / `Create`; ADR-0012 fixed a binder's
  options at its entry point; ADR-0017 made the application-wide options default
  settable. All three describe the entry point in its previous DTO-first shape.

## Decision

The binder is source-agnostic: its untyped entry point declares the failure
envelope up front and attaches inputs as peers — a DTO through a property source,
and individually named out-of-DTO arguments (each stating its provenance) through
an argument source — with the command type named only at the `New` / `Create`
terminal, and with no complex out-of-DTO argument.

## Rationale

* Making the entry declare the envelope and attach the DTO and the arguments as
  peers is what lets a route/query/header value bind into the *same* envelope,
  with the *same* paths and the *same* two structural codes, as a body property —
  which is the requirement. A DTO-first entry has no place to put an input that is
  not in the DTO; a peer model does.
* The entry is untyped because the source-agnostic model removes the reason to
  type it. The command is no longer "built over a DTO"; it is assembled at the
  terminal from peers. Naming the command type at the terminal resolves the
  three-way tension in Context: the terminal infers it from the assembler, so it
  is named nowhere else, one binder type serves top-level and nested binding
  alike, and — because the nested type now appears only in the nested binding's
  *return* position — method-group inference recovers it with no explicit type
  argument. Intention is still expressed, by the envelope factory's name and the
  command's own type, not by a redundant type parameter on the entry.
* An out-of-DTO argument states its own name because there is no property to
  derive it from; the name is used verbatim as the path, so the caller controls
  the wire key directly, exactly where a DTO property defers to the name provider.
* Capturing an argument's provenance, and *only* an argument's, matches where the
  information exists and is worth keeping: an argument's origin was stated by the
  caller and distinguishes otherwise-similar failures, whereas a DTO property's
  origin is the single implicit body and would be noise on every property. The
  asymmetry is deliberate, not an omission.
* Reusing the existing converter surface (`AsRequired`, `AsOptional`, the value
  and reference optionals, and the list form) for arguments keeps one mental model
  for every input: an argument differs from a property only in how it is named and
  sourced, never in how it is bound.
* Omitting a complex out-of-DTO argument is a consequence of what the two concepts
  are, not a feature cut: a complex property dereferences a DTO path, and an
  argument has no DTO to dereference. A complex value assembled from several
  arguments is expressed by binding each as a peer and combining them in the
  terminal — no new concept required.
* The pre-release status means the entry-point shape is settled now, when there
  are no consumers to migrate.

## Alternatives Considered

### Keep the DTO-first entry and treat an out-of-DTO value as a synthetic one-property DTO

Considered because it reuses the existing property path unchanged.

Rejected because it forces the caller to wrap each loose value in a throwaway DTO
purely to satisfy the entry shape, and the reflection-derived path then reports
that wrapper's property name rather than the caller's intended wire key — the
inverse of what an out-of-DTO argument needs.

### Type the entry point on the command (a generic `Bind.To<TCommand>`)

Considered because it states the target type at the top, reads as intention-first,
and keeps a single binder type.

Rejected because, with the command type fixed on the entry, a nested complex
binding puts the nested type in a delegate parameter position where method-group
inference cannot recover it, so every nested call site must spell out an explicit
type argument — a persistent papercut across the most common binder shape. The
untyped entry removes the papercut while still expressing intention through the
envelope and the command type.

### Add a complex out-of-DTO argument mirroring the complex property

Considered for symmetry with the DTO side, so arguments and properties would offer
the same shapes.

Rejected because it has no referent: a complex property binds a nested DTO reached
by a path, and an out-of-DTO argument has no DTO and no path. The symmetric-looking
API would name a thing that does not exist; the existing peers-plus-terminal
composition already covers "a complex value built from arguments".

### Encode provenance in the argument's error path (for example `route:bookingId`)

Considered because it needs no second context key.

Rejected because it conflates two distinct facts — *where* the input is (the path,
used to locate the field) and *what kind of origin* it has (the source, used to
classify the failure) — into one string that consumers would then have to parse,
the very message-parsing the binder exists to avoid. A separate, typed context key
keeps both facts first-class.

## Consequences

### Positive

* A command assembled from a body and any mix of route/query/header values binds
  in one pass, into one envelope, with one set of paths and codes.
* The nested-binding call site needs no explicit type argument; one binder type
  serves top-level and nested binding.
* An argument failure carries its provenance, so a consumer can classify failures
  (a bad route vs a bad header) without parsing messages.
* The converter surface is unchanged, so an argument is bound with exactly the
  verbs a property is.

### Negative

* The entry-point shape changes from the DTO-first form that ADR-0007, ADR-0012
  and ADR-0017 describe in prose; those ADRs' decisions are unaffected, but their
  illustrative surface is now historical.
* Two ways to attach an input (property source, argument source) are a slightly
  larger surface than one — accepted because they model two genuinely different
  provenances, not two flavours of the same thing.

### Risks

* A caller could reach for a non-existent "complex argument" and be briefly
  surprised by its absence; mitigated by documenting the peers-plus-terminal
  composition as the intended way to build a complex value from arguments.
* Free-form provenance labels could drift across a codebase ("route" vs "path");
  mitigated by the provenance-shortcut helpers (`FromRoute`, `FromQuery`, …) that
  fix the common labels, leaving the raw `From(source, …)` for the rest.

## Follow-up Actions

* Update the request-binder guide (EN + FR) and the package README to the
  source-agnostic entry and the out-of-DTO argument section.
* Consider a host-integration package that extracts values from an incoming HTTP
  request (rather than tagging already-extracted ones), if consumer demand
  appears.

## References

* ADR-0007 — name the binder terminals New and Create; the terminal that now also
  carries the command type parameter. Unchanged decision.
* ADR-0012 — fix the binder options before binding begins; the options are still
  fixed at the (now source-agnostic) entry point. Unchanged decision.
* ADR-0017 — provide a configurable application-wide default for the binder
  options; the default still backs the bare entry point. Unchanged decision.
* Issue #148 — the request this decision resolves.
* [`fluent-request-binder`](https://github.com/Reefact/fluent-request-binder) —
  the prior-art binder whose source/argument model informed this decision.
