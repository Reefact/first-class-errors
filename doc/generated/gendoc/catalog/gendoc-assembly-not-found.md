# Requested assembly not found

- **Code:** `GENDOC_ASSEMBLY_NOT_FOUND`
- **Source:** `DocumentationRequest`

A documentation-generation request explicitly designates an assembly that does not exist on disk. The full path, as resolved by the generator, is carried in the error context.

> **Business rule:** Every assembly explicitly designated by a generation request must exist on disk.

## Diagnostics

- **The assembly was never built, or was built to a different configuration or target framework than the path assumes.** — _origin:_ External — Build the project first and compare the path in the error context with the actual build output directory.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-assembly-not-found",
  "title": "A requested assembly was not found.",
  "detail": "One of the assemblies passed to the documentation generator does not exist on disk.",
  "code": "GENDOC_ASSEMBLY_NOT_FOUND"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationRequest] Assembly not found: '/src/app/bin/Release/net8.0/Application.dll'. error.code=GENDOC_ASSEMBLY_NOT_FOUND
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `AssemblyPath` | `System.String` | Full path of the assembly being documented. | `/src/app/bin/Release/net8.0/Application.dll` |

