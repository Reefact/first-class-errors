# Generated documentation

This folder holds **living documentation produced automatically by CI/CD**.
Do not edit its contents by hand: anything here is regenerated (and overwritten)
on the next generation run.

- [`gendoc/`](gendoc) — GenDoc's own error catalog (`FirstClassErrors.GenDoc` documenting itself with
  `fce generate`), regenerated on every pull request by the `gendoc-docs` workflow:
  - [`catalog/`](gendoc/catalog) — the human-readable catalog (Markdown, one page per error).
  - `errors-baseline.json` — the versioned contract: the catalog as of the last `cli` release. Only
    changed by `fce catalog update`, run automatically after a successful `cli` release.
  - `errors-diff.md` — the catalog's pending changes against that baseline (informational; see
    [Catalog Versioning](../handwritten/for-users/CatalogVersioning.en.md)). A breaking change reported
    here must be matched by a major version bump at the next `cli` release, or `release.yml` refuses to
    publish it.

Handwritten documentation lives under [`../handwritten`](../handwritten):
[`for-users`](../handwritten/for-users) and
[`for-maintainers`](../handwritten/for-maintainers).
