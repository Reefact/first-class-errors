# Documentation worker not deployed

- **Code:** `GENDOC_WORKER_NOT_DEPLOYED`
- **Source:** `DocumentationToolchain`

No explicit worker path is configured, and the extraction worker was not found next to the tool — the conventional location it is deployed to. The probed directory is carried in the error context.

> **Business rule:** The extraction worker ships next to the tool; an installation without it cannot extract documentation.

## Diagnostics

- **The tool was copied or repackaged without 'FirstClassErrors.GenDoc.Worker.dll' (an incomplete manual install).** — _origin:_ Internal — Inspect the probed directory named in the error context; reinstall the tool, or configure an explicit worker path.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-worker-not-deployed",
  "title": "The documentation worker is missing.",
  "detail": "The extraction worker was not found next to the tool; the installation is incomplete.",
  "code": "GENDOC_WORKER_NOT_DEPLOYED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] The documentation worker 'FirstClassErrors.GenDoc.Worker.dll' could not be located in '/tools/fce'. Set SolutionGenerationOptions.WorkerAssemblyPath, or ensure the worker is deployed next to the tool. error.code=GENDOC_WORKER_NOT_DEPLOYED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `ProbedDirectory` | `System.String` | Directory probed for the documentation worker assembly. | `/tools/fce` |

