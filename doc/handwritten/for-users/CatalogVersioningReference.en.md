# Catalog Versioning Reference

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./CatalogVersioningReference.fr.md)

This page is the technical reference for `fce catalog update`, `fce catalog diff`, and the baseline file. For an introduction, start with [Catalog Versioning](CatalogVersioning.en.md).

## 🧾 Snapshot and baseline

The **canonical snapshot** is a JSON projection of the catalog containing only the data tracked by versioning:

- each error code;
- its title and source;
- the name and type of its context keys.

The **baseline** is a snapshot selected as the reference and committed to the repository. Its default path is `errors-baseline.json`.

The snapshot is independent of the renderer used to publish the human-facing catalog. It is deterministic: errors are ordered by code, context keys by name, and line endings are normalized. The `fce catalog` commands always extract under the `en` culture so the baseline does not depend on the catalog language.

## `fce catalog update`

```bash
fce catalog update --solution MyApp.sln
```

The command extracts the current catalog, then creates or replaces the baseline.

> Running `catalog update` means **explicitly accepting the current contract**, including any breaking changes it contains.

Behavior:

| Situation | Behavior |
| --- | --- |
| Baseline is missing | The file is created. |
| Baseline is already identical | Nothing is written. |
| Catalog differs | The baseline is replaced and the accepted changes are summarized. |
| Baseline is unreadable or corrupt | It is regenerated with a warning. |
| Baseline was produced by a newer schema | The command refuses to downgrade it and fails. |

Exit codes:

| Code | Meaning |
| --- | --- |
| `0` | Baseline created, already current, or replaced successfully. |
| `1` | Execution error or baseline schema newer than the tool. |
| `130` | Execution interrupted. |

## `fce catalog diff`

```bash
fce catalog diff --solution MyApp.sln
```

The command compares the baseline with the current snapshot and writes the report to standard output.

Exit codes:

| Code | Meaning |
| --- | --- |
| `0` | No change reaches the threshold selected by `--fail-on`. |
| `2` | At least one change reaches that threshold. |
| `1` | Execution error: missing baseline, failed extraction, invalid file, and so on. |
| `130` | Execution interrupted. |

### Failure policy: `--fail-on`

```bash
fce catalog diff --solution MyApp.sln --fail-on breaking
```

| Value | Effect |
| --- | --- |
| `breaking` | Default. Fails only on breaking changes. |
| `any` | Fails on any detected change, including compatible additions and informational changes. |
| `none` | Produces the report without failing because of drift. |

### Report format: `--report`

```bash
fce catalog diff --solution MyApp.sln --report markdown
```

| Value | Usage |
| --- | --- |
| `text` | Default terminal-oriented output. |
| `markdown` or `md` | Report ready to publish in a pull request. |
| `json` | Machine-readable report. |

### Compare an existing snapshot: `--against`

```bash
fce catalog diff --against candidate-snapshot.json
```

This variant compares the baseline with an existing snapshot file instead of extracting the catalog from source. It is useful for comparing two release artifacts.

## Change classification

| Change | Impact |
| --- | --- |
| Error code removed | Breaking |
| Context key removed | Breaking |
| Context key type changed | Breaking |
| Error code added | Compatible |
| Context key added | Compatible |
| Title or source changed | Informational |

A rename is represented as a removal followed by an addition and therefore remains breaking. When exactly one new error has the same title as the removed error, the report indicates a probable rename.

## Produce a snapshot without modifying the baseline

```bash
fce generate --snapshot artifacts/error-catalog.snapshot.json
```

`fce generate --snapshot` writes the canonical snapshot in addition to the rendered catalog. Unlike the `fce catalog` commands, this snapshot reflects the selected render language. A committed baseline should remain language-independent: prefer `fce catalog update`, or force `--language en`.

## Configuration in `fce.json`

Paths can be centralized:

```json
{
  "solution": "MyApp.sln",
  "baseline": "errors-baseline.json",
  "snapshot": "artifacts/error-catalog.snapshot.json"
}
```

Relative paths are resolved from the configuration file.

## Common extraction options

The `catalog update` and `catalog diff` commands accept, among others:

| Option | Purpose |
| --- | --- |
| `--solution <PATH>` | Extract from a solution. |
| `--assemblies <PATH>` | Extract from one or more already-built assemblies. |
| `--baseline <PATH>` | Override the configured baseline path. |
| `--configuration <NAME>` | Select the build configuration. |
| `--framework <TFM>` | Restrict a multi-target solution to one framework. |
| `--no-build` | Use existing binaries without rebuilding. |
| `--strict` | Stop extraction on the first failure. |
| `--verbose` | Write detailed diagnostics to standard error. |

---

<div align="center">
<a href="CatalogVersioning.en.md">← Catalog versioning guide</a> · <a href="../../../README.md#-next-steps">↑ Table of contents</a> · <a href="CatalogVersioningCI.en.md">CI/CD integration →</a>
</div>

---