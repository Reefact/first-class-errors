# Configured worker path invalid

- **Code:** `GENDOC_WORKER_PATH_INVALID`
- **Source:** `DocumentationRequest`

A documentation-generation request explicitly configures the path of the extraction worker, but no file exists there. The configured path is carried in the error context.

> **Business rule:** A configured worker path must point to an existing FirstClassErrors.GenDoc.Worker.dll.

## Diagnostics

- **The configured path is stale — the worker was moved, or the path was written for another machine or installation layout.** — _origin:_ External — Check the path in the error context; remove the explicit setting to fall back to the worker deployed next to the tool.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-worker-path-invalid",
  "title": "The configured documentation worker was not found.",
  "detail": "The configured worker path must point to an existing FirstClassErrors.GenDoc.Worker.dll.",
  "code": "GENDOC_WORKER_PATH_INVALID"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationRequest] The configured documentation worker was not found at '/tools/fce/FirstClassErrors.GenDoc.Worker.dll'. error.code=GENDOC_WORKER_PATH_INVALID
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `WorkerPath` | `System.String` | Configured path of the documentation worker assembly. | `/tools/fce/FirstClassErrors.GenDoc.Worker.dll` |

