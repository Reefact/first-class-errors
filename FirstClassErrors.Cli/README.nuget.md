# FirstClassErrors.Cli (`fce`)

`fce` is the command-line **documentation generator** for
[FirstClassErrors](https://www.nuget.org/packages/FirstClassErrors). It scans a
solution — or already-built assemblies — for the errors you documented in code and
renders the whole catalog as **Markdown, HTML, or JSON**.

It exists for CI/CD: run it as a build step so your error catalog is published as an
artifact and always matches the deployed system, with no manual upkeep.

## Install

    dotnet tool install --global FirstClassErrors.Cli

This installs the `fce` command and requires the .NET 10 runtime. Use `--global` for a
machine-wide tool, or install it into a
[tool manifest](https://learn.microsoft.com/dotnet/core/tools/local-tools-how-to-use)
for a version-pinned, per-repository tool.

## Use

Document a solution and write a Markdown catalog:

    fce generate --solution MyApp.sln --output docs/errors.md --format markdown --service-name my-api

Only projects whose `.csproj` sets
`<GenerateErrorDocumentation>true</GenerateErrorDocumentation>` are scanned. The
`--service-name` option is required for the `markdown` and `html` formats — it forms the
RFC 9457 problem `type` of rendered examples (`urn:problem:{service}:{code}`); the `json`
format does not need it.

Other commands:

- `fce config init` — create an `fce.json` so options need not be repeated on every run
- `fce config show` — print the resolved configuration
- `fce renderer add|list|remove` — manage custom renderer libraries

Run `fce --help` or `fce generate --help` for the full option list.

## In CI

    dotnet tool install --global FirstClassErrors.Cli
    dotnet build MyApp.sln -c Release
    fce generate --solution MyApp.sln --no-build \
                 --output artifacts/errors.md --format markdown --service-name my-api
    # then publish artifacts/errors.md as a pipeline artifact or to a docs portal

Emit one catalog per locale by adding `--language` (e.g. a matrix over `en`, `fr`); file
names and anchors stay stable across languages.

## Compatibility with FirstClassErrors

`fce` reads the error documentation you author with FirstClassErrors' attributes and the
`DescribeError` DSL — the *documentation contract* — and is versioned **independently** of
the library:

- a **minor** `fce` release adds support for a new FirstClassErrors contract, or a new
  option or output format; a **major** `fce` release changes `fce`'s own command-line
  surface or drops support for an older contract.
- at run time `fce` documents your solution against **its** FirstClassErrors version (the
  worker binds to your target's dependency closure), so what matters is that your `fce`
  understands the contract the library version you use produces.

While FirstClassErrors is in **0.x preview** that contract may still change between minor
versions — use an `fce` from the same preview line as the FirstClassErrors version you
document. From FirstClassErrors 1.0 on, `fce` will state the contract version(s) it
supports and stop with a clear message rather than mis-read a newer, unsupported one.

## Documentation

Full CI/CD integration guide and the rest of the documentation on GitHub:
https://github.com/Reefact/first-class-errors
