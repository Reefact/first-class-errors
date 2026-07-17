# Solution path not supported

- **Code:** `GENDOC_SOLUTION_PATH_UNSUPPORTED`
- **Source:** `DocumentationRequest`

A documentation-generation request designates a file that is not a .sln or .slnx solution. Solution filters (.slnf) are deliberately not supported: the 'dotnet sln' commands the generator relies on do not process them.

> **Business rule:** A generation request must designate a .sln or .slnx solution file.

## Diagnostics

- **A project file, a solution filter (.slnf), or another artifact was passed instead of the solution.** — _origin:_ External — Check the path in the error context; pass the .sln/.slnx file, or document built assemblies directly instead.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-solution-path-unsupported",
  "title": "The solution path is not supported.",
  "detail": "The documentation generator accepts .sln and .slnx solution files.",
  "code": "GENDOC_SOLUTION_PATH_UNSUPPORTED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationRequest] Expected a .sln or .slnx file path, got: '/src/app/Application.slnf'. error.code=GENDOC_SOLUTION_PATH_UNSUPPORTED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `SolutionPath` | `System.String` | Full path of the solution file the generation request designates. | `/src/app/Application.slnf` |

