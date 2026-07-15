<!--
  Please write this PR in ENGLISH: title, summary, changes, testing notes, and related issue references.

  Title: name the whole change in English. A single-intention PR mirrors its commit
  header (type(scope): description); a multi-intention PR uses a short descriptive
  title. Issue links go in "Related issues" below, not the title.
  See CONTRIBUTING.md -> "Pull request titles".

  Fill in the applicable sections below.
  Do not invent information.
  Only check testing items that were actually run.
  Delete a section only if it truly does not apply.
-->

## Summary

<!-- One or two sentences: what does this PR change, and why? -->

## Type of change

* [ ] Bug fix
* [ ] New feature
* [ ] Breaking change
* [ ] Refactoring
* [ ] Analyzer / diagnostic change
* [ ] Tests
* [ ] Documentation
* [ ] Build / CI / tooling

## Changes

<!-- Bullet list of the concrete changes made in this PR. Keep it factual. -->

*

## Testing

<!-- Check only the commands/tests that were actually run. Add details if something was not run. -->

* [ ] `dotnet build FirstClassErrors.sln`
* [ ] `dotnet test FirstClassErrors.sln`
* [ ] Analyzer tests pass (`FirstClassErrors.Analyzers.UnitTests`)

## Documentation

<!-- State whether documentation was updated, or why no documentation change was needed. -->

* [ ] Public API / error documentation updated
* [ ] README / `doc/` updated
* [ ] French translation (`doc/README.fr.md`) updated if user-facing behavior changed
* [ ] No documentation change required

## Architecture decisions

<!-- Every pull request is checked against the ADR base (maintainers/adr/). Most
     embark no architectural decision — tick the first box. Agents draft ADRs as
     `Proposed`; the maintainer accepts or supersedes. See maintainers/adr/README.md. -->

* [ ] No architectural decision in this pull request
* [ ] New decision recorded — ADR drafted as `Proposed`: ADR-____
* [ ] Supersedes an existing ADR — successor proposed, status not flipped: ADR-____
* [ ] ⚠️ Conflicts with an existing ADR — flagged for the maintainer: ADR-____

## Related issues

<!-- e.g. Closes #123 -->
