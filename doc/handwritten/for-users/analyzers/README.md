# Analyzers

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./README.fr.md)

This repository ships Roslyn rules with two packages. They run while your project compiles, turning mistakes that the runtime and documentation pipeline would otherwise report late — or never at all — into build-time diagnostics. The **FirstClassErrors** rules (`FCExxx`) ship inside the `FirstClassErrors` package; the **JustDummies** rules (`JDxxx`) ship inside the `JustDummies` package. Any project that references a package picks up its rules automatically, with no extra install.

Each rule has a stable id (`FCExxx` or `JDxxx`). Errors are hard defects; warnings flag likely mistakes; the info rules are conventions, and several are opt-in (see each page for how to enable them).

## Error codes

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE001 DuplicateErrorCode](FCE001.en.md) | 🔴 Error | on | The same literal error code is created by more than one ErrorCode.Create("...") in the compilation. |
| [FCE002 EmptyErrorCode](FCE002.en.md) | 🔴 Error | on | ErrorCode.Create is called with an empty, whitespace, or null literal. |
| [FCE003 NonLiteralErrorCode](FCE003.en.md) | 🔵 Info | opt-in | ErrorCode.Create is called with an argument that is not a compile-time constant. |
| [FCE004 InvalidErrorCodeFormat](FCE004.en.md) | 🔵 Info | opt-in | A literal error code does not follow the UPPER_SNAKE_CASE convention. |
| [FCE005 TooGenericErrorCode](FCE005.en.md) | 🔵 Info | opt-in | A literal error code is one of a small set of catch-all words (ERROR, INVALID, FAILED, ...) that carry no diagnostic value. |

## Documentation wiring

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE006 DocumentedByTargetNotFound](FCE006.en.md) | 🔴 Error | on | A [DocumentedBy("...")] names a documentation method that does not exist on the containing type. |
| [FCE007 DocumentedByInvalidSignature](FCE007.en.md) | 🔴 Error | on | The method referenced by [DocumentedBy] exists but cannot be used as a documentation factory. |
| [FCE008 DocumentedByWithoutProvidesErrorsFor](FCE008.en.md) | 🔴 Error | on | A type declares [DocumentedBy] factories but is missing [ProvidesErrorsFor]. |
| [FCE009 ErrorFactoryNotDocumented](FCE009.en.md) | 🟠 Warning | on | A non-private static factory that returns an Error in a [ProvidesErrorsFor] type carries no [DocumentedBy]. |
| [FCE010 MultipleFactoriesShareDocumentation](FCE010.en.md) | 🟠 Warning | on | Two or more factories in the same type point [DocumentedBy] at the same documentation method. |

## Documentation content

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE011 DuplicateDocumentedCode](FCE011.en.md) | 🔴 Error | on | More than one documented factory produces the same error code by referencing the same ErrorCode field. |
| [FCE012 EmptyExamples](FCE012.en.md) | 🟠 Warning | on | The terminal WithExamples() call of the documentation DSL is given no example factory. |
| [FCE013 ExampleDoesNotCallDocumentedFactory](FCE013.en.md) | 🟠 Warning | on | An example passed to WithExamples(...) does not invoke any factory of the type that declares the documentation. |
| [FCE014 ShortMessageSameAsDetailedMessage](FCE014.en.md) | 🔵 Info | on | WithPublicMessage(short, detailed) is called with two identical literal messages. |
| [FCE015 DocumentationTitleTooGeneric](FCE015.en.md) | 🔵 Info | opt-in | A WithTitle("...") uses a title that describes nothing (Error, Invalid value, Failure, ...). |

