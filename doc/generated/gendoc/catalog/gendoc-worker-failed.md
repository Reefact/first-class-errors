# Documentation worker failed

- **Code:** `GENDOC_WORKER_FAILED`
- **Source:** `DocumentationToolchain`

The extraction worker runs once per documented assembly, in its own process, against that assembly's dependency graph. It exited with a non-zero code for one assembly. The assembly path and the exit code are carried in the error context; the worker's error output is appended to the diagnostic message.

## Diagnostics

- **The target assembly failed to load in the worker (missing dependency, mismatched FirstClassErrors version).** — _origin:_ External — Read the worker's error output in the diagnostic message; check the target's deps.json next to the assembly.
- **A documentation method or example factory of the target threw while the worker executed it.** — _origin:_ External — Read the worker's error output; run the target's documentation methods in a unit test to reproduce.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-worker-failed",
  "title": "The documentation worker failed.",
  "detail": "The extraction worker exited with an error for one of the documented assemblies.",
  "code": "GENDOC_WORKER_FAILED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] The documentation worker failed (exit code 1) for '/src/app/bin/Release/net8.0/Application.dll'. Fatal error while extracting documentation. error.code=GENDOC_WORKER_FAILED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `AssemblyPath` | `System.String` | Full path of the assembly being documented. | `/src/app/bin/Release/net8.0/Application.dll` |
| `ExitCode` | `System.Int32` | Exit code returned by the child process. | `1` |

