# Integrating Catalog Versioning into CI/CD

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./CatalogVersioningCI.fr.md)

This guide assumes that the baseline has already been created and committed:

```bash
fce catalog update --solution MyApp.sln
git add errors-baseline.json
git commit -m "chore: add error catalog baseline"
```

CI then needs only one read-only operation: compare the current catalog with the accepted contract.

```bash
fce catalog diff --solution MyApp.sln
```

## 🚦 Expected pull-request behavior

The recommended workflow is:

1. the pull request runs `fce catalog diff` against `errors-baseline.json`;
2. no change lets the job pass;
3. compatible additions are reported but do not fail the job with the default policy;
4. a breaking change returns `2` and blocks the pull request;
5. the author either fixes the accidental change or deliberately updates the baseline.

Drift should be visible where the change is reviewed: ideally in the pull request itself, not only in a pipeline log.

## GitHub Actions

The following example preserves the report, publishes it only when a change reaches the configured threshold, and then propagates the real `fce` exit code.

```yaml
name: error-catalog

on:
  pull_request:
    branches: [main]

jobs:
  catalog-diff:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: write

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Make fce available on the runner: dotnet tool, cached source build,
      # or your internal distribution mechanism.

      - name: Compare the catalog with the baseline
        id: catalog
        shell: bash
        run: |
          set +e
          fce catalog diff \
            --solution MyApp.sln \
            --report markdown > catalog-diff.md
          exit_code=$?
          echo "exit_code=$exit_code" >> "$GITHUB_OUTPUT"
          exit 0

      - name: Post the breaking-change report
        if: steps.catalog.outputs.exit_code == '2'
        run: gh pr comment "${{ github.event.pull_request.number }}" --body-file catalog-diff.md
        env:
          GH_TOKEN: ${{ github.token }}

      - name: Propagate the catalog result
        if: steps.catalog.outputs.exit_code != '0'
        run: exit "${{ steps.catalog.outputs.exit_code }}"
```

This separation avoids confusing two situations:

- `2` means the contract changed according to the selected policy;
- `1` means the command could not execute correctly.

With `--fail-on breaking`, only breaking changes fail the job. To fail CI on every change, use `--fail-on any`.

## GitLab CI

```yaml
error-catalog:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    # Make fce available on the runner before this command.
    - fce catalog diff --solution MyApp.sln --report markdown > catalog-diff.md
  artifacts:
    when: always
    paths:
      - catalog-diff.md
```

The `fce` exit code directly controls the job result. `artifacts: when: always` preserves the report even when the job fails.

You can then publish `catalog-diff.md` on the merge request through the [GitLab Notes API](https://docs.gitlab.com/ee/api/notes.html).

## ✍️ Accepting a change

The simplest and most explicit flow remains local:

```bash
fce catalog update --solution MyApp.sln
git diff -- errors-baseline.json
git add errors-baseline.json
git commit -m "chore: update error catalog baseline"
```

The reviewer can then see the contract change in the pull request's Git diff.

> Do not run `catalog update` automatically after a failed diff. That would turn every accidental break into an accepted contract without a human decision.

## ⚙️ Automatically seeding a missing baseline

Local initialization is recommended because it keeps CI read-only. Automatic seeding can still be useful when rolling the tool out progressively across many repositories.

Run this operation only on the default branch, with explicit write permission:

```yaml
permissions:
  contents: write

steps:
  - uses: actions/checkout@v4

  - name: Seed the error-catalog baseline if missing
    run: |
      if [ ! -f errors-baseline.json ]; then
        fce catalog update --solution MyApp.sln
        git add errors-baseline.json
        git -c user.name='github-actions[bot]' \
            -c user.email='41898282+github-actions[bot]@users.noreply.github.com' \
            commit -m 'chore: seed the error-catalog baseline'
        git push
      fi
```

This step must create the file only when it is absent. It must not silently refresh an existing baseline.

## 🏷️ Accepting a change with a GitHub label

A team can turn acceptance into an explicit pull-request action. For example, adding an `accept-contract-change` label can regenerate and commit the baseline on the PR branch.

```yaml
name: accept-error-catalog-change

on:
  pull_request:
    types: [labeled]

jobs:
  accept:
    if: github.event.label.name == 'accept-contract-change'
    runs-on: ubuntu-latest
    permissions:
      contents: write

    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ github.event.pull_request.head.ref }}

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Make fce available on the runner before this step.

      - name: Regenerate and commit the baseline
        run: |
          fce catalog update --solution MyApp.sln
          git add errors-baseline.json

          if git diff --cached --quiet; then
            echo 'Baseline already up to date — nothing to accept.'
          else
            git -c user.name='github-actions[bot]' \
                -c user.email='41898282+github-actions[bot]@users.noreply.github.com' \
                commit -m 'chore: accept error-catalog contract change'
            git push
          fi
```

This workflow works directly for branches in the same repository. For a pull request from a fork, the author generally needs to run `fce catalog update` locally and push the commit to their fork.

## 📦 Checking the default branch and releases

Also running `fce catalog diff` on the default branch detects a change that bypassed the normal pull-request flow.

To compare a release artifact with the baseline without rebuilding the solution:

```bash
fce catalog diff --against path/to/release-snapshot.json
```

This form can produce release notes for the error contract or compare the previous release snapshot with a candidate snapshot.

## 📚 Related documents

- [Understanding and using catalog versioning](CatalogVersioning.en.md)
- [Command, format, and exit-code reference](CatalogVersioningReference.en.md)

---

<div align="center">
<a href="CatalogVersioningReference.en.md">← Command reference</a> · <a href="../../../README.md#-documentation">↑ Table of contents</a> · <a href="ArchitectureOfTheDocumentationPipeline.en.md">Architecture of the Documentation Pipeline →</a>
</div>

---
