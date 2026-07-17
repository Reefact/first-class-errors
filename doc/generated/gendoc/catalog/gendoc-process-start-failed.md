# Toolchain process failed to start

- **Code:** `GENDOC_PROCESS_START_FAILED`
- **Source:** `DocumentationToolchain`

The generator drives the .NET SDK and its extraction worker through child processes. One of them could not be started at all. The executable name is carried in the error context.

## Diagnostics

- **The 'dotnet' host is not installed or not on the PATH of the process running the generation.** — _origin:_ External — Run 'dotnet --info' in the same environment (shell, CI step, service account) as the generation.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-process-start-failed",
  "title": "A required process could not be started.",
  "detail": "A child process required by the documentation generation could not be started.",
  "code": "GENDOC_PROCESS_START_FAILED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] Failed to start process 'dotnet'. error.code=GENDOC_PROCESS_START_FAILED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `ProcessFileName` | `System.String` | Executable name of the child process the generator tried to run. | `dotnet` |

