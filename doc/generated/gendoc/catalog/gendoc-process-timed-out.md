# Toolchain process timed out

- **Code:** `GENDOC_PROCESS_TIMED_OUT`
- **Source:** `DocumentationToolchain`

A child process of the generation (an SDK command or the extraction worker) exceeded its configured timeout and was killed. The command, its target and the timeout are carried in the error context; the output captured before the kill is appended to the diagnostic message. It is transient: the run can be retried.

## Diagnostics

- **The machine is under load, or a cold NuGet cache made the first build far slower than usual.** — _origin:_ External — Retry the run; compare its duration with the configured build, SDK-query and worker timeouts.
- **A documented assembly's example factory hangs (extraction executes the documentation methods of the target).** — _origin:_ InternalOrExternal — Check which assembly was being processed when the timeout hit; review its documentation examples for blocking calls.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-process-timed-out",
  "title": "A documentation process timed out.",
  "detail": "The operation exceeded its configured timeout and was killed; it can be retried.",
  "code": "GENDOC_PROCESS_TIMED_OUT"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] Process 'dotnet build /src/app/Application.sln' timed out after 00:10:00 and was killed. Determining projects to restore... error.code=GENDOC_PROCESS_TIMED_OUT
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `Command` | `System.String` | Short description of the child-process command that was running. | `dotnet build /src/app/Application.sln` |
| `Target` | `System.String` | Path of the solution, project or assembly the timed-out command was operating on. | `/src/app/Application.sln` |
| `Timeout` | `System.TimeSpan` | Configured timeout the child process exceeded. | `00:10:00` |