## Usage

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [FCE016 UnusedToExceptionResult](FCE016.en.md) | 🟠 Warning | on | Error.ToException() is called as a standalone statement, or its result is explicitly discarded with `_ =`. |
| [FCE017 SensitiveDataInErrorContext](FCE017.en.md) | 🟠 Warning | opt-in | An ErrorContextKey name denotes a secret, credential, or personal data (password, token, secret, connection string, credit card, ...). |
| [FCE018 OversizedErrorContextValue](FCE018.en.md) | 🔵 Info | opt-in | An ErrorContextKey value type is a bulk payload (byte array, Stream, or FileInfo) that does not belong in a loggable context. |
| [FCE019 TryCatchesTooBroadly](FCE019.en.md) | 🟠 Warning | on | Outcome.Try catches System.Exception, turning unexpected bugs into anticipated errors instead of the single exception the operation is expected to throw. |
| [FCE020 TryCatchesRichProtocolException](FCE020.en.md) | 🟠 Warning | opt-in | Outcome.Try catches a protocol failure (HttpRequestException, DbException, SocketException, ...) whose status or result data is lost when reduced to a throw. |
| [FCE021 PreferNonThrowingAlternativeToTry](FCE021.en.md) | 🟠 Warning | on | Outcome.Try wraps a call that already has a non-throwing TryXxx / TryCreate counterpart available for the target framework; consider mapping its result (advisory — suppress where the counterpart is not a true inverse). |
| [FCE022 TryCatchesCancellation](FCE022.en.md) | 🟠 Warning | on | Outcome.Try binds TException to OperationCanceledException (or a subtype); Try always lets cancellation propagate, so the catch is unreachable and the mapper never runs. |

## JustDummies — Reproducibility

These rules ship in the **`JustDummies`** package (not FirstClassErrors) and keep an asynchronous test body from silently swallowing its own failures.

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [JD001 AsyncBodyPassedToReproducibly](JD001.en.md) | 🔴 Error | on | An async lambda is passed to the synchronous Any.Reproducibly(Action); bound to an Action it becomes async void and its failures never fail the test. Use Any.ReproduciblyAsync and await it. |
| [JD002 DiscardedReproduciblyAsyncResult](JD002.en.md) | 🔴 Error | on | The task returned by Any.ReproduciblyAsync is discarded (a bare statement, or `_ =`); the body's failures are lost. Await it. |
| [JD003 AwaitableBodyPassedToReproducibly](JD003.en.md) | 🔴 Error | on | A synchronous lambda whose body drops a task, or an async void method group, reaches Any.Reproducibly; the scope returns before the assertions run, and CS4014 does not fire. |
| [JD004 DiscardedSeedingResult](JD004.en.md) | 🔴 Error | on | The handle returned by Any.UseSeed is discarded, leaving the seed pinned for whatever runs next — or Any.WithSeed is called for effect, which pins nothing at all. |
| [JD007 DrawOutsideThePinnedScope](JD007.en.md) | 🟠 Warning | on | A value is drawn during a [Reproducible] test class's construction, which xUnit runs before the seed scope opens; the reported seed does not replay it. |
| [JD008 ArbitraryValueInTheoryData](JD008.en.md) | 🟠 Warning | on | A theory's data provider draws a value at discovery, before any seed is pinned; every case shares the one value. |
| [JD009 DrawInStaticInitializer](JD009.en.md) | 🟠 Warning | on | A static initializer draws once for the whole suite, under whichever test ran first, making the tests order-dependent and replayable from no seed. |
| [JD010 ReproducibleOnNonTestMethod](JD010.en.md) | 🟠 Warning | on | [Reproducible] on a method xUnit never treats as a test; it pins nothing, and looks exactly like the working form. |

## JustDummies — Usage

A generator is an immutable *recipe*, and `Generate()` is the only thing that materializes a value from it. These rules close the two ways that distinction is lost silently.

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [JD005 GeneratorRenderedAsText](JD005.en.md) | 🔴 Error | on | A generator is interpolated, concatenated or ToString()'d instead of generated from; no generator overrides ToString(), so the text is the builder's type name. |
| [JD006 DiscardedGeneratorResult](JD006.en.md) | 🟠 Warning | on | The generator returned by a constraint is discarded as a bare statement; generators are immutable, so the declared invariant is silently lost. |

## Configuring

Every rule's severity can be tuned in `.editorconfig`, for example:

```ini
# turn an opt-in rule on
dotnet_diagnostic.FCE004.severity = warning

# or silence a rule you do not want
dotnet_diagnostic.FCE014.severity = none
```

> `FCE001` and `FCE011` are whole-compilation checks: they appear at build / full-solution analysis rather than as you type in a single file.
