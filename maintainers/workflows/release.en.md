# `release` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](release.fr.md)

> Maintainer documentation — part of the [workflow reference](README.en.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/release.yml`](../../.github/workflows/release.yml)

## What it is for

`release` builds, attests, and publishes the NuGet packages. It is the only
workflow whose full path is otherwise **never exercised before a real tag** —
version resolution, pack, SBOM, OIDC and attestation permissions all run for the
first time in production conditions. To make that testable, it doubles as a
**manual dry run** that runs everything up to and including the attestation while
skipping the publish steps.

> For the operational side of the manual dry run — how to launch it, what it
> touches, and when to use it — see the dedicated guide:
> **[Release dry run (manual)](../ReleaseDryRun.en.md)**. This page covers what
> the workflow *is* and the traps in its structure.

## When it runs

- On **push of a version tag** `v*.*.*` (e.g. `v1.2.3`, `v1.2.3-beta.1`) — this
  publishes.
- On **`workflow_dispatch`** with two inputs: `version` and `dry_run` (**default
  `true`**). A manual run publishes only if `dry_run` is explicitly unticked.

## How it runs

One job, `pack-push`: checkout → setup .NET → **resolve & validate version** →
restore → build → test → **pack** (via `tools/packaging/pack.sh`, embedding the
SPDX SBOM) → upload artifacts → **attest build provenance** → **NuGet login
(OIDC)** → **push to NuGet** → **publish GitHub Release**. The last two steps
(and only those) are gated off on a dry run.

## Permissions & security

The workflow needs three write scopes: `contents: write` (create the Release and
upload assets), `id-token: write` (mint the short-lived NuGet key via trusted
publishing), and `attestations: write` (store the signed provenance). No
long-lived `NUGET_API_KEY` is stored.

## Handle with care

This workflow encodes several hard-won fixes. Each of the following is
deliberate:

- **Version input is validated against a strict SemVer allowlist, read via the
  environment.** The tag/input is attacker-controllable (a tag like `v1.2.3;id`
  is a valid ref matching the trigger). It is passed through `env:` rather than
  interpolated into the shell, and rejected if it does not match the regex —
  otherwise it could inject commands into every step that uses it.
- **Build metadata (`+…`) is rejected even though SemVer allows it.** NuGet
  strips `+build` from the package identity, so `v1.2.3+build5` would pack as
  `1.2.3`; combined with `--skip-duplicate` on push, an already-published `1.2.3`
  would turn the release into a green no-op that publishes nothing. Failing
  loudly is the point.
- **`Attest build provenance` runs *before* both publications, and runs even in
  a dry run.** Only attested artifacts are ever released or pushed; and OIDC /
  attestation-permission failures are exactly what the dry run is there to catch.
- **The attestation deliberately does not match the nuget.org copy.** nuget.org
  repository-signs every upload (adds `.signature.p7s` inside the `.nupkg`),
  changing the checksum. The attested bytes are therefore published as **GitHub
  Release assets**, and consumers verify provenance against *those* with `gh
  attestation verify` — the nuget.org copy is verified with `dotnet nuget verify`
  instead. Do not "simplify" by attesting only the nuget.org copy.
- **`NuGet login (OIDC)` runs on every trigger, including dry run — only the push
  and the Release are gated.** The token exchange is what validates the
  trusted-publishing policy, so a dry run fails red when the policy or
  `NUGET_USER` is missing. It mints a single-use key the dry run never spends.
  Requires a trusted-publishing policy on nuget.org and the `NUGET_USER` secret
  (the profile **username**, not the email).
- **The Release step pins `--target "$GITHUB_SHA"`.** On `workflow_dispatch` the
  tag does not exist yet and `gh` would otherwise create it from the default
  branch's latest state; pinning the SHA ties the tag, source archive and
  attested packages to the commit this job actually built. The `|| … upload
  --clobber` fallback keeps a re-run idempotent.
- **`concurrency` sets `cancel-in-progress: false`.** Never cancel a
  half-finished publish.

## Related

- [Release dry run (manual)](../ReleaseDryRun.en.md) — the operational guide.
- [`release-dryrun`](release-dryrun.en.md) — the automatic, side-effect-free
  rehearsal that runs on every PR and push, sharing the same `pack.sh`.
- The README's **Supply chain** section documents how a consumer verifies the
  provenance and SBOM this workflow produces.
