# Request Binder contract specification

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](request-binder.fr.md)

This page describes the current mechanics behind the Request Binder decisions.
The public user-facing examples remain in the
[Request Binder guide](../../for-users/RequestBinder.en.md).

## Entry and lifetime

* `Bind.Request(envelopeFactory)` creates a source-agnostic `RequestBinder` and
  fixes the failure envelope before any input is attached.
* `Bind.WithOptions(options)` creates a reusable configured entry point; options
  are fixed before binding starts.
* The bare entry uses `RequestBinderOptions.Default`, which may be configured once
  at application startup and freezes on first production read.
* A DTO is attached with `PropertiesOf(request)` and out-of-DTO values are attached
  independently with `Argument(...)`; both feed the same binder and envelope.
* The command type is inferred only by the `New` or `Create` terminal.

## Input sources

### DTO properties

A `PropertySource<TRequest>` selects simple, list, or complex properties. Property
paths are derived through the configured `IArgumentNameProvider` and nested paths
are composed by the binder.

### Out-of-DTO arguments

An argument is an already-extracted host value. The caller supplies:

* the wire name, used verbatim as the error path;
* its provenance, normally through helpers such as `FromRoute` or `FromQuery`, or
  through the general `From` form;
* the raw value and the same converter shape used for a property.

The binder does not inspect an HTTP request or any framework object. Provenance is
stored separately from the path in typed error context. Complex out-of-DTO
arguments are not a separate concept: bind their constituent values as peer
arguments and assemble the complex value at the terminal.

## Nullable value-type selectors

A nullable value-type DTO property is selected through a dedicated
`where TArgument : struct` overload whose expression type is
`Nullable<TArgument>`. The converter receives the non-nullable underlying value.
This keeps method-group conversion identical to reference properties while
preserving `null` as the absence signal.

Lists of nullable value types use the corresponding dedicated path. A `null`
element is recorded as a missing argument at its indexed path; a present element
is unwrapped before conversion. Non-nullable value-type DTO properties remain a
programming error because absence cannot be distinguished from `default(T)`.

## Binding result model

Converters return field tokens. Values are readable only through the binding
scope supplied to `New` or `Create`, after the binder has confirmed that no input
failure was recorded.

* `New` runs a total constructor and wraps the value in success.
* `Create` runs a validating factory returning `Outcome<T>` and flattens it.
* Missing or malformed inputs are collected in declaration order under the one
  structural envelope; the invalid-input path does not throw.

## Structural errors and catalog ownership

The binder's required-argument and invalid-argument definitions are bundled as
`BinderErrorDefinition` values on `RequestBinderOptions`, including code and
public messages.

When a consumer overrides one of these definitions, the consumer owns the
effective emitted code and documents it in its own `[ProvidesErrorsFor]` type.
Public binder documentation seams provide:

* the code-independent prose for the structural error; and
* a sample built from the consumer's effective definition with the same shape as
  the runtime error.

GenDoc continues to scan the consumer's opted-in projects; it does not
cross-scan referenced package catalogs or infer runtime configuration.

## Sources of truth

* `FirstClassErrors.RequestBinder/Bind.cs` and the binder source/converter types —
  public API shape.
* `RequestBinderOptions` and `BinderErrorDefinition` — configuration and structural
  errors.
* Request Binder unit and property tests — overload resolution, path construction,
  collection order, option freezing, and collect-all behaviour.
* The [Request Binder guide](../../for-users/RequestBinder.en.md) — supported public
  usage and examples.

A change to source shape, option lifetime, structural-code ownership, or terminal
semantics requires an ADR. A refactoring that preserves those contracts updates
this specification and tests only.
