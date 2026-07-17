# Documentation worker output missing

- **Code:** `GENDOC_WORKER_OUTPUT_MISSING`
- **Source:** `DocumentationToolchain`

The extraction worker exited successfully but the result file it was asked to write does not exist. The assembly path is carried in the error context.

## Diagnostics

- **The temporary directory is not writable, or an antivirus or cleanup job removed the file between the worker's exit and its harvesting.** — _origin:_ External — Check the permissions and free space of the temporary directory used by the generation.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-worker-output-missing",
  "title": "The documentation worker produced no output.",
  "detail": "The extraction worker exited successfully but its result file is missing.",
  "code": "GENDOC_WORKER_OUTPUT_MISSING"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] The documentation worker produced no output for '/src/app/bin/Release/net8.0/Application.dll'. error.code=GENDOC_WORKER_OUTPUT_MISSING
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `AssemblyPath` | `System.String` | Full path of the assembly being documented. | `/src/app/bin/Release/net8.0/Application.dll` |

