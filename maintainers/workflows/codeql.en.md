# `codeql` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](codeql.fr.md)

> Maintainer documentation — part of the [workflow reference](README.en.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/codeql.yml`](../../.github/workflows/codeql.yml)

## What it is for

`codeql` runs GitHub's CodeQL static analysis over the C# code and uploads the
findings to the repository's **code-scanning dashboard** (the Security tab). It
is the semantic security scanner: it looks for vulnerability patterns
(injection, unsafe deserialization, etc.) rather than style issues. It powers
the `codeql` badge in the README.

## When it runs

- On every **push to `main`** and **pull request targeting `main`**.
- **Weekly** on a `schedule` (`cron: 17 6 * * 1`), so newly shipped CodeQL
  queries run against unchanged code.
- On demand via **`workflow_dispatch`**.

## How it runs

One job, `analyze`:

1. Checkout.
2. **Initialize CodeQL** for `csharp` with **`build-mode: none`** (buildless
   extraction).
3. **Perform CodeQL analysis** and upload the results.

## Permissions & security

The workflow defaults to `contents: read`; the `analyze` job adds
`security-events: write` (to upload results to the dashboard) and `actions:
read` (needed by the action on private repositories, harmless on public ones).

## Handle with care

- **`build-mode: none` is a deliberate choice.** Buildless extraction needs no
  .NET SDK or build step, and it sidesteps compiler-tracing problems on a very
  new SDK. If you ever want deeper data-flow analysis, switch to `manual` and add
  an explicit `dotnet build` step — do not expect `autobuild` to be a free
  upgrade.
- **The two CodeQL steps must stay on the same action SHA.** `init` and
  `analyze` come from `github/codeql-action`; bump them together (and the
  `upload-sarif` reference in [`scorecard`](scorecard.en.md), which uses the same
  action family).
- **The weekly `schedule` is not redundant.** It applies query updates to code
  that has not changed; removing it means new query classes are never run until
  the next push.

## Related

- [`scorecard`](scorecard.en.md) — also uploads SARIF to code-scanning, via the
  same `github/codeql-action/upload-sarif` action.
- [`dependency-review`](dependency-review.en.md) — the dependency-side security
  gate, complementary to this code-side one.
