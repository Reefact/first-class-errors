# Release dry run (manual)

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](ReleaseDryRun.fr.md)

↑ Part of the [maintainer documentation](README.md) · see also the workflow
reference for [`release`](workflows/release.en.md) and
[`release-dryrun`](workflows/release-dryrun.en.md).

> Maintainer / operational documentation. This is **not** part of the library's
> user documentation under `doc/`.

## What it is

The `release` workflow (`.github/workflows/release.yml`) publishes the NuGet
packages. It normally runs **only when a version tag is pushed** (`v1.2.3`), so
its whole pipeline — version parsing, build, test, pack, SBOM generation, OIDC,
provenance attestation, and the publish steps — otherwise runs *for the first
time in production, on a tag, once*.

The **manual dry run** lets you run that same pipeline **on demand**, all the way
through the provenance attestation and the NuGet OIDC login, **without publishing
anything**. It is a rehearsal: you confirm the release machinery is healthy
before it matters.

## What it does — and does not do

| Step | Real release (tag push) | Dry run |
| --- | --- | --- |
| Resolve & validate version | ✅ | ✅ |
| Restore, build, test | ✅ | ✅ |
| Pack the published trains | ✅ | ✅ |
| Embed the SPDX SBOM | ✅ | ✅ |
| Upload packages as workflow artifacts | ✅ | ✅ |
| **Sign the provenance attestation** | ✅ | ✅ (see *Impacts*) |
| **Log in to NuGet (OIDC)** | ✅ | ✅ (see *Impacts*) |
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

A dry run is *almost* free of side effects, with two things to be aware of:

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
- **It performs the real NuGet OIDC login.** The trusted-publishing token
  exchange runs in a dry run — that is the point: it validates the nuget.org
  policy, so a dry run **fails red** if the trusted-publishing policy or the
  `NUGET_USER` secret is missing or misconfigured. It mints a short-lived,
  single-use API key that the dry run never spends (the push is skipped), so
  nothing is published.
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

Neither dry run proves that the **installed `fce` tool actually runs**, either.
`fce generate` does not do all the work in-process: it spawns
`FirstClassErrors.GenDoc.Worker` in a child process and resolves it next to the
installed executable (`AppContext.BaseDirectory`). The worker travels inside the
tool package only because `PackAsTool` packs the CLI's *publish* output and the
`_PublishDocumentationWorker` target drops the worker's closure there — a
mechanism `dotnet build` and `dotnet publish` do not exercise (they lay files out
next to a build binary, never inside the `.nupkg`). See the tool-install smoke
test below.

## The tool-install smoke test (the `fce` worker)

`tools/packaging/pack.sh` asserts, for the `cli` train, that the worker **file**
(`FirstClassErrors.GenDoc.Worker.dll`) is present under `tools/<tfm>/any/` in the
`.nupkg`. That guard is real and runs on every packed `cli` train — but a present
file is not a working tool: the worker's closure can still be incomplete (a
missing dependency, a wrong `.runtimeconfig.json`), which the file-presence check
cannot see. The only oracle that proves the packaged closure actually runs is a
real install followed by `fce generate`.

Run it **at least once before the first `cli-v…` tag**, and again after any change
to the CLI packaging (the `.csproj` worker targets, `PackAsTool`, or the worker's
dependencies):

```sh
# 1. Build and pack the cli train exactly as the release does.
dotnet build FirstClassErrors.sln -c Release
tools/packaging/pack.sh 0.0.0-workercheck.1 cli   # -> artifacts/FirstClassErrors.Cli.0.0.0-workercheck.1.nupkg

# 2. Install the packed tool globally. Install by PACKAGE ID (FirstClassErrors.Cli);
#    that is not the command name (fce).
dotnet tool install --global --add-source ./artifacts FirstClassErrors.Cli --version 0.0.0-workercheck.1

# 3. Generate a catalog. Either point at a built, opted-in assembly...
fce generate --assemblies path/to/YourProject.dll --format markdown --output ./out/catalog.md --service-name demo
#    ...or at a solution with at least one opted-in project (GenerateErrorDocumentation=true):
fce generate --solution path/to/Your.sln --format markdown --output ./out --service-name demo
#    Expect a NON-EMPTY catalog and ZERO "documentation worker could not be located".

# 4. Clean up.
dotnet tool uninstall --global FirstClassErrors.Cli
```

A convenient in-repo target for step 3 is `FirstClassErrors.Usage`: it opts in
(`GenerateErrorDocumentation=true`) and defines documented errors, so after a
Release build its assembly at
`FirstClassErrors.Usage/bin/Release/net10.0/FirstClassErrors.Usage.dll` yields a
non-empty catalog. Two naming traps to avoid: the package file is
`FirstClassErrors.Cli.<version>.nupkg` (the `PackageId`), not `fce.<version>.nupkg`,
and `dotnet tool install` takes that same package id, not the `fce` command name.

## Related

- [`release`](workflows/release.en.md) — the workflow this rehearses, described
  structurally (triggers, jobs, the traps in its design).
- [`release-dryrun`](workflows/release-dryrun.en.md) — the automatic,
  side-effect-free dry run that runs on every PR and push.
- [Maintainer documentation](README.md) — the index of all maintainer docs.
