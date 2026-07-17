# Project build output not found

- **Code:** `GENDOC_TARGET_ASSEMBLY_NOT_FOUND`
- **Source:** `DocumentationRequest`

The generator resolved a project's build output path (MSBuild TargetPath), but no assembly exists there. Both the project path and the resolved target path are carried in the error context.

> **Business rule:** Every documented project must have been built for the requested configuration and target framework.

## Diagnostics

- **The solution was not built before generation (for example the build step was skipped), so the output is missing.** — _origin:_ External — Build the solution first, or let the generator build it by enabling its build step.
- **The generation request targets a different configuration or framework than the one that was built.** — _origin:_ External — Compare the resolved TargetPath in the error context with the directory that was actually built.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-target-assembly-not-found",
  "title": "A project's build output was not found.",
  "detail": "The project's resolved target assembly does not exist on disk.",
  "code": "GENDOC_TARGET_ASSEMBLY_NOT_FOUND"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationRequest] Target assembly not found for project '/src/app/Application/Application.csproj'. Resolved TargetPath='/src/app/Application/bin/Release/net8.0/Application.dll'. error.code=GENDOC_TARGET_ASSEMBLY_NOT_FOUND
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `ProjectPath` | `System.String` | Full path of the project file being processed. | `/src/app/Application/Application.csproj` |
| `TargetPath` | `System.String` | Build output path resolved for the project (MSBuild TargetPath). | `/src/app/Application/bin/Release/net8.0/Application.dll` |

