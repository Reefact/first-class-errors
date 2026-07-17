# Documentation worker output unreadable

- **Code:** `GENDOC_WORKER_OUTPUT_UNREADABLE`
- **Source:** `DocumentationToolchain`

The extraction worker exited successfully and wrote a result file, but the file does not deserialize into an extraction result. The assembly path is carried in the error context.

## Diagnostics

- **The worker and the generator come from different tool versions and no longer agree on the result format.** — _origin:_ Internal — Check that the worker next to the tool belongs to the same installation; reinstall the tool if in doubt.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-worker-output-unreadable",
  "title": "The documentation worker produced unreadable output.",
  "detail": "The extraction worker's result file could not be read as an extraction result.",
  "code": "GENDOC_WORKER_OUTPUT_UNREADABLE"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] The documentation worker produced unreadable output for '/src/app/bin/Release/net8.0/Application.dll'. error.code=GENDOC_WORKER_OUTPUT_UNREADABLE
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `AssemblyPath` | `System.String` | Full path of the assembly being documented. | `/src/app/bin/Release/net8.0/Application.dll` |

