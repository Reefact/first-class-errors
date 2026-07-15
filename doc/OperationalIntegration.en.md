# Generating and Publishing the Error Catalog

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./OperationalIntegration.fr.md)

FirstClassErrors becomes operationally useful when the catalog is generated from the exact code being built, and published where developers, support teams, and operators can reach it.

This guide covers the delivery workflow. For structured logging and production diagnostics, see the [structured logging guide](LoggingIntegration.en.md).

## The delivery flow

```mermaid
flowchart LR
    A[Build application] --> B[Extract error knowledge]
    B --> C[Render catalog]
    C --> D[Publish artifact or site]
    B --> E[Compare contract baseline]
```

A reliable pipeline should:

1. build the application;
2. generate the catalog from the built code;
3. publish the generated files;
4. optionally compare the current contract with a committed baseline — the last accepted snapshot of your error codes and context keys (see [Catalog Versioning](CatalogVersioning.en.md)).

## Opt projects into generation

Solution-level generation is opt-in. Add the marker directly to every `.csproj` that defines documented application errors:

```xml
<PropertyGroup>
  <GenerateErrorDocumentation>true</GenerateErrorDocumentation>
</PropertyGroup>
```

The marker is read from the project file itself. A value inherited from `Directory.Build.props` is not detected.

When no project opts in, the generator warns instead of silently presenting an empty catalog as a valid result. The command still exits successfully — the empty catalog is a warning, not a failure. If a solution must always produce a catalog, enforce that on the CI side, for example by failing the job when the generated output is empty; the CLI does not treat a missing opt-in as an error on its own (`--strict` aborts on extraction failures, which is a different situation).

For ambiguous declarations, project discovery, and worker behavior, see [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md).

## Generate the catalog locally

Install the CLI, build, then generate from the existing binaries:

```bash
dotnet tool install --global FirstClassErrors.Cli
dotnet build MyApp.sln -c Release
fce generate \
  --solution MyApp.sln \
  --configuration Release \
  --no-build \
  --format markdown \
  --output artifacts/errors.md \
  --service-name my-api
```

`--service-name` is required for Markdown and HTML because their RFC 9457 examples use problem types such as:

```text
urn:problem:my-api:payment-declined
```

JSON output does not require a service name.

## Minimal GitHub Actions workflow

The workflow renders the catalog as HTML with `--layout split` (one page per error; see [The HTML Renderer](TheHtmlRenderer.en.md)):

```yaml
name: error-documentation

on:
  pull_request:
  push:
    branches: [main]

jobs:
  generate-error-catalog:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install FirstClassErrors CLI
        run: dotnet tool install --global FirstClassErrors.Cli

      - name: Build
        run: dotnet build MyApp.sln -c Release

      - name: Generate catalog
        run: |
          fce generate \
            --solution MyApp.sln \
            --configuration Release \
            --no-build \
            --format html \
            --layout split \
            --output artifacts/error-catalog \
            --service-name my-api

      - name: Publish catalog artifact
        uses: actions/upload-artifact@v4
        with:
          name: error-catalog
          path: artifacts/error-catalog
```

The build and generation steps use the same configuration. `--no-build` prevents the generator from rebuilding a different set of binaries.

With `--layout split`, the output directory holds one page per error plus a shared index and assets:

```text
artifacts/error-catalog/
├── index.html
├── errors/
│   ├── PAYMENT_DECLINED.html
│   └── CUSTOMER_NOT_FOUND.html
└── assets/
    └── search-index.json
```

This is a first, verifiable integration — it publishes a temporary workflow artifact, nothing more. On a pull request the job checks that generation still succeeds; on `main` it publishes the accepted catalog as that artifact. Durable, versioned publication (see [Choose a publication target](#choose-a-publication-target) and [Keep versioned catalogs](#keep-versioned-catalogs)) belongs on a tag or release trigger, where the version name is stable.

## Generate several languages

Run one generation per locale:

```yaml
strategy:
  matrix:
    language: [en, fr]

steps:
  # checkout, setup, install, and build omitted

  - name: Generate ${{ matrix.language }} catalog
    run: |
      fce generate \
        --solution MyApp.sln \
        --configuration Release \
        --no-build \
        --format html \
        --layout split \
        --language "${{ matrix.language }}" \
        --output "artifacts/error-catalog-${{ matrix.language }}" \
        --service-name my-api
```

File names and anchors remain stable across languages, so the same error code resolves to the same file name in every language. Publish each language in its own directory, as the matrix above does, so they never overwrite one another. See [Internationalization](Internationalization.en.md) for how content and renderer templates are localized.

## Choose a publication target

The generated catalog may be:

- retained as a pipeline artifact;
- attached to a release;
- deployed as a static site;
- copied into an internal documentation portal;
- published beside service operational documentation.

The important requirement is not the platform. It is that the catalog for a deployed version is reachable by the people investigating that version.

## Keep versioned catalogs

Two complementary forms of versioning matter, and they are easy to confuse: keeping the published **documentation** of each release, and controlling how the error **contract** itself evolves against a baseline. This section is about the first; [Guard the error contract](#guard-the-error-contract) below is about the second.

A single “latest” site is useful for daily work, but it does not explain an older production release after the contract has evolved.

For long-lived or support-critical systems, publish at least one immutable form per release:

```text
/errors/latest/
/errors/releases/2.4.0/
/errors/releases/2.3.1/
```

In a tag- or release-triggered workflow, derive the output directory from the version so each release lands in its own immutable path:

```yaml
--output "artifacts/error-catalog/${{ github.ref_name }}"
```

`github.ref_name` is only meaningful as a version on a tag or release trigger; on a branch push it is the branch name.

Log events that record the deployed version alongside the error then let support start from a logged occurrence — its `InstanceId` plus that version — and open the matching catalog.

## Guard the error contract

Generation answers “what does this version document?” Catalog versioning answers “did this version break a previously accepted contract?”

`fce catalog diff` compares the catalog extracted from the current build against a baseline file committed to the repository and reports contract changes:

```bash
fce catalog diff --solution MyApp.sln --configuration Release --no-build
```

Keep the accepted baseline in source control and run the comparison in pull requests.

See:

- [Catalog Versioning](CatalogVersioning.en.md) for the mental model and daily workflow;
- [Catalog Versioning CI/CD](CatalogVersioningCI.en.md) for complete GitHub Actions and GitLab examples.

## Failure policy

Treat these situations differently:

| Situation | Pipeline meaning |
| --- | --- |
| application build fails | the product cannot be produced |
| extraction or rendering fails | the catalog cannot be trusted |
| no project opted in | configuration is incomplete or the solution intentionally has no documented project |
| catalog contract breaks | human review is required before acceptance |
| publishing fails | the documentation is unavailable even if generation succeeded |

Do not hide extraction or publication failures behind `continue-on-error` in a pipeline that promises operational documentation.

## Review checklist

Before approving a catalog-delivery pipeline, verify that:

- opted-in projects declare the marker in their own `.csproj`;
- generation uses the same binaries and configuration as the application build;
- `--service-name` is supplied for Markdown or HTML;
- generated files are published somewhere reachable;
- locale-specific outputs do not overwrite one another;
- each supported release has an immutable documentation URL or artifact;
- contract comparison is separate from catalog rendering;
- generation and publication failures are visible.

---

<div align="center">
<a href="ArbitraryTestValues.en.md">← Arbitrary Test Values</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="LoggingIntegration.en.md">Integrate FirstClassErrors with Structured Logging →</a>
</div>

---