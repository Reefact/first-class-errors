# Solution file not found

- **Code:** `GENDOC_SOLUTION_NOT_FOUND`
- **Source:** `DocumentationRequest`

A documentation-generation request designates a solution file that does not exist on disk. The full path, as resolved by the generator, is carried in the error context.

> **Business rule:** A generation request must designate an existing solution file.

## Diagnostics

- **The path is wrong or relative to an unexpected working directory (a CI step often runs from a different directory than a developer shell).** — _origin:_ External — Compare the resolved path in the error context with the actual solution location; check the working directory of the caller.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-solution-not-found",
  "title": "The solution file was not found.",
  "detail": "The path passed to the documentation generator does not point to an existing solution file.",
  "code": "GENDOC_SOLUTION_NOT_FOUND"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationRequest] Solution file not found: '/src/app/Application.sln'. error.code=GENDOC_SOLUTION_NOT_FOUND
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `SolutionPath` | `System.String` | Full path of the solution file the generation request designates. | `/src/app/Application.sln` |

