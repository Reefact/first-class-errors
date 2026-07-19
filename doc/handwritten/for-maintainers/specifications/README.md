# Maintainer specifications

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](README.fr.md)

These pages describe the **current technical and operational contracts** that
implement architecture decisions. Unlike an accepted ADR, a specification is
expected to evolve when implementation mechanics change while the decision
remains valid.

Use the distinction deliberately:

* an [ADR](../adr/README.md) records **what was decided and why**;
* a specification records **how the accepted decisions are currently realised**;
* workflow references describe the exact GitHub Actions structure and permissions;
* user guides describe the public experience rather than maintainer mechanics.

When a specification change would alter the decision rather than merely its
implementation, write a new ADR before changing the contract.

## Index

| Specification | Decisions implemented |
|---|---|
| [Platform compatibility](platform-compatibility.en.md) | ADR-0001, ADR-0002, ADR-0022 |
| [ADR review process](adr-review-process.en.md) | ADR-0004 |
| [Request Binder contracts](request-binder.en.md) | ADR-0008, ADR-0012, ADR-0017, ADR-0018, ADR-0019, ADR-0021 |
| [GenDoc catalog contract](gendoc-catalog-contract.en.md) | ADR-0009, ADR-0010 |
| [Dummies generation contracts](dummies-generation.en.md) | ADR-0011, ADR-0013, ADR-0015, ADR-0020 |

## Maintenance rules

1. Keep the English page canonical and update the French counterpart in the same
   pull request.
2. Prefer links to source files and workflow references over copying long code or
   YAML fragments.
3. State observable contracts and the source of truth for each value.
4. Update an ADR only when a decision changes, except for the one-time editorial
   migration authorised by [ADR-0023](../adr/0023-extract-specifications-from-accepted-adrs.md).
