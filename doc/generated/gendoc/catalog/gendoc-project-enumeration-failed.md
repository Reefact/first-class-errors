# Solution project enumeration failed

- **Code:** `GENDOC_PROJECT_ENUMERATION_FAILED`
- **Source:** `DocumentationToolchain`

The generator enumerates a solution's projects by running 'dotnet sln list'. That command failed, so no project could be discovered. The solution path and the exit code are carried in the error context; the command's error output is appended to the diagnostic message.

## Diagnostics

- **The solution file is malformed or references projects that no longer exist.** — _origin:_ External — Run 'dotnet sln <solution> list' by hand and read its error output.
- **The .NET SDK on the machine is missing or too old for the solution format (for example .slnx support).** — _origin:_ External — Check 'dotnet --version' against the solution format and the repository's SDK requirements.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-project-enumeration-failed",
  "title": "The solution's projects could not be listed.",
  "detail": "The 'dotnet sln list' command failed for the requested solution.",
  "code": "GENDOC_PROJECT_ENUMERATION_FAILED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] Failed to list the projects of solution '/src/app/Application.sln' (exit code 1). The solution file is invalid. error.code=GENDOC_PROJECT_ENUMERATION_FAILED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `SolutionPath` | `System.String` | Full path of the solution file the generation request designates. | `/src/app/Application.sln` |
| `ExitCode` | `System.Int32` | Exit code returned by the child process. | `1` |

