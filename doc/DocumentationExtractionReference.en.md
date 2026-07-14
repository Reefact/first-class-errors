# Extraction and Project Discovery Reference

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./DocumentationExtractionReference.fr.md)

This page is the operational reference for selecting projects and assemblies, running extraction workers, and handling failures. For the mental model first, read [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md).

## Solution mode

The common CLI path starts from a solution:

```bash
fce generate --solution ./MyApp.sln --format markdown --service-name my-api --output ./docs/errors
```

At a high level, solution mode:

1. builds the whole solution unless `--no-build` is set;
2. lists projects through `dotnet sln list`;
3. selects projects according to the opt-in marker;
4. locates their output assemblies;
5. launches one extraction worker per assembly;
6. aggregates documentation and failures.

The build runs on the solution itself, before project selection: a compile error in a project that never opted in still fails the run.

## Project opt-in

A project participates when its own `.csproj` contains:

```xml
<PropertyGroup>
  <GenerateErrorDocumentation>true</GenerateErrorDocumentation>
</PropertyGroup>
```

The marker is read directly from the project XML. It is not evaluated as a normal MSBuild property.

| Declaration | Result |
| --- | --- |
| `true` once, unconditionally | project is included |
| absent | project is skipped |
| `false` | project is skipped |
| declared more than once | ambiguous and reported |
| declared under `Condition` | ambiguous and reported |

Important consequences:

- declaring the value only in `Directory.Build.props` does not opt the project in;
- importing the property from another file does not opt the project in;
- passing `-p:GenerateErrorDocumentation=true` to `dotnet build` does not opt the project in;
- the marker must be literal and unambiguous in the `.csproj` itself.

Under a continue-on-failure policy, ambiguous projects are reported and skipped. Under a strict policy, they fail generation.

## Programmatic opt-in options

`SolutionGenerationOptions` allows programmatic callers to change the defaults:

- `OptInPropertyName` changes the marker name;
- `IncludeProjectsWithoutOptIn` includes projects without the marker.

The `fce` CLI uses `GenerateErrorDocumentation` and the opt-in behavior described above.

## Assembly mode

Use pre-built assemblies when solution discovery or building should not be part of the run:

```bash
fce generate \
  --assemblies ./artifacts/MyApp.Domain.dll \
  --assemblies ./artifacts/MyApp.Application.dll \
  --format json \
  --output ./artifacts/errors.json
```

`--assemblies` takes one path per occurrence; repeat the option for each assembly.

Assembly mode documents exactly the binaries supplied. It does not apply the `.csproj` opt-in filter.

Use it when:

- another pipeline stage already built the application;
- assemblies come from different solutions;
- the caller needs exact binary selection;
- project discovery would be inappropriate.

The caller remains responsible for providing compatible dependency files and runtime assets beside the target assembly.

## Single-assembly extraction

`AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly)` performs in-process extraction for one already loaded assembly.

It:

- finds `[ProvidesErrorsFor]` classes;
- resolves methods referenced by `[DocumentedBy]`;
- invokes documentation methods and example factories;
- returns an `ErrorDocumentationExtractionResult` containing documentation and failures;
- deduplicates and orders documentation by error code.

This low-level API is useful for controlled tooling and tests. Solution-level generation normally uses isolated workers instead.

## Worker execution

Each selected assembly is extracted in a short-lived worker process. The generator launches the worker with the target assembly's dependency context so its application dependencies and FirstClassErrors version resolve independently.

The worker:

1. loads the target assembly;
2. runs extraction;
3. serializes the complete extraction result as JSON;
4. exits.

The generator reads that result and continues with the next assembly.

## Why workers are required

Documentation methods and example factories are executable code. They may:

- initialize static state;
- load application dependencies;
- use a different FirstClassErrors version;
- throw during execution;
- crash the process;
- hang indefinitely.

Per-assembly workers isolate those risks. A failure remains associated with the assembly that produced it.

## Failures and continuation

Extraction failures are data, not necessarily immediate process crashes.

Failures reported by a worker that completes normally are always recorded and logged, and generation continues with the remaining assemblies regardless of the configured failure behavior:

- a `[DocumentedBy]` target cannot be found;
- the target has an invalid signature;
- a documentation method throws;
- an example factory throws.

Process-level failures honor the configured failure behavior, which determines whether the generator records the problem and continues with other assemblies, or treats the problem as fatal:

- an assembly cannot be loaded;
- the worker exits unexpectedly;
- the worker exceeds its timeout.

A continued run can therefore produce a partial catalog plus explicit failures. Consumers must not mistake “a file was generated” for “every assembly was documented successfully.”

## Timeouts and process failures

A worker that does not complete within its configured timeout is terminated and recorded as failed. A worker crash is also recorded with the available process information.

When investigating a timeout:

1. run the documented factory or example directly in a test;
2. check for blocking I/O, deadlocks, or environment-dependent initialization;
3. confirm the target's runtime and dependency files are available;
4. avoid network or production-service access in documentation factories;
5. make example factories small and deterministic.

Documentation code should construct representative errors, not perform real application workflows.

## Building and `--no-build`

In solution mode, the generator builds the solution by default. Use `--no-build` only when the expected outputs already exist and match the current source.

```bash
fce generate --solution ./MyApp.sln --no-build --format markdown --service-name my-api --output ./docs/errors
```

A safe CI sequence is:

```bash
dotnet build MyApp.sln -c Release
fce generate --solution MyApp.sln --configuration Release --no-build --format markdown --service-name my-api --output artifacts/errors
```

If `--no-build` points to stale or missing outputs, extraction may document old code or fail to locate assemblies.

## Configuration and framework selection

The selected configuration and target framework must identify a real output for each participating project. Multi-targeted projects may require an explicit framework.

Keep the CLI configuration aligned with the build that produced the assemblies:

```bash
fce generate \
  --solution ./MyApp.sln \
  --configuration Release \
  --framework net8.0 \
  --no-build \
  --output ./artifacts/errors
```

## Failure-safe documentation factories

A documentation method should be:

- deterministic;
- fast;
- free of external I/O;
- independent of environment secrets;
- safe to execute repeatedly;
- limited to constructing documentation and representative errors.

Avoid:

- database calls;
- HTTP calls;
- reading mutable production configuration;
- reliance on current time or randomness when it affects output;
- starting background work;
- modifying global application state.

## Troubleshooting checklist

When expected errors are missing, verify:

- the project has a literal `<GenerateErrorDocumentation>true</GenerateErrorDocumentation>` in its `.csproj`;
- the factory class has `[ProvidesErrorsFor]`;
- the factory has `[DocumentedBy]`;
- the referenced method exists and has a valid documentation-factory signature;
- the documentation method and example factories complete successfully;
- the intended configuration and framework were built;
- `--no-build` is not reusing stale outputs;
- worker failures and warnings were reviewed;
- assembly-mode paths point to the intended binaries.

---

<div align="center">
<a href="ArchitectureOfTheDocumentationPipeline.en.md">← Architecture of the Documentation Pipeline</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="WritingACustomRenderer.en.md">Writing a custom renderer →</a>
</div>

---