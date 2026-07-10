# Release dry run (manual)

> Maintainer / operational documentation. This is **not** part of the library's
> user documentation under `doc/`.

## What it is

The `release` workflow (`.github/workflows/release.yml`) publishes the NuGet
packages. It normally runs **only when a version tag is pushed** (`v1.2.3`), so
its whole pipeline — version parsing, build, test, pack, SBOM generation, OIDC,
provenance attestation, and the publish steps — otherwise runs *for the first
time in production, on a tag, once*.

The **manual dry run** lets you run that same pipeline **on demand**, all the way
through the provenance attestation, **without publishing anything**. It is a
rehearsal: you confirm the release machinery is healthy before it matters.

## What it does — and does not do

| Step | Real release (tag push) | Dry run |
| --- | --- | --- |
| Resolve & validate version | ✅ | ✅ |
| Restore, build, test | ✅ | ✅ |
| Pack the two published projects | ✅ | ✅ |
| Embed the SPDX SBOM | ✅ | ✅ |
| Upload packages as workflow artifacts | ✅ | ✅ |
| **Sign the provenance attestation** | ✅ | ✅ (see *Impacts*) |
| Log in to NuGet (OIDC) | ✅ | ⛔ skipped |
| **Push to nuget.org** | ✅ | ⛔ skipped |
| **Create the GitHub Release** | ✅ | ⛔ skipped |

The three publish steps are gated on
`github.event_name == 'push' || inputs.dry_run == false`, so:

- a **tag push always publishes** — the normal release path is untouched;
- a **manual run publishes only if you explicitly untick `dry_run`**.

## How to run it

1. On GitHub, open the **Actions** tab.
2. In the left sidebar, select the **release** workflow.
3. Click **Run workflow** (top right).
4. Fill in the inputs:
   - **version** — any valid SemVer; use an obviously fake one such as
     `0.0.0-dry.1` (nothing is published, so the value only has to be valid).
   - **dry run** — **already ticked by default**. Leave it ticked.
5. Click **Run workflow** and watch the run.

If the run is green through *Attest build provenance*, the release pipeline is
healthy.

## Impacts

A dry run is *almost* free of side effects, with one exception to be aware of:

- **It creates a real provenance attestation.** The `Attest build provenance`
  step runs in a dry run (on purpose — OIDC and attestation-permission failures
  are exactly what it is there to catch). That attestation is written to the
  repository's attestation store and to the public Sigstore transparency log —
  it is **permanent and public**, and references the throwaway version. This is
  harmless but not nothing, so:
  - use a clearly fake version (`0.0.0-dry.N`) so a throwaway attestation is
    never mistaken for a real release;
  - run the manual dry run deliberately (before a real release, or after
    changing `release.yml`), not casually in a loop.
- **Nothing is published.** No package reaches nuget.org, and no GitHub Release
  or git tag is created.
- **The packed `.nupkg` / `.snupkg` are uploaded as workflow-run artifacts**,
  which you can download from the run page to inspect, and which expire on the
  repository's normal artifact retention.

## When to use it

- Before cutting an important release, as a final smoke test of the pipeline.
- After changing `release.yml`, the packable `.csproj` files, or the packaging
  configuration (`Directory.Build.props`), since those changes are otherwise
  unverified until a real tag.

## Related: the automatic dry run

For the **side-effect-free** part of the pipeline — build, pack, and SBOM
embedding — there is nothing to trigger by hand: the `release-dryrun` workflow
(`.github/workflows/release-dryrun.yml`) runs it automatically on **every pull
request and push to `main`**, and fails if the SBOM stops being embedded. It has
no attestation and no publish, so it runs continuously with no side effects.

Use the **manual** dry run documented here when you additionally want to rehearse
the **attestation / OIDC** path that the automatic one deliberately leaves out.

## What neither dry run can test

The actual **push to nuget.org** and the **repository-signed bytes** nuget.org
serves cannot be exercised without publishing — nuget.org has no "dry-run push".
That final link is only ever validated by a real release.
