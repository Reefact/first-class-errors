# FirstClassErrors

**Treat errors as first-class, documented, and diagnosable concepts — carried as
values or thrown as exceptions — so failures stay explicit and easy to support in
production.**

Define each error once, in code: a stable code, the messages its two audiences are
allowed to see, its diagnostics, and its documentation. That single definition is
checked at build time and can be extracted into a living error catalog.

## What you get

- **Errors as values *or* exceptions.** Carry an error as data with `Outcome` /
  `Outcome<T>`, or throw it with `.ToException()` — same model both ways. Pick per
  context (domain logic, validation, pipelines) instead of committing everything to
  one style.
- **A message model that can't leak internals.** Each error separates a public
  `ShortMessage` (+ optional public `DetailedMessage`) from an internal
  `DiagnosticMessage`. The split is enforced by construction, so support/developer
  detail never reaches an API client by accident. Maps onto RFC 9457 problem details
  (`Code` → `type`, `ShortMessage` → `title`, `DetailedMessage` → `detail`).
- **Structured diagnostics.** Each error can declare its likely causes and where to
  start investigating — guidance that travels with the error instead of living in
  someone's head.
- **Living documentation, generated from code.** Document errors with a small fluent
  DSL, then extract the whole catalog as **Markdown / HTML / JSON** (custom renderers,
  opt-in per project, multi-language) with the companion `fce` tool. Run it in CI so
  the catalog is published as a build artifact and never drifts from the deployed
  system. See *Generate the catalog in CI* below.
- **16 Roslyn analyzers in the box (`FCE001`–`FCE016`).** Bundled in the package — no
  separate install. They catch, at build time, what would otherwise surface late:
  duplicate error codes, unresolved `[DocumentedBy]` references, documented errors
  missing from the catalog, an unused `ToException()` result, and more.
- **Zero runtime dependencies, .NET Standard 2.0.** Runs on .NET Framework 4.7.2+,
  .NET Core 2.0+, .NET 5+ (and Mono / Xamarin / Unity). Nothing added to your
  dependency graph.

## Example

    [ProvidesErrorsFor(nameof(Temperature))]
    public static class InvalidTemperatureError {

        [DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
        internal static DomainError BelowAbsoluteZero(decimal value, TemperatureUnit unit) =>
            DomainError.Create(
                    Code.TemperatureBelowAbsoluteZero,
                    diagnosticMessage: $"Failed to instantiate temperature: {value} {unit} is below absolute zero.")
                .WithPublicMessage(
                    shortMessage: "Temperature is invalid.",
                    detailedMessage: $"The temperature {value} {unit} is below absolute zero.");

        private static ErrorDocumentation BelowAbsoluteZeroDocumentation() =>
            DescribeError.WithTitle("Temperature below absolute zero")
                         .WithDescription("Occurs when instantiating a temperature below absolute zero.")
                         .WithRule("Absolute zero is the point of minimum possible energy; nothing goes below it.")
                         .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                         .WithExamples(() => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin));
    }

The error, its rule, its diagnostics, and its examples are defined together — and that
same definition is what the analyzers check and the documentation pipeline extracts.

## Installation

    dotnet add package FirstClassErrors

Analyzers are included. Loading them requires .NET 8 SDK / Visual Studio 2022 17.8+.
Each release ships with signed build provenance (SLSA) and an embedded SBOM.

## Generate the catalog in CI

The documentation generator ships as a .NET tool,
[`FirstClassErrors.Cli`](https://www.nuget.org/packages/FirstClassErrors.Cli), so a
pipeline can produce the error catalog as a build artifact:

    dotnet tool install --global FirstClassErrors.Cli
    dotnet build MyApp.sln -c Release
    fce generate --solution MyApp.sln --no-build \
                 --output artifacts/errors.md --format markdown --service-name my-api

Only projects whose `.csproj` sets
`<GenerateErrorDocumentation>true</GenerateErrorDocumentation>` are scanned.

## Companion package

**FirstClassErrors.Testing** adds framework-agnostic assertions on `Outcome` / `Error`
(`ShouldSucceed()`, `ShouldFail().WithCode(...)`) plus a freezable clock and instance
ids for deterministic tests.

## Documentation

Full guides, the analyzer reference, and the CI/CD integration guide on GitHub:
https://github.com/Reefact/first-class-errors
