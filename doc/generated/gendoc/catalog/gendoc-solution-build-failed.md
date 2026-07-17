# Solution build failed

- **Code:** `GENDOC_SOLUTION_BUILD_FAILED`
- **Source:** `DocumentationToolchain`

The generator builds the solution before documenting it (unless the build step is disabled). The build failed, so no assembly could be documented. The solution path and the exit code are carried in the error context; the build output is appended to the diagnostic message.

## Diagnostics

- **The solution under documentation has compile errors.** — _origin:_ External — Read the build output in the diagnostic message; build the solution by hand to reproduce.
- **Package restore failed (offline machine, feed outage, or authentication).** — _origin:_ InternalOrExternal — Check the restore section of the build output and the reachability of the configured NuGet feeds.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-solution-build-failed",
  "title": "The solution build failed.",
  "detail": "The solution under documentation did not build; the build output is in the diagnostic message.",
  "code": "GENDOC_SOLUTION_BUILD_FAILED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] dotnet build failed for solution '/src/app/Application.sln' (exit code 1). CS1002: ; expected error.code=GENDOC_SOLUTION_BUILD_FAILED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `SolutionPath` | `System.String` | Full path of the solution file the generation request designates. | `/src/app/Application.sln` |
| `ExitCode` | `System.Int32` | Exit code returned by the child process. | `1` |

