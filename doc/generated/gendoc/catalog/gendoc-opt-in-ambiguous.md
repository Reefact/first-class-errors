# Documentation opt-in ambiguous

- **Code:** `GENDOC_OPT_IN_AMBIGUOUS`
- **Source:** `DocumentationRequest`

The generator reads the documentation opt-in property literally from the project XML, without MSBuild evaluation. When the property is defined more than once, or gated behind an MSBuild Condition (directly or via a Choose/When branch), its effective value cannot be known, so the generator refuses to guess and skips the project. The project path, the property name, and the reason are carried in the error context.

> **Business rule:** The opt-in property must be declared at most once, literally and unconditionally, in each project file.

## Diagnostics

- **The property is declared in several PropertyGroups, or under a Condition attribute or a Choose/When branch.** — _origin:_ External — Inspect the project file named in the error context; keep a single unconditional declaration, or document the built assembly explicitly instead.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:gendoc:gendoc-opt-in-ambiguous",
  "title": "A project's documentation opt-in is ambiguous.",
  "detail": "Declare the opt-in property once, literally and unconditionally, in the project file — or document the assembly explicitly.",
  "code": "GENDOC_OPT_IN_AMBIGUOUS"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [DocumentationRequest] Cannot determine the opt-in for project '/src/app/Application/Application.csproj': the 'GenerateErrorDocumentation' property is defined 2 times in the project XML, which GenDoc reads without MSBuild evaluation. Declare it once, literally and unconditionally in the project file, or document the built assembly explicitly. error.code=GENDOC_OPT_IN_AMBIGUOUS
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `ProjectPath` | `System.String` | Full path of the project file being processed. | `/src/app/Application/Application.csproj` |
| `OptInProperty` | `System.String` | Name of the MSBuild property read as the documentation opt-in marker. | `GenerateErrorDocumentation` |
| `AmbiguityReason` | `System.String` | Why the opt-in property's effective value cannot be determined from the project XML. | `defined 2 times` |

