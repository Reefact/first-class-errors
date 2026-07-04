# The HTML renderer

The **html** renderer turns the error catalog into a **self-contained static website**: a searchable, filterable home page and — in the multi-page layout — one page per error. It is a final, human-facing output, not a pivot format.

## When to use HTML vs Markdown vs JSON

| Format | Use it for |
| --- | --- |
| **json** | machine consumption, integration, a stable technical pivot |
| **markdown** | portable docs in Git, a wiki, or a Confluence-friendly page |
| **html** | a visual, standalone catalog: local browsing, a CI/CD artifact, a static portal, a product demo, an internal error-reference site |

Reach for **html** when the audience is human (developers, support, QA, operations) and you want a polished, navigable catalog. Keep **json**/**markdown** for tooling and text-first workflows.

## Generating

```bash
# Multi-page site (default, recommended for large catalogs)
fce generate --format html --layout split --output ./error-catalog

# Single-page site
fce generate --format html --layout single --output ./error-catalog
```

The output is a complete folder, ready to open locally or publish to any static host (GitHub/GitLab Pages, Azure Static Web Apps, S3, an internal nginx, …). Publishing is left to your CI/CD — the renderer only produces files.

```text
error-catalog/
  index.html                   (CSS and JS inlined — self-contained)
  errors/                      (split layout only)
    ORDER_ALREADY_SHIPPED.html
    TEMPERATURE_BELOW_ABSOLUTE_ZERO.html
  assets/
    search-index.json          (for external tooling; in-page search is self-contained)
```

Each error has a stable URL derived from its **code** (`errors/ORDER_ALREADY_SHIPPED.html`, or `#err-ORDER_ALREADY_SHIPPED` in single-page) — never from its message, title, or generation order.

## Features

- **No external dependency.** The CSS and JS are inlined into every page, icons are inline SVG, the font stack is the system default — each page is self-contained and works offline from a local folder (or on its own), with no CDN.
- **Light / dark theme.** Follows the system preference (`prefers-color-scheme`) by default, with a manual toggle that is remembered in `localStorage`.
- **Search & filters.** A client-side search over every error (code, messages, documentation, context) and filters by source and by presence of a public detail. Search works offline (it reads data embedded in the page, so it does not depend on a network fetch).
- **Localization.** Labels are localized for `--language` (e.g. `--language fr`), like the Markdown renderer. Public messages follow the culture; the internal diagnostic message stays in the author language.
- **Deterministic.** Errors are ordered by code and no timestamp is emitted, so the generated site diffs cleanly and can be compared across builds.

## The three messages

Each error is shown with its public and internal messages clearly separated:

- **Public summary** and **Public detail** — the exposable messages.
- **Internal diagnostic message** — flagged as internal and rendered as a log line; it is **never** presented as an exposable public message.

## ⚠️ Security — this can be an internal artifact

The HTML site can display the **internal diagnostic message** of every error. If your catalog carries rich diagnostics (identifiers, offending values, internal state), **publishing the site on a public space can expose sensitive information**. Treat the generated site as potentially internal: publish it behind the same access controls as your logs, or generate a public variant from a catalog whose diagnostic messages are safe.

> Regenerate into a fresh output folder: the renderer overwrites the files it produces but does not delete stale files left by a previous run.
