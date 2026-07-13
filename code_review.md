# Pull request review guidelines

All pull request review comments must follow the Conventional Comments format
described below. These instructions apply to automated Codex reviews.

## Language

* Every review comment is written in English: label, decorations, subject and discussion.
* Code identifiers, API names, exception messages and other technical terms are
  quoted verbatim in their original form.
* Use a direct, factual and professional tone.
* Do not use emojis.

## Comment format

Every inline review comment must use this format:

```text
<label> [(decorations)]: <subject>

[discussion]
```

The subject must summarize the finding in one sentence.
The optional discussion must explain:

1. why the code is problematic;
2. the concrete consequence;
3. how the problem can be corrected, or at least a useful direction.

Example:

```text
issue: This `Outcome` is discarded, so the failure it carries is silently lost.

`Validate(order)` returns an `Outcome`, but the result is never inspected. When
validation fails the caller keeps going as if it had succeeded, and the error is
dropped instead of propagated.

Capture the result and short-circuit on `IsFailure` (or chain with `Then`) so the
error reaches the caller.
```

## General rules

* Use exactly one label per comment.
* Report exactly one independent finding per comment.
* Use no more than two decorations.
* Do not repeat the same finding on several lines of the diff.
* Place the comment on the smallest relevant code range.
* Do not write an introduction or conclusion around the formatted comment.
* Do not include a severity or priority prefix such as `P0`, `P1`, `P2`, `P3`,
  `critical`, `major` or `minor` anywhere in the comment. Blocking status is
  expressed only through the label and the `(blocking)` / `(non-blocking)` decoration.
* Do not comment on code that is outside the pull request unless the changed code
  introduces or exposes the problem.
* Do not comment on purely subjective preferences as though they were defects.
* Do not invent a finding merely to produce a more complete-looking review.
* If there are no relevant findings, approve the review without manufacturing comments.

## Labels

### `praise:`

Use to highlight something specifically and genuinely successful.
A praise comment must explain what is good and why it is worth preserving.
Do not generate generic or complimentary filler.

```text
praise: The test names state the exact error condition they pin down.

`Create_Throws_When_Code_Is_Whitespace` reads as the specification of
`ErrorCode.Create`, so the guarded invariant is clear without opening the test body.
```

### `note:`

Use to provide relevant information when no change or answer is expected.
Do not use `note:` for a hidden request.

### `thought:`

Use for an architectural or design observation outside the scope of the pull request.
The comment must explicitly state that no change is required in the current pull request.

```text
thought (archi): The Error-to-HTTP-status mapping is now duplicated across several endpoints.

Nothing to change in this pull request. Centralizing the mapping could be a separate
architecture decision if a third endpoint needs it.
```

### `question:`

Use when the code appears suspicious but the available evidence is insufficient
to assert that it is wrong.
A question expects an explanation, not necessarily a code change.
Do not disguise an assertion as a question.

```text
question: Is materializing `context.Values` with `.ToList()` before the `.Where()` intentional?

It appears to copy every context entry before filtering on the key, which does more work
than filtering first. If the goal is to snapshot the entries before a later mutation that
is fine; otherwise the `.ToList()` can move after the filter.
```

When the defect is certain, use `issue:` instead.

### `nitpick:`

Use only for a clearly subjective and optional preference.
The author must be free to ignore it.
Do not use `nitpick:` for correctness, security, performance, compatibility,
maintainability or architectural problems.
Automated reviews should rarely produce nitpicks.

### `suggestion:`

Use to propose a concrete, non-mandatory improvement.
State what should change and why the proposed form is better.
Do not use `suggestion:` when the current implementation is actually incorrect.
Use `issue:` instead.

```text
suggestion (test, if-minor): Add the case where the error context is empty.

`ErrorContext.Empty` is a distinct boundary from a populated context. Add it here only
if it does not require rebuilding the fixture.
```

### `chore:`

Use for a mandatory process step that must happen before merge, such as:

* updating a changelog;
* regenerating committed artifacts (for example the generated error catalog);
* updating version metadata;
* running a required repository-specific operation.

When possible, name the relevant command, file or documented procedure.
`chore:` is blocking by default.

### `todo:`

Use for a mandatory change that is small, obvious, local and not reasonably open
to debate.

```text
todo: Fix `occured` to `occurred` in the `NotFoundError` message.
```

Do not use `todo:` for a substantial defect or design problem. Use `issue:` instead.
`todo:` is blocking by default.

### `issue:`

Use for a confirmed defect that must be addressed.
Typical issues include:

* incorrect behavior;
* a regression;
* data loss or corruption;
* an unhandled failure;
* a security vulnerability;
* an invalid public API;
* broken compatibility;
* a race condition;
* a meaningful performance regression;
* a missing test that leaves a demonstrated regression unprotected;
* a violation of an explicit repository invariant.

An issue must include:

1. the problematic behavior;
2. the circumstances under which it occurs;
3. its concrete impact;
4. a correction or a useful direction toward one.

Do not write vague comments such as:

```text
issue: This code is badly designed.
```

When the problem cannot be asserted confidently, use `question:`.
`issue:` is blocking by default.

## Decorations

