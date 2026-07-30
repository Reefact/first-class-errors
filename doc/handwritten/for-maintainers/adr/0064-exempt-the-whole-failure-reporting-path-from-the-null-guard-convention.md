# ADR-0064 | Exempt the whole failure-reporting path from the null-guard convention

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-07-30
**Decision Makers:** Reefact

Supersedes [ADR-0045](0045-guard-public-and-internal-arguments-against-null.md).

## Context

ADR-0045 requires every public and internal member to reject a `null` non-nullable reference
argument with `ArgumentNullException`, and enforces it with a reflection convention in
`JustDummies.UnitTests` that discovers members rather than naming them. It exempted exception types,
for a reason worth repeating: their constructors run while an error is being handled, so throwing
an `ArgumentNullException` there would replace the failure being reported with a failure about
reporting it, and the original would be lost.

That exemption is keyed on *being* an `Exception`. The hazard is not.

ADR-0063 made the library throw through factories named after the failure, and one of those
factories needed to say which of two constraints a conflict should blame. Five loose strings in an
order nothing checks was the wrong signature, so the pair — a constraint and what it claims — became
a small type, `ConstraintClaim`. It is built at the throw site, as an argument to the exception
factory:

```csharp
throw ConflictingAnyConstraintException.Contradicts(applying,
                                                    ConstraintClaim.Of(_exactConstraint!, $"already fixes the count at {V(exact)}"),
                                                    ConstraintClaim.Of(_minConstraint!,   $"already requires at least {Elements(_min)}"));
```

`ConstraintClaim` is not an `Exception`, so the convention inspected it and failed the build until it
guarded its arguments. Adding those guards satisfied the convention and recreated precisely what
ADR-0045 forbids, one call frame earlier: a `null` would now surface as `ArgumentNullException` from
a helper instead of as the conflict the code was reporting.

The convention was right that the rule as written applied. The rule as written was drawn one frame
too narrow.

## Decision

ADR-0045's rule stands in full, with its exemption widened from exception types to any type that
exists only to build one of the library's exceptions, declared by an internal marker the reflection
convention reads rather than inferred from usage.

## Rationale

**The hazard belongs to the path, not to the base type.** What makes a guard harmful there is *when*
it runs — while a failure is being reported — and that is a property of how the type is used, not of
what it derives from. A rule keyed on `: Exception` catches the common case and misses the rest, and
the rest is exactly what ADR-0063's factories create.

**A marker keeps the exemption honest.** The alternative is inference, and inference on this would
have to guess: "is only used by exception factories" is not a property a reflection test can
establish, and any approximation of it would either miss cases or silently exempt types that should
be guarded. A marker is one line, greppable, and wrong only if someone writes it wrongly — which a
reviewer can see.

**Nothing is actually given up.** Every call site on this path passes values the compiler has proven
non-null. The runtime guard was defending against a case the compiler already rejects, at the price
of masking real failures if it ever fired.

## Alternatives Considered

### Keep the guards on the helper types

What the code did before this ADR, and what the convention forced. It recreates the masking ADR-0045
exists to prevent, one frame away from where ADR-0045 forbids it. Rejected on the merits, not on
convenience: the guard is not merely redundant, it is harmful in the only circumstance it would ever
run.

### Infer the exemption from usage

"Exempt a type all of whose callers are exception factories" sounds principled and is not
implementable from reflection metadata, which sees signatures rather than call graphs. Any proxy —
naming, namespace, assignability — would be a guess, and a guess that silently removes a guard is
worse than no rule.

### Make the helper type private to the exception

A nested private type is already out of the convention's scope, so no ADR would have been needed.
But then the throw site cannot build one, and the factory is back to five loose strings in an order
nothing checks — the signature ADR-0063 rejected. The exemption exists so the call site can stay
readable.

### Fold the pair into the message at the call site

Composing the sentence at the throw site removes the type and the question with it, and reinstates
the prose-in-business-code ADR-0063 was written to end. Rejected there, rejected here.

## Consequences

### Positive

* The hazard ADR-0045 identified is now covered wherever it occurs, instead of wherever a type
  happens to derive from `Exception`.
* A throw site can build the arguments that make its message readable without the helper type
  reintroducing the masking one call frame earlier.
* The exemption is greppable. One marker, one reason written on it, and a reviewer can see every
  type that claims it.

### Negative

* The exemption is now something a contributor can apply, where before it followed from the type
  system alone. It costs a decision at the point of writing the type.
* The guard requirement, which held with no exception outside exception types, now holds with one
  more — a rule with two exemptions is marginally harder to state than a rule with one.

### Risks

* A marker on a type that is *not* confined to the failure path would silently drop a real guard
  requirement, and no test can catch that — the marker is trusted by construction. Mitigations: it is
  `internal`, it applies to classes and structs only, and its reason is written on the attribute so a
  reviewer meets the argument before the usage.
* The compiler now carries what the guard carried. That is stronger for internal callers, but it does
  mean a reflective or `null!`-defeated caller would reach the constructor unchecked — an accepted
  trade, since such a caller has already left the contract.

## References

* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.md) — the rule this supersedes
  and restates, and the exemption this widens.
* [ADR-0063](0063-throw-the-library-s-own-exceptions-through-named-factories.md) — the named
  factories whose value objects made the narrow exemption insufficient.
