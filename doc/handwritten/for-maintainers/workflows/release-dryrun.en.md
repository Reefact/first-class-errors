# `release-dryrun` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](release-dryrun.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/release-dryrun.yml`](../../../../.github/workflows/release-dryrun.yml)

## What it is for

`release-dryrun` continuously rehearses the **side-effect-free** portion of the
release — build, pack, and embed the SBOM for the two published projects — on
every push and pull request. Because [`release`](release.en.md) itself runs only
on a tag or a manual dispatch, its packaging path would otherwise be exercised
for the first time in production, on a tag, once. This catches packaging/SBOM
regressions in ordinary CI instead.

It is the automatic counterpart to the **manual** dry run documented in
[Release dry run (manual)](../ReleaseDryRun.en.md): this one deliberately leaves
out the attestation/OIDC path (which has side effects), the manual one adds it.

## When it runs

- On every **push to `main`**, **pull request targeting `main`**, and on demand
  via **`workflow_dispatch`**.

## How it runs

One job, `pack`: checkout → setup the release SDK (10.0.x) → `dotnet build` →
**`tools/packaging/pack.sh`**, which packs the two published projects with their
SBOM and asserts the SBOM is actually embedded.

## Permissions & security

`contents: read` only. It stops before every step that has a side effect, so it
needs none of `release`'s write scopes:

- **no provenance attestation** — that writes a permanent public record
  (Sigstore/Rekor + the attestation store); kept to the manual dry run and real
  releases;
- **no NuGet login / push** — nuget.org has no "dry-run push", so the publish
  stays release-only;
- **no GitHub Release** — no tag or release is ever created here.

## Handle with care

- **It shares `tools/packaging/pack.sh` with `release`, and that is the point.**
  There is exactly one definition of "pack the release artifacts", so this
  rehearsal cannot drift from the real release. Do not inline a separate pack
  command here — change `pack.sh` and both follow.
- **Keep it side-effect-free.** The value of this workflow is that it runs on
  *every* PR with no attestation and no publish. Do not add the attestation or a
  login step here; those belong to the manual dry run in `release`, which runs
  deliberately, not on every push.
- **The `DRYRUN_VERSION` is a throwaway.** Nothing is published, so the exact
  value only has to be a valid SemVer the pack accepts. The real version comes
  from the tag and is validated in `release`.
- **This job's unique contribution is the *packaging*.** The unit/integration
  tests run in [`ci`](ci.en.md); do not duplicate them here.

## Related

- [`release`](release.en.md) — the real publish path, sharing the same `pack.sh`.
- [Release dry run (manual)](../ReleaseDryRun.en.md) — the manual dry run that
  additionally rehearses the attestation/OIDC path.