Decorations refine the label.
Use a decoration only when it adds information not already implied by the label.
Allowed decorations are:

* `(blocking)` — must be resolved before merge;
* `(non-blocking)` — must not prevent merge;
* `(if-minor)` — perform only if the change remains trivial;
* `(security)` — concerns security;
* `(perf)` — concerns performance;
* `(test)` — concerns test coverage or test quality;
* `(archi)` — concerns architecture beyond the immediate scope of the pull request.

Use one decoration in normal cases and never more than two.

Correct:

```text
issue (security): The API key is copied into the error context and surfaces in the logs.
```

Correct:

```text
suggestion (test, if-minor): Add a case for the empty error context.
```

Incorrect:

```text
suggestion (test, perf, non-blocking, if-minor): ...
```

## Blocking rules

The following labels are blocking by default:

* `issue:`
* `todo:`
* `chore:`

The following labels are non-blocking by default:

* `praise:`
* `note:`
* `thought:`
* `question:`
* `nitpick:`
* `suggestion:`

An explicit blocking decoration overrides the default:

```text
suggestion (blocking): ...
```

An explicit non-blocking decoration also overrides the default:

```text
issue (non-blocking): ...
```

Do not add a decoration that merely repeats the default.

Avoid:

```text
issue (blocking): ...
nitpick (non-blocking): ...
```

Prefer:

```text
issue: ...
nitpick: ...
```

Use an explicit override only when the finding genuinely differs from the default behavior.

## Selecting findings

Prioritize findings according to their actual impact:

1. correctness;
2. security;
3. data integrity;
4. regressions;
5. public API and compatibility;
6. concurrency and reliability;
7. significant performance problems;
8. missing tests for demonstrated risks;
9. violations of explicit repository rules (for example a value object declared
   as a `struct` instead of a `class`).

Do not report:

* formatting already enforced by an automated formatter;
* problems already flagged by an `FCExxx` analyzer;
* naming violations already enforced by repository tooling;
* speculative problems without a plausible execution path;
* broad refactoring opportunities unrelated to the pull request;
* personal style preferences presented as requirements;
* pre-existing issues not materially affected by the pull request.

When a repeated style problem should be automated by `.editorconfig`, a formatter
or an analyzer, do not keep adding review comments for each occurrence.

## Writing actionable comments

A comment must be understandable without requiring the author to infer the
reviewer's intent.

Prefer:

```text
issue: This property exposes the mutable context dictionary in the public API.

`BuildContext` returns the backing `Dictionary<ErrorContextKey, object?>` directly, so any
caller can mutate an error's context after the error was created, breaking the immutability
`ErrorContext` guarantees.

Return `IReadOnlyDictionary<ErrorContextKey, object?>` (as `ErrorContext.Values` does), or
wrap the dictionary in a `ReadOnlyDictionary` before returning it.
```

Avoid:

```text
issue: This method should not be public.
```

Do not overstate consequences.
Use conditional language only when the execution path is genuinely conditional:

```text
issue: When two threads initialize the error catalog concurrently, each can build and cache its own instance.

The lazy initialization checks `_catalog is null` and assigns without synchronization, so
concurrent callers each run the factory. Whichever assignment lands last wins, while callers
that already read an earlier instance keep using it.

Guard the initialization with `Lazy<T>` or a lock so the catalog is built exactly once.
```

Do not use vague formulations such as:

* "maybe";
* "I'm not a fan";
* "in my opinion";
* "this seems weird";
* "could you take a look?".

When uncertainty is material, use a precise `question:` instead.

## Relationship between an issue and its correction

A confirmed problem and its proposed correction normally belong in the same comment.
Do not create separate comments such as:

```text
issue: The documentation is written to several files without rollback.
```

and:

```text
suggestion: Write to a temporary directory first.
```

Prefer one complete comment:

```text
issue: A failed documentation run can leave a partially written catalog on disk.

GenDoc writes each error page in sequence, overwriting the previous output in place. If a
write fails midway, the already-written pages remain while the rest stay stale, so the
published catalog is internally inconsistent.

Write the full set to a temporary directory and swap it in only once every page succeeded,
or delete the output on failure so the run is all-or-nothing.
```

A separate `suggestion:` is appropriate only when it describes an independent,
optional improvement.

## Tests

Do not request a test merely because a changed method has no dedicated test.
Request a test when it protects a meaningful behavior, boundary or regression.

Use:

```text
issue (test): No test covers `ErrorCode.Create` rejecting a whitespace code.

This change reworks the validation in `ErrorCode.Create`, but no test exercises the
whitespace case. `Create` is contractually required to throw `ArgumentException` on null,
empty or whitespace input; without a test the guard can be removed again without anything
failing.

Add a test that calls `ErrorCode.Create(" ")` and asserts an `ArgumentException`.
```

Use `suggestion (test):` when the additional coverage is beneficial but not
required to make the pull request safe.

## Final review summary

The final review summary must be concise.
When findings were reported, summarize only:

* the number of blocking findings;
* the number of non-blocking findings;
* the principal risk areas.

Do not duplicate every inline comment in the summary.
When no finding was reported, state clearly that no blocking issue was found.
The summary itself is not an inline Conventional Comment and therefore does not
require a label.
