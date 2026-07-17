# Target path resolution failed

- **Code:** `GENDOC_TARGET_PATH_RESOLUTION_FAILED`
- **Source:** `DocumentationToolchain`

The generator resolves each project's build output path by querying the .NET SDK ('dotnet msbuild -getProperty:TargetPath'). That query threw, so the project cannot be located and is skipped (or the generation stops, per the configured failure behavior). The project path is carried in the error context.

## Diagnostics

- **The project file does not evaluate (broken import, missing SDK workload, malformed XML).** — _origin:_ External — Run 'dotnet msbuild <project> -getProperty:TargetPath' by hand and read its error output.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-target-path-resolution-failed",
  "title": "A project's target path could not be resolved.",
  "detail": "The build output path of a project could not be resolved through the .NET SDK.",
  "code": "GENDOC_TARGET_PATH_RESOLUTION_FAILED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationToolchain] Failed to resolve target path for project '/src/app/Application/Application.csproj'. error.code=GENDOC_TARGET_PATH_RESOLUTION_FAILED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `ProjectPath` | `System.String` | Full path of the project file being processed. | `/src/app/Application/Application.csproj` |

