# FirstClassErrors analyzers

Roslyn analyzers that catch, at build time, the mistakes the FirstClassErrors runtime and documentation
pipeline would otherwise only surface later (or silently). They ship **inside the `FirstClassErrors` NuGet
package** — any project that references it gets the rules automatically, no extra install.

Each rule has a stable id `FCExxx`. Errors are hard defects; warnings flag likely mistakes; the info rules
are conventions, and several are opt-in (see each page for how to enable them).

## Error codes

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE001 DuplicateErrorCode](FCE001.md) | 🔴 Error | on | The same literal error code is created by more than one ErrorCode.Create("...") in the compilation. |
| [FCE002 EmptyErrorCode](FCE002.md) | 🔴 Error | on | ErrorCode.Create is called with an empty, whitespace, or null literal. |
| [FCE003 NonLiteralErrorCode](FCE003.md) | 🔵 Info | opt-in | ErrorCode.Create is called with an argument that is not a compile-time constant. |
| [FCE004 InvalidErrorCodeFormat](FCE004.md) | 🔵 Info | opt-in | A literal error code does not follow the UPPER_SNAKE_CASE convention. |
| [FCE005 TooGenericErrorCode](FCE005.md) | 🔵 Info | opt-in | A literal error code is one of a small set of catch-all words (ERROR, INVALID, FAILED, ...) that carry no diagnostic value. |

## Documentation wiring

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE006 DocumentedByTargetNotFound](FCE006.md) | 🔴 Error | on | A [DocumentedBy("...")] names a documentation method that does not exist on the containing type. |
| [FCE007 DocumentedByInvalidSignature](FCE007.md) | 🔴 Error | on | The method referenced by [DocumentedBy] exists but cannot be used as a documentation factory. |
| [FCE008 DocumentedByWithoutProvidesErrorsFor](FCE008.md) | 🔴 Error | on | A type declares [DocumentedBy] factories but is missing [ProvidesErrorsFor]. |
| [FCE009 ErrorFactoryNotDocumented](FCE009.md) | 🟠 Warning | on | A non-private static factory that returns an Error in a [ProvidesErrorsFor] type carries no [DocumentedBy]. |
| [FCE010 MultipleFactoriesShareDocumentation](FCE010.md) | 🟠 Warning | on | Two or more factories in the same type point [DocumentedBy] at the same documentation method. |

## Documentation content

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE011 DuplicateDocumentedCode](FCE011.md) | 🔴 Error | on | More than one documented factory produces the same error code by referencing the same ErrorCode field. |
| [FCE012 EmptyExamples](FCE012.md) | 🟠 Warning | on | The terminal WithExamples() call of the documentation DSL is given no example factory. |
| [FCE013 ExampleDoesNotCallDocumentedFactory](FCE013.md) | 🟠 Warning | on | An example passed to WithExamples(...) does not invoke any factory of the type that declares the documentation. |
| [FCE014 ShortMessageSameAsDetailedMessage](FCE014.md) | 🔵 Info | on | WithPublicMessage(short, detailed) is called with two identical literal messages. |
| [FCE015 DocumentationTitleTooGeneric](FCE015.md) | 🔵 Info | opt-in | A WithTitle("...") uses a title that describes nothing (Error, Invalid value, Failure, ...). |

## Usage

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE016 UnusedToExceptionResult](FCE016.md) | 🟠 Warning | on | Error.ToException() is called as a standalone statement and its result is discarded. |

## Configuring

Every rule's severity can be tuned in `.editorconfig`, for example:

```ini
# turn an opt-in rule on
dotnet_diagnostic.FCE004.severity = warning

# or silence a rule you do not want
dotnet_diagnostic.FCE014.severity = none
```

> `FCE001` and `FCE011` are whole-compilation checks: they appear at build / full-solution analysis rather
> than as you type in a single file.
