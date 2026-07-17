# Documentation worker run failed

- **Code:** `GENDOC_WORKER_RUN_FAILED`
- **Source:** `DocumentationToolchain`

Launching the extraction worker, or harvesting its result, threw an unexpected runtime exception (an I/O failure, a permission error…). The assembly path is carried in the error context; the runtime cause travels with the raised exception.

## Diagnostics

- **A file-system or permission problem around the temporary result file or the worker binary.** — _origin:_ InternalOrExternal — Read the inner exception attached to the failure; check the temporary directory and the tool's installation directory.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-worker-run-failed",
  "title": "The documentation worker could not be run.",
  "detail": "Running the extraction worker failed unexpectedly for one of the documented assemblies.",
  "code": "GENDOC_WORKER_RUN_FAILED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] Failed to run the documentation worker for '/src/app/bin/Release/net8.0/Application.dll'. error.code=GENDOC_WORKER_RUN_FAILED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `AssemblyPath` | `System.String` | Full path of the assembly being documented. | `/src/app/bin/Release/net8.0/Application.dll` |

