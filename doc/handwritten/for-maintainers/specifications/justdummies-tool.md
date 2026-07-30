# JustDummies tool (`dum`) — specification v1.0

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](justdummies-tool.fr.md)

**Status:** specification, ready to implement. Nothing is built yet.
**Supersedes:** the working pre-specification 0.1 (never committed)

---

## 0. How to read this document

This specification is **self-contained on purpose**. JustDummies is expected to move to its own
repository before the tool is built, so nothing here may depend on being read inside
`Reefact/first-class-errors`.

* **§1–§9 are the product.** What the tool does, what it emits, and why. Read §2 first: eleven
  decisions carry everything else. §5 is the hard part and the only section with real design risk.
* **§10–§12 are the build.** Two projects, the contract between them, and the test plan.
* **§13 is the portability contract.** Everything the tool needs *from its host repository*,
  stated as requirements rather than paths. If JustDummies has moved, start here.
* **§14 is the reference.** Every fact about the JustDummies library that this specification
  relies on, inlined, with the command to re-derive each one. Nothing in §1–§12 requires reading
  the library's source to be checked.
* **§17 is the evidence.** The emitted skeleton of §4.1 was compiled and run against the real
  library, and the two contested claims were measured. §17.2 says how to re-run all of it.

Everything in this document is **decided** unless it appears in §16 (deferred) or is explicitly
marked open. There are no open questions blocking implementation.

---

## 1. What `dum` is

`dum` is a **scaffolder**, not a code generator.

Given a type from the developer's own code, it writes **one C# file, once**, containing a named,
composable generator for that type. From the moment the file is written it belongs to the
developer: they read it, edit it, commit it, and never run the tool on it again.

```console
$ cd Shop.Tests
$ dum generate Order
✓ AnyOrder.cs
```

```csharp
Order order = new AnyOrder()
    .WithStatus(OrderStatus.Pending)
    .Generate();
```

The distinction from a *generator* is the whole product position and it settles most of the
design at once:

* there is no drift, because there is nothing to keep in sync — the file is the developer's, not
  the tool's;
* there is therefore **no `check` verb, no source generator, no regeneration story**;
* the tool is allowed to leave the file **unfinished**, because finishing it is the developer's
  half of the deal.

The value proposition stays distinct from the library's: the **library** makes values valid; the
**tool** makes the test concise.

### 1.1 Design rules this specification answers to

1. **Extremely simple to use.** The nominal invocation is one verb and one type name, from the
   directory the file will land in, with no configuration file and no options.
2. **Cheap at both ends.** Nothing to configure before the first use; nothing to configure per
   use.
3. **Generate as much as can be generated, and no more.** Where the tool cannot know, it says so
   in the file and in the console, and hands the skeleton back.
4. **Naming is fixed in v1.0.** `Order` becomes `AnyOrder`, full stop. Renaming
   (`OrderFactory`, a custom prefix) is v1.1+ and §16 reserves its shape so v1.0 does not block
   it.

---

## 2. Decisions

These are the load-bearing decisions. §15 lists the ADRs to draft for them.

| # | Decision | Why, in one line |
|---|---|---|
| **D1** | Scaffold once; the file belongs to the developer. | Kills drift, `check`, and the source-generator question in one move. |
| **D2** | The emitted type implements `IAny<T>` and is **immutable**. | Composability, and it re-arms the `JustDummies.Usage` analyzers on the emitted type. |
| **D3** | The emitted file is **not** marked as generated code. | All 27 analyzers exempt generated code; marking it would blind the file. |
| **D4** | Never emit a member not resolved in the target compilation. | One rule covers the TFM split, the public-API baseline, version skew and unsigned arithmetic. |
| **D5** | Read constructor guard clauses to seed each generator. | Without it the emitted code produces values the constructor rejects. |
| **D6** | An unresolved parameter is emitted as a **compile error**. | The developer is already in the file; a red squiggle is the cheapest possible signal. |
| **D7** | The emitted generator draws from the **ambient** context only. | `AnyContext` support costs API surface for a case `.WithX(IAny<T>)` already covers. |
| **D8** | The emitted type lives in the **target type's namespace**. | The test already has that `using`; `new AnyOrder()` just works. |
| **D9** | The tool takes **no dependency on the JustDummies package**. | Resolution by metadata name, exactly like the analyzers — version skew becomes structurally impossible. |
| **D10** | Never emit `.OrNull()`. | A dummy that is randomly `null` is the flakiness the library exists to remove. |
| **D11** | The scaffolding **engine is a separate library** at the Roslyn floor; the CLI is a shell. | The engine's plausible second consumer is an IDE refactoring, which is not a CLI and cannot load a `net8.0` assembly. |

### 2.1 D3 in detail — why the file is not marked generated

Every one of the 27 analyzers in the JustDummies analyzer package calls
`ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` (§14.6). A file carrying an
`<auto-generated/>` header, or named `*.g.cs` / `*.generated.cs`, is therefore **entirely exempt
from every JD diagnostic**.

This was measured, not reasoned about. One file containing exactly two violations — a `JD006`
warning (a discarded constraint) and a `JD005` **error** (a generator rendered as text) — was
compiled twice against the built analyzers, changing nothing but its first line:

| First line of the file | Diagnostics reported | Build |
|---|---|---|
| *(none)* | `JD006` warning, `JD005` error | fails |
| `// <auto-generated/>` | **none** | succeeds |

A compile error became silence. Marking the scaffolded file would create the only file in the
developer's test project that the JustDummies safety net does not cover — and it is the file most
likely to contain a JustDummies mistake, since the developer is about to edit it.

So: a plain header comment, an ordinary `.cs` name, no `GeneratedCodeAttribute`. The file is
analyzed like any other. This pays off directly: a guard-derived chain the tool gets wrong
(§5.3) surfaces as `JD015`/`JD023` in the developer's IDE instead of as a runtime
`ConflictingAnyConstraintException`.

### 2.2 D2 in detail — why `IAny<T>` is not optional

The analyzers recognise a generator as *the `IAny<T>` interface itself, or any type implementing
it* (§14.6). Making `AnyOrder` an `IAny<Order>` therefore does two things:

* it becomes consumable by `Any.Combine`, `.As(...)`, `Any.ListOf(...)`, `Any.OneOf(...)` and
  every reproducibility scope, with no extra code — nested aggregates compose for free;
* the whole `JustDummies.Usage` category (`JD005`, `JD006`, `JD012`, `JD013`) starts recognising
  `AnyOrder` as a recipe, so `Assert.Equal("x", new AnyOrder())` or a discarded
  `new AnyOrder().Generate()` is reported exactly as it would be for `Any.String()`.

Immutability is not a style preference: `IAny<T>` is documented as *"an immutable recipe […] each
fluent constraint returns a new generator"*, and every generator in the library honours it
(§14.5). A `.WithX()` returning `this` would make `AnyOrder` the only mutable generator in the
ecosystem and would break this:

```csharp
AnyOrder baseOrder = new AnyOrder().WithCustomer(customer);
AnyOrder pending   = baseOrder.WithStatus(OrderStatus.Pending);
AnyOrder shipped   = baseOrder.WithStatus(OrderStatus.Shipped);  // must not disturb `pending`
```

### 2.3 D4 in detail — resolve, then emit

The engine holds a `Compilation` for the developer's project. Before emitting any JustDummies
member, it **looks that member up in that compilation** (`GetTypeByMetadataName("JustDummies.Any")`,
then a member lookup). If the lookup fails, the member is not emitted and the parameter falls
through to the unresolved path (§5.5).

One rule, five problems solved:

* **The TFM split.** `DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half` exist only on the
  `net8.0` asset (§14.1). A test project resolving the `netstandard2.0` asset — `net472`,
  `netstandard2.0`, any consumer below `net8.0` — has no `Any.DateOnly()`. The engine does not
  need to know this: the lookup fails and the parameter becomes a TODO.
* **Unsigned arithmetic.** `Positive()` and `Negative()` do not exist on `AnyByte`, `AnyUInt16`,
  `AnyUInt32` or `AnyUInt64` — an unsigned type cannot express them (§14.3). A `p <= 0` guard on
  a `uint` parameter therefore resolves to nothing and is silently skipped, rather than emitting
  a call that does not compile.
* **The public-API baseline.** Anything resolvable in the compilation is, by construction, part
  of the shipped surface.
* **Version skew.** An old `dum` against a new library emits less; a new `dum` against an old
  library emits less. Neither emits something that does not compile.
* **The developer's own generators.** The same lookup finds `AnyCustomer` in their code (§5.4).

### 2.4 D11 in detail — why the engine is its own library

The naive argument for splitting — *"the CLI may grow other verbs"* — is weak. Extra verbs are
extra files in the CLI project, all sitting above the same engine; that justifies no boundary.
After D1 the plausible verb list is nearly empty anyway, since `check`, `init` and `list` all died
with the scaffolder decision.

The real argument runs the other way: **the engine has a plausible consumer that is not a CLI.**
A Roslyn `CodeRefactoringProvider` — right-click a type, *"Scaffold a generator"* — is the natural
second surface for a library that already ships analyzers, and emitting a document is exactly what
a code refactoring does well. That consumer wants `(Compilation, ITypeSymbol) → source` and
nothing else: no Spectre, no MSBuild, no console, no file system.

Keeping that door open has a cost that must be paid **now or not at all**: an analyzer-hosted
assembly must target `netstandard2.0` and compile against the Roslyn floor (§13.2), while the CLI
targets `net8.0` and needs `MSBuildWorkspace`, which is neither. An engine born `net8.0` closes
the IDE path, and reopening it later means re-verifying every API against the floor. The cost is
asymmetric — cheap now, expensive later — and the engine is pure Roslyn plus string building, so
`netstandard2.0` costs it almost nothing. This is a deliberate, stated exception to YAGNI.

Two immediate benefits come along, independent of any future consumer:

* **Tests.** The test plan (§12) is dominated by engine tests — resolver behaviour over an
  in-memory `CSharpCompilation`, golden-file emission, compiling the output with analyzers wired.
  None of them want a console or an argument parser.
* **Mutation testing.** A single project would put Spectre command plumbing under the same
  mutation budget as the resolver. Two projects give one high-value target and one low-value one,
  configurable apart.

---

## 3. Command surface

The tool ships as a .NET tool whose command is **`dum`**.

```console
dotnet tool install --global JustDummies.Cli
dum generate <Type> [<Type>...] [options]
```

`generate` is the only verb in v1.0.

| Option | Default | Meaning |
|---|---|---|
| `--project <path>` | the single `*.csproj` in the current directory | Project whose compilation is analyzed. |
| `--output <dir>` | the current directory | Where the file is written. |
| `--namespace <ns>` | the target type's namespace (D8) | Namespace of the emitted type. |
| `--force` | off | Overwrite an existing file. |
| `--dry-run` | off | Print the file to stdout; write nothing. |

That is the entire surface. There is no config file, no `init`, no `list`, no `--all`, and — by
D1 — no `check`. §16 lists what is deliberately deferred.

### 3.1 Where the tool is run

From the **test project**, because that is where the file belongs. The test project references
the production project, so `Order` is reachable from its compilation, and `--output`'s default
puts `AnyOrder.cs` next to the tests that use it.

`--project` resolution: if exactly one `*.csproj` sits in the current directory, use it; if none
or several, fail with a message naming the candidates and pointing at `--project`.

### 3.2 Resolving the target type

`Order` is matched, in order:

1. by full metadata name, if the argument contains a `.` (`Shop.Domain.Order`);
2. by simple name across the compilation's source types and referenced assemblies.

Zero matches → error, listing the closest names by edit distance. More than one match → error,
listing the full names, asking for one of them. Both exit `1`.

---

## 4. The emitted file

### 4.1 Worked example

This example is not a sketch: it was compiled and run against the real library (§17).

Source under analysis:

```csharp
namespace Shop.Domain;

public sealed class Order {

    public Order(OrderReference reference, Customer customer, int quantity,
                 OrderStatus status, IReadOnlyList<string> tags, DateTime placedAt) {
        if (reference is null) { throw new ArgumentNullException(nameof(reference)); }
        if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
        ...
    }

}

public sealed class OrderReference {

    public static OrderReference Create(string value) {
        if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(...); }
        ...
    }

}
```

`dum generate Order`, with `AnyCustomer` already scaffolded in the project, emits:

```csharp
// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>A generator of arbitrary <see cref="Order" /> values.</summary>
public sealed partial class AnyOrder : IAny<Order> {

    private readonly IAny<OrderReference>        _reference;
    private readonly IAny<Customer>              _customer;
    private readonly IAny<int>                   _quantity;
    private readonly IAny<OrderStatus>           _status;
    private readonly IAny<IReadOnlyList<string>> _tags;
    private readonly IAny<DateTime>              _placedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(reference: Any.String().NonEmpty().As(OrderReference.Create),
               customer:  new AnyCustomer(),
               quantity:  Any.Int32().Positive(),
               status:    Any.Enum<OrderStatus>(),
               tags:      Any.ListOf(Any.String().NonEmpty()),
               placedAt:  Any.DateTime()) { }

    private AnyOrder(IAny<OrderReference>        reference,
                     IAny<Customer>              customer,
                     IAny<int>                   quantity,
                     IAny<OrderStatus>           status,
                     IAny<IReadOnlyList<string>> tags,
                     IAny<DateTime>              placedAt) {
        _reference = reference;
        _customer  = customer;
        _quantity  = quantity;
        _status    = status;
        _tags      = tags;
        _placedAt  = placedAt;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyOrder WithReference(OrderReference value) {
        return WithReference(new FixedValue<OrderReference>(value));
    }

    /// <summary>Draws <c>reference</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithReference(IAny<OrderReference> generator) {
        return new AnyOrder(generator, _customer, _quantity, _status, _tags, _placedAt);
    }

    // ... one such pair per parameter ...

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        return new Order(_reference.Generate(),
                         _customer.Generate(),
                         _quantity.Generate(),
                         _status.Generate(),
                         _tags.Generate(),
                         _placedAt.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
```

### 4.2 Shape rules

* `public sealed partial class Any{Type} : IAny<{Type}>`. `partial` so the developer's own
  members live in a neighbouring file and survive a `--force`.
* One `private readonly IAny<TParam> _param;` per constructor parameter, in declaration order.
* A **public parameterless constructor** carrying the inferred recipe, written with named
  arguments so the reader maps each expression to its parameter without counting.
* A **private all-arguments constructor** performing the copy.
* Per parameter, **two** `With{Param}` overloads returning a new instance:
  `With{Param}(TParam value)` and `With{Param}(IAny<TParam> generator)`.
  The value overload is the ergonomic one; the generator overload is what keeps composition
  possible and is why passing `Any.String().StartingWith("ORD-")` does not become a `JD011`/`JD012`
  mistake.
* `public {Type} Generate()` calling the constructor with each field's `Generate()`.
* The private nested `FixedValue<TValue>` helper. Rationale: it accepts `null` (which
  `Any.OneOf(value)` rejects) and consumes no draw from the ambient source, so pinning a
  parameter does not shift the values drawn for the others (§14.5). It is nested and private, so
  any number of scaffolded files coexist. *(If `Any.Fixed<T>(value)` is ever added to the
  library, the helper can be dropped — see §15.)*
* `With{Param}` casing: the parameter name, first letter upper-cased, invariant culture. A
  parameter named `_id` or `@class` is normalised by stripping the leading `_`/`@`.

### 4.3 Header rules

Exactly three comment lines, as above. **No timestamp and no tool version**: both would make the
byte content depend on something other than the analyzed type, so every scaffold after a tool
upgrade would produce a spurious diff. Determinism is a hard requirement (§8.1).

### 4.4 Language level

The emitted code uses no construct newer than **C# 7.3**: no `var` (it reads better in a
skeleton), no target-typed `new`, no records, no switch expressions, no file-scoped namespace
unless the target type's own file already uses one. The file lands in the developer's project and
must compile at that project's `LangVersion`.

The one exception is the namespace form, which is copied from the target type's declaration
style so the emitted file looks like its neighbours.

---

## 5. Resolution — how a parameter becomes a generator

For each parameter, the engine produces an expression of type `IAny<TParam>`, or fails to and
marks the parameter unresolved.

### 5.1 Choosing the constructor

1. Public instance constructors, most parameters first; ties broken by source order. The chosen
   signature is always printed (§6).
2. If the type has **no** accessible constructor but exposes a recognised static factory (§5.4)
   returning itself, that factory is used instead and `Generate()` calls it.
3. A parameterless constructor yields a valid, trivial `AnyOrder` with no `With` methods.
4. Positional records work with no special handling — their primary constructor is an ordinary
   public constructor. `init` and `required` members are **out of scope** (§16).

### 5.2 The base table

Every entry is subject to D4: the member is emitted only if it resolves in the compilation.

| Parameter type | Emitted |
|---|---|
| `string` | `Any.String().NonEmpty()` |
| `bool` | `Any.Boolean()` |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` | `Any.SByte()` … `Any.UInt64()` |
| `float` `double` `decimal` | `Any.Single()` / `Any.Double()` / `Any.Decimal()` |
| `char` | `Any.Char()` |
| `Guid` | `Any.Guid().NonEmpty()` |
| `DateTime` `DateTimeOffset` `TimeSpan` | `Any.DateTime()` / `Any.DateTimeOffset()` / `Any.TimeSpan()` |
| `DateOnly` `TimeOnly` `Int128` `UInt128` `Half` | the matching factory — **`net8.0` asset only**, D4 decides |
| any `enum E` | `Any.Enum<E>()` |
| `Uri` | `Any.Uri().Web()` |
| `T[]` | `Any.ArrayOf(<T>)` |
| `List<T>` `IReadOnlyList<T>` `IList<T>` `ICollection<T>` `IReadOnlyCollection<T>` | `Any.ListOf(<T>)` |
| `IEnumerable<T>` | `Any.SequenceOf(<T>)` |
| `HashSet<T>` `ISet<T>` | `Any.SetOf(<T>)` |
| `Dictionary<K,V>` `IDictionary<K,V>` `IReadOnlyDictionary<K,V>` | `Any.DictionaryOf(<K>, <V>)` |
| `T?` where `T` is a reference type | the generator for `T` unchanged — **never** `.OrNull()` (D10) |
| `T?` where `T` is a value type | `<generator for T>.As(value => (T?)value)` — **never** `.OrNull()` (D10) |
| a type with a scaffolded `AnyT` in the compilation | `new AnyT()` (§5.4) |
| a type with a recognised one-parameter static factory | `<param generator>.As(T.Create)` (§5.4) |
| anything else | unresolved (§5.5) |

Three notes on the table.

**`Any.String().NonEmpty()`, not `Any.String()`.** Unconstrained, `Any.String()` yields *0 to 16*
ASCII letters and digits (§14.5) — it can return the empty string. A constructor parameter of type
`string` in a domain type is overwhelmingly required non-empty, and a default that fails roughly
one call in sixteen (measured: §17) is exactly the flakiness the library exists to remove. Same
reasoning for `Any.Guid().NonEmpty()`.

**Collections rely on covariance — and value types do not.** `IAny<out T>` is covariant, so
`Any.ListOf(...)`, whose type is `IAny<List<T>>`, is directly assignable to a field of type
`IAny<IReadOnlyList<T>>`; no adapter is needed for any of the interface rows, and the same holds
for `HashSet<T>`/`ISet<T>` and `Dictionary<K,V>`/`IReadOnlyDictionary<K,V>`.

Variance in C# applies only across **reference** conversions, which is why the two nullable rows
differ. `IAny<string>` is an `IAny<string?>` and needs nothing; `IAny<int>` is **not** an
`IAny<int?>`, so an `int?` parameter needs the explicit `.As(value => (int?)value)` hop. Getting
this wrong is the most likely way an implementer produces a table that does not compile — the
`net8.0`-only rows are all value types too.

**Element generators recurse.** `IReadOnlyList<OrderLine>` resolves its element through this same
table, so it becomes `Any.ListOf(new AnyOrderLine())` when `AnyOrderLine` exists. Recursion is
depth-limited to 3 and cycle-guarded; exceeding either makes the parameter unresolved.

### 5.3 Guard clauses

This is the feature that makes the tool worth building rather than templating.

When the constructor's (or factory's) **body is available as source** — which it is for any type
in the developer's solution, and is not for a type coming from a NuGet package — the engine reads
its leading guard clauses and tightens the generator accordingly.

A statement is a guard only when **all** of the following hold. The rule is deliberately
conservative, mirroring how the library's own analyzers under-report rather than misfire:

* it is an `if` statement whose body throws unconditionally, with no `else`;
* it appears before the first assignment to a field or property;
* its condition mentions **exactly one** parameter and contains no `&&` or `||`;
* every other operand is a compile-time constant.

The recognised set is closed:

| Condition that throws | Constraint added |
|---|---|
| `p is null`, `p == null` | none — the generator never returns `null` anyway |
| `string.IsNullOrEmpty(p)`, `string.IsNullOrWhiteSpace(p)`, `p.Length == 0`, `p.Length < 1` | `.NonEmpty()` |
| `p.Length > N` | `.WithMaxLength(N)` |
| `p.Length < N` | `.WithMinLength(N)` |
| `p <= 0`, `p < 1` | `.Positive()` |
| `p < 0` | `.GreaterThanOrEqualTo(0)` |
| `p >= 0` | `.Negative()` |
| `p == 0` | `.NonZero()` |
| `p > N` | `.LessThanOrEqualTo(N)` |
| `p < N` | `.GreaterThanOrEqualTo(N)` |
| `p == Guid.Empty` | `.NonEmpty()` |
| `!Regex.IsMatch(p, "literal")` | the base generator is replaced by `Any.StringMatching("literal")` |
| `!Enum.IsDefined(typeof(E), p)` | none — `Any.Enum<E>()` already draws only declared members |

`.NonEmpty()` covers `IsNullOrWhiteSpace` as well as `IsNullOrEmpty`, because an unconstrained
`Any.String()` draws only ASCII letters and digits, so a non-empty draw can never be whitespace
(§14.5).

Constraints are grouped by **axis** — length, range, charset, pattern. If two recognised guards
land on the same axis, **both are dropped** and the parameter is reported as
`guards not combined`; the developer sees the neutral generator and the console tells them to
look. This is the only place the engine could emit a chain the library rejects at runtime with
`ConflictingAnyConstraintException`, and this rule removes it.

Every constraint above is still subject to D4. `.Positive()` on a `uint` parameter does not
resolve (§14.3) and is skipped.

Guard reading is also what makes factory composition correct rather than nominally present:
`OrderReference.Create` guards on `IsNullOrWhiteSpace`, so the tool emits
`Any.String().NonEmpty().As(OrderReference.Create)` — a chain that works — instead of
`Any.String().As(OrderReference.Create)`, which was measured throwing `AnyGenerationException`
**594 times in 10 000 draws**, about one in sixteen (§17).

That single measurement is the argument for this whole section. A tool that emits the second
chain does not merely fall short: it manufactures, in the developer's test suite, the exact
intermittent failure the library was built to eliminate.

### 5.4 Composition

**A scaffolded generator wins.** If the compilation contains a type named `Any{T}` implementing
`IAny<T>` with a public parameterless constructor, the engine emits `new Any{T}()`. This is how
aggregates compose in cascade, and it works whether that type was scaffolded earlier or written
by hand.

**Otherwise, a static factory.** A method qualifies when it is `public static`, returns the
parameter's type, takes exactly one parameter, and is named `Create`, `From`, `Of` or `Parse`.
If several qualify, `Create` wins; if several remain, the parameter is unresolved and the console
names the candidates. The emission is `<generator for the factory's parameter>.As(T.Create)`,
with §5.3 applied to the factory's own body.

Convention, not attribute, not configuration: an attribute would mean touching the developer's
production code to please a test tool, and a configuration file breaks design rule 2.

### 5.5 Unresolved parameters

The parameter's argument in the public constructor becomes an identifier that does not exist:

```csharp
    public AnyOrder()
        : this(reference: Any.String().NonEmpty().As(OrderReference.Create),
               // TODO(dum): no generator inferred for 'Customer customer'.
               //   Scaffold one:  dum generate Customer
               //   or write one here, or delete this argument and always pass .WithCustomer(...).
               customer:  TODO_supply_a_generator_for_customer,
               quantity:  Any.Int32().Positive(),
               ...
```

The file does not compile until the developer acts. That is the point (D6). The compiler's own
message — *"The name 'TODO_supply_a_generator_for_customer' does not exist in the current
context"* — is the instruction, and it appears in the IDE, in the error list, and in CI.

The two alternatives were rejected: a `throw` expression compiles and defers the failure to the
first test run, and omitting the parameter makes `AnyOrder` quietly unusable without saying so.
The developer runs the tool and opens the file in the same minute; a red squiggle at the exact
line costs them ten seconds, and a runtime failure a week later costs far more.

---

## 6. Console output

The console recap is not decoration: it is the mechanism that keeps the tool honest about what it
inferred and what it guessed.

```console
$ dum generate Order

Analyzing Shop.Domain.Order
  constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

  reference  OrderReference         Any.String().NonEmpty().As(OrderReference.Create)  factory, guard
  customer   Customer               —                                                  TODO
  quantity   int                    Any.Int32().Positive()                             guard
  status     OrderStatus            Any.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())
  placedAt   DateTime               Any.DateTime()

✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO.
  The file will not compile until you resolve it. That is deliberate.
```

The right-hand column carries the provenance of each expression: empty for the base table,
`guard` when §5.3 tightened it, `factory` when §5.4 composed it, `AnyX` when a scaffolded
generator was reused, `guards not combined` for the §5.3 conflict case, `no source` when the
constructor body was unavailable so no guard could be read.

**Provenance is data, not output.** The engine returns it in its result model (§10.3); the CLI
renders it. That is what makes the recap testable without a console.

`--dry-run` prints the same recap to stderr and the file to stdout.

---

## 7. Failure modes and exit codes

| Situation | Exit | Behaviour |
|---|---|---|
| File written, everything inferred | `0` | — |
| File written, one or more TODOs | `0` | The write succeeded; the developer's build reports the rest. |
| `--dry-run` | `0` | Nothing written. |
| Type not found / ambiguous | `1` | Candidates listed. |
| Output file exists, no `--force` | `1` | Names the file, suggests `--force`, warns that edits are lost. |
| No project / several projects found | `1` | Candidates listed, `--project` suggested. |
| Project fails to load or restore | `1` | The MSBuild diagnostic, verbatim. |
| The project does not reference JustDummies | `1` | Nothing can be resolved (D4); says so and suggests the package. |
| `Any{Type}` shadows a `JustDummies.Any*` type | `0` | **Warning**, then generate. |

That last row deserves its own note. The library owns 39 public `Any*` type names (§14.2) —
`AnyList`, `AnySet`, `AnyArray`, `AnySequence`, `AnyPattern`, `AnyUri`, `AnyChar`, `AnyString`, …
A domain type named `Set`, `List`, `Sequence` or `Pattern` scaffolds to a name that, inside its own
namespace, **silently shadows the library's type** for every file in that namespace: C# resolves
the enclosing namespace before any `using`. It compiles; it is just wrong later. The tool warns,
names both types, and generates anyway — under design rule 4, the rename is the developer's call,
and v1.1 gives them the switch.

Multiple type arguments (`dum generate Order Customer Invoice`) are processed independently; the
exit code is the worst of them, and one failure does not prevent the others being written.

---

## 8. Guarantees

### 8.1 Determinism

The same type analyzed against the same compilation produces a **byte-identical** file, on any
machine, under any tool version that resolves the same members. Nothing time-, path-,
culture- or hash-order-dependent enters the output: no timestamp, no tool version, no absolute
path, and every enumeration the emitter walks is ordered by declaration.

This matters even without a `check` verb: it is what makes a re-scaffold reviewable as a diff.

### 8.2 Reproducibility

The emitted generator draws from the **ambient** random context, because every expression it
emits comes from the static `Any` façade, and the ambient source resolves the current `AsyncLocal`
frame **at draw time**, not at construction time (§14.5). Therefore:

```csharp
AnyOrder recipe = new AnyOrder();          // built outside the scope
Any.Reproducibly(() => {
    Order order = recipe.Generate();       // still pinned by the scope's seed
});
```

is reproducible, and so is the ordinary case where both happen inside the scope. This was
verified (§17).

**`Any.WithSeed(seed)` is out of scope by decision (D7).** An `AnyContext` carries its own fixed
random source and is unaffected by the ambient scope, so a generator built from `Any.*` cannot
draw from it. Supporting it would mean an `AnyOrder(AnyContext)` constructor and a second recipe
path. It is not worth the surface: the `.With{Param}(IAny<TParam>)` overload already lets a
developer on `WithSeed` supply `context.String()` per parameter. The emitted XML doc says so in
one sentence.

The emitter never produces static state, so `JD009` and `JD020` have nothing to fire on.

### 8.3 No reflection in the emitted code

The emitted file contains no reflection — it is constructor calls and fluent chains. The
library's *"no reflection"* claim is a claim about what runs in the developer's test, and it
holds.

The **tool itself** is a build-time program and is under no such constraint; it uses Roslyn, which
is not reflection anyway. The two questions are independent.

---

## 9. Non-goals for v1.0

Named explicitly so they are not mistaken for oversights.

* **Realistic data.** The tool inherits the library's scope: arbitrary-but-valid, never plausible.
  No names, no emails, no addresses.
* **Object-graph auto-filling.** Composition is one hop through `Any{T}` or a one-parameter
  factory, depth-limited to 3. Beyond that the developer writes it.
* **Invariants the tool cannot see.** §5.3 reads a closed set of guard idioms. A constructor that
  validates through a helper method, a `Guard.Against` library, or a cross-parameter rule gets the
  neutral generator and a console line. It does not get a wrong guess.
* **Round-tripping.** The tool never reads a file it previously wrote.
* **`init` / `required` members, property-only construction.** Constructor and static factory only.
* **Anything under `--all`.** Explicit type arguments only.

---

## 10. Architecture

### 10.1 Two projects

| Project | TFM | Role |
|---|---|---|
| `JustDummies.GenAny` | `netstandard2.0`, pinned to the Roslyn floor (§13.2) | The engine. Resolution, guard reading, composition, emission. |
| `JustDummies.Cli` | `net8.0`, `RollForward=Major` | The shell. Commands, project loading, file IO, console. |

On the name: the repository's existing engine for the sibling tool is called `GenDoc` — a
**function** name, not a pattern name (`GenDoc` generates documentation). `GenAny` follows it
exactly: it generates the `AnyX` types, and `Any` is the library's central noun (`Any.String()`,
`IAny<T>`, `AnyOrder`). "Scaffolder" was rejected as a project name — it names a generic role
rather than a product, and every framework has one. The word survives in the prose, where it
describes *behaviour* (§1); the project is named after what it *produces*.

### 10.2 The boundary

**`JustDummies.GenAny` owns** the resolution table (§5.2), guard reading (§5.3), composition and
factory recognition (§5.4), the emitter (§11.2), and the naming function (§11.3).
It depends on `Microsoft.CodeAnalysis.CSharp` **only** — not `Workspaces`, which it does not need:
guard reading wants a syntax tree and a semantic model, and emission is string building.

**It performs no IO, writes to no console, and never touches MSBuild.** Those three constraints
are what keep it loadable inside a Roslyn host.

**`JustDummies.Cli` owns** the Spectre command definitions and settings, project discovery,
`MSBuildLocator` / `MSBuildWorkspace`, file writing, `--force` / `--dry-run` handling, the console
recap rendering, and the exit codes of §7.

### 10.3 The contract between them

One entry point, shaped so the future IDE consumer can call it unchanged:

* **Input** — a `Compilation`, the target `ITypeSymbol`, and an options record carrying the
  namespace override and the type-naming pattern (§16).
* **Output** — a result model, never a bare string:
  * the file name and the full source text;
  * per-parameter rows: name, type display string, emitted expression (or none), and provenance
    (§6);
  * warnings, such as the `Any*` shadowing case of §7;
  * a flag for "contains at least one TODO".

The CLI renders that model; a code refactoring would apply the source text and ignore the rest.
Nothing in the model is a console string.

### 10.4 Packaging

`JustDummies.Cli` is packed as the .NET tool (`PackAsTool`, `ToolCommandName=dum`,
`PackageId=JustDummies.Cli`). `JustDummies.GenAny` is **not published as its own package** in
v1.0: it travels inside the tool package as an ordinary managed dependency, which is exactly how
the sibling repository ships its `GenDoc` engine. Publishing it later, when an IDE consumer
exists, is a purely additive decision.

Consequence: neither project carries a public-API compatibility promise, so neither takes a
public-API baseline (§13.4).

**D9 applies to both.** Neither project references the `JustDummies` package or project. Every
JustDummies symbol is resolved by metadata name against the developer's compilation, exactly as
the library's analyzers do. Version skew between tool and library is therefore structurally
impossible, and the tool package must declare no `JustDummies` dependency (§13.6).

---

## 11. Implementation notes

### 11.1 Pipeline

1. `MSBuildLocator.RegisterDefaults()` — **before touching any Roslyn workspace type**. Loading
   `MSBuildWorkspace` first is the classic way this fails, with a `FileNotFoundException` on
   `Microsoft.Build` that names nothing useful. (CLI only.)
2. `MSBuildWorkspace.Create()`, open the project, take its `Compilation`. Workspace diagnostics
   are surfaced, not swallowed. (CLI only.)
3. Hand the `Compilation` to the engine. Everything from here is `JustDummies.GenAny`.
4. Resolve `JustDummies.Any`, `JustDummies.IAny\`1` and `JustDummies.AnyExtensions` by metadata
   name. Absent → the engine reports it and the CLI exits `1` (§7).
5. Resolve the target type (§3.2), pick the constructor (§5.1).
6. Per parameter: base table (§5.2) → guards (§5.3) → composition (§5.4) → unresolved (§5.5).
   Every candidate member is looked up in the compilation before it is kept (D4).
7. Emit into the result model (§10.3).
8. The CLI writes the file and renders the recap.

### 11.2 Emitter

A plain string builder over an ordered model, not `SyntaxFactory`. The output must be readable
and match a hand-written layout — aligned field declarations, explicit types, braces — and
`SyntaxFactory`-normalised whitespace does not produce that. Since the emitter is covered by
golden-file tests (§12), the fragility argument for a syntax API does not apply.

### 11.3 Naming

Route the emitted type name through **one** function, `TypeNaming.GeneratorNameFor(ITypeSymbol,
NamingOptions)`. v1.1 (§16) is then a change to that function plus an options binding, not a
sweep. In v1.0 `NamingOptions` carries a single fixed pattern, `Any{Type}`.

---

## 12. Test plan

**Engine — `JustDummies.GenAny.UnitTests`** (the bulk):

* **Resolver unit tests.** Build a `CSharpCompilation` in memory with a reference to the built
  `JustDummies.dll`, and assert the emitted expression string per parameter. Fast, no MSBuild.
  Cover every row of §5.2, every row of §5.3, both §5.4 paths, and the §5.5 fallback. Include the
  unsigned case (`p <= 0` on a `uint`) and the value-type nullable case.
* **Emitter golden files.** One approved file per representative shape: no parameters, one
  parameter, six parameters, a TODO, a name collision, a positional record, a static-factory
  target.
* **Compile-the-output tests.** Each golden file is compiled against `JustDummies.dll` **with the
  JustDummies analyzers wired**, and the compilation must produce no `CS*` error and no `JD*`
  diagnostic. This is the check D3 buys: since the file is not marked as generated code, the
  analyzers actually run on it. The harness must include a **control file with a known violation**,
  asserted to fire — otherwise "no diagnostics" cannot be distinguished from "analyzers not
  loaded" (§17.2).
* **The own-code test.** Scaffold the **hosting repository's real types**, compile the results,
  and generate a value from each. The reasoning is recorded in the analyzer-on-own-code decision
  (§13.7): a rule and the snippet that tests it, both written by the same author, share the same
  misconception and pass together; code written for other reasons does not. `ErrorCode.Create` in
  the current repository is the canonical case — it guards on `IsNullOrWhiteSpace`, so without
  §5.3 the scaffolded code fails about one call in sixteen, which no golden file would reveal.
  In a repository without such types, use any validating value object with a static factory.
* **Asset-selection test.** Scaffold against a `netstandard2.0`-asset consumer and a `net8.0`-asset
  consumer for a type with a `DateOnly` parameter, and assert the first produces a TODO and the
  second `Any.DateOnly()`. This is the executable proof of D4 (§13.8).

**Shell — `JustDummies.Cli.UnitTests`:** project discovery, option handling, exit codes of §7,
and recap rendering from a fixed result model.

---

## 13. What the hosting repository must provide

JustDummies is expected to move to its own repository before this tool is built. This section
states each dependency on the host as a **requirement**, with the current repository's
realization as an example. If the library has moved, re-establish these there; do not build the
tool against another repository's infrastructure.

**13.1 Pinned package versions** for the tool's dependencies. New to the tool:
`Microsoft.CodeAnalysis.Workspaces.MSBuild` and `Microsoft.Build.Locator` (CLI only). Already
present for the library and its analyzers: `Microsoft.CodeAnalysis.CSharp` and
`Spectre.Console.Cli`. *Current realization: central package management in
`Directory.Packages.props`.*

**13.2 A Roslyn floor property.** `JustDummies.GenAny` must compile against the **same minimum
Roslyn version as the analyzer package**, and must not float above it — an assembly loaded by a
consumer's compiler fails silently (`CS8032`) on an older host if it was built against a newer
Roslyn. *Current realization: `RoslynFloorVersion` = `4.8.0`, set once in `Directory.Build.props`
and applied with `VersionOverride`.* The CLI is **not** bound by this: it hosts its own compiler.

**13.3 Solution nesting.** If the host uses a `.sln`, add both projects and both test projects to
its `GlobalSection(NestedProjects)` under the source and test solution folders. A project missing
from that section appears loose at the solution root instead of grouped with its siblings. This
has been missed and fixed after the fact several times; check it every time a `.csproj` is added.

**13.4 Public-API baseline exclusion.** Neither `JustDummies.GenAny` nor `JustDummies.Cli` opts
into the public-API baseline: tools carry no compatibility promise, and the analyzer would flag
their entire surface as undeclared. *Current realization: only the shipping libraries import
`build/PublicApiBaseline.props`.*

**13.5 Mutation testing.** If the host measures mutation on projects whose code ships or runs,
both projects qualify. Give each its own configuration — the engine is the high-value target, the
shell is not — and register them with the rest. *Current realization: one JSON per project under
`build/stryker/`, driven by a dedicated workflow, advisory per pull request and enforced by a
weekly sweep.*

**13.6 A release train for the tool,** separate from the library's. The tool does not version in
lockstep with the library (D9), so it must not ride the library's train. The train's packing step
must assert that the produced `.nupkg` declares **no `JustDummies` dependency** — the executable
form of D9. *Current realization: `tools/packaging/pack.sh` with one train per package family and
a standalone assertion already written for the library's train.*

**13.7 The analyzers must be runnable over the host's own code,** so the own-code test of §12 can
exist. *Current realization: the analyzer project is wired into the repository's own suites, a
decision taken after the analyzers' unit suite was found unable to catch five wrong rules that
running over real code caught immediately.*

**13.8 A way to consume the packed library from two consumer TFMs,** so the asset-selection test
of §12 can exist: one consumer at `net8.0` (resolves the `net8.0` asset) and one below it
(resolves `netstandard2.0`). *Current realization: an isolated project outside the solution,
multi-targeted, consuming the packed `.nupkg` from a local feed.*

**13.9 Test framework.** *Current realization: `xunit.v3`, `NFluent`, `Verify.XunitV3` for golden
files, `NSubstitute`.* Any equivalent works; the golden-file tests need a snapshot library.

**13.10 Commit, branch and pull-request conventions,** and an ADR process for §15. *Current
realization: Conventional Commits with a closed type and scope list, enforced by a hook and by
CI; ADRs under `doc/handwritten/for-maintainers/adr/` where an agent drafts as `Proposed` and the
maintainer accepts.*

---

## 14. Library facts this specification depends on

Everything below was read from the library's source. It is inlined so this document can be
implemented from without opening the library, and so a future reader can tell which claims are
load-bearing. §14.7 gives the command to re-derive each block.

### 14.1 Package identity and target frameworks

* `PackageId` **`JustDummies`**, `TargetFrameworks` **`netstandard2.0;net8.0`**, `Nullable`
  enabled, `LangVersion` latest.
* The two assets diverge: the `net8.0` leg additionally carries `DateOnly`, `TimeOnly`, `Int128`,
  `UInt128` and `Half`, guarded by `#if NET8_0_OR_GREATER`. A consumer below `net8.0` resolves the
  `netstandard2.0` asset and does not see them. This is the fact D4 exists to absorb.
* The analyzers ship **inside** that package under `analyzers/dotnet/cs`, so every consumer gets
  them automatically. This is why the emitted file is analyzed at all (D3).
* A companion package adapts the library to xUnit v3 (`[Reproducible]`); the tool does not
  interact with it.

### 14.2 Entry points

`JustDummies.Any` is a static façade, split across partial files by family. The complete set of
factories, all drawing from the ambient random context:

* **Primitives** — `String()`, `Boolean()`, `Char()`, `Guid()`,
  `SByte()`, `Byte()`, `Int16()`, `UInt16()`, `Int32()`, `UInt32()`, `Int64()`, `UInt64()`,
  `Single()`, `Double()`, `Decimal()`,
  `TimeSpan()`, `DateTime()`, `DateTimeOffset()`,
  `Enum<TEnum>() where TEnum : struct, Enum`.
* **`net8.0` asset only** — `DateOnly()`, `TimeOnly()`, `Int128()`, `UInt128()`, `Half()`.
* **Pattern** — `StringMatching(string)`, `StringMatching(Regex)`.
* **URI** — `Uri()`, then a family selector: `.Web()`, `.Ftp()`, `.Mailto()`, `.Relative()`,
  `.WebSocket()`.
* **Choice** — `OneOf<T>(params T[])`, `ElementOf<T>(IReadOnlyList<T>)`,
  `ElementOf<T>(IEnumerable<T>)`.
* **Collections** — `ListOf<T>`, `ArrayOf<T>`, `SequenceOf<T>`, `SetOf<T>` (with an optional
  comparer), `DictionaryOf<TKey,TValue>` (with an optional key comparer).
* **Composition** — `Combine` in arities 2 through 8, `PairOf`, `TripleOf`.
* **Reproducibility** — `WithSeed(int)`, `UseSeed(int)`, `UseSeed(int, string)`,
  `Reproducibly(...)`, `ReproduciblyAsync(...)`.

Note the naming traps: it is **`Any.Boolean()`**, not `Any.Bool()`; and `double` maps to
**`Any.Double()`**, not `Any.Decimal()`.

`AnyContext`, returned by `Any.WithSeed(int)`, mirrors the primitives, the pattern, the URI and
the choice entry points as **instance** methods drawing from its own fixed source. It does **not**
mirror the collection or composition entry points. D7 puts it out of scope.

The library declares **39 public `Any*` type names** (37 generators plus `AnyContext` and
`AnyGenerationException`). That set is what the shadowing warning of §7 checks against.

### 14.3 Constraint surfaces the emitter uses

| Generator family | Constraints relied on by §5.2 and §5.3 |
|---|---|
| `AnyString` | `NonEmpty`, `WithMinLength`, `WithMaxLength`, `WithLength`, `WithLengthBetween`, `StartingWith`, `EndingWith`, `Containing`, `Alpha`, `Numeric`, `AlphaNumeric`, `UpperCase`, `LowerCase`, `WithChars`, `OneOf`, `Except`, `DifferentFrom` |
| Signed integers (`SByte`, `Int16`, `Int32`, `Int64`) | `Positive`, `Negative`, `NonZero`, `Zero`, `Between`, `GreaterThan(OrEqualTo)`, `LessThan(OrEqualTo)`, `MultipleOf`, `OneOf`, `Except`, `DifferentFrom` |
| **Unsigned integers** (`Byte`, `UInt16`, `UInt32`, `UInt64`) | the same **less `Positive` and `Negative`**, which an unsigned type cannot express |
| `AnyDouble`, `AnySingle` | as signed integers, less `MultipleOf` |
| `AnyDecimal` | as signed integers, less `MultipleOf`, plus `WithScale` |
| `AnyGuid` | `NonEmpty`, `Empty`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyBoolean` | `True`, `False`, `DifferentFrom` |
| `AnyEnum` | `AllowingCombinations`, `OneOf`, `Except`, `DifferentFrom` |
| Temporal (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) | `After(OrEqualTo)`, `Before(OrEqualTo)`, `Between`, `WithGranularity`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyTimeSpan` | temporal-style plus `Positive`, `Negative`, `NonZero`, `Zero` |
| Collections | `Empty`, `NonEmpty`, `WithCount`, `WithCountBetween`, `WithMinCount`, `WithMaxCount`, `Containing`, `ContainingAny` |

The unsigned row is the one that bites: it is why D4 must gate `.Positive()` rather than the
emitter assuming a uniform numeric algebra.

### 14.4 Composition seams

* `AnyExtensions.As<TSource,TResult>(this IAny<TSource>, Func<TSource,TResult>)` → `IAny<TResult>`.
  A method group such as `OrderReference.Create` binds directly. When the factory rejects the
  generated value, the call throws `AnyGenerationException`.
* `Any.Combine` (arities 2–8) → `IAny<TResult>`.
* Collection generators derive from a common base implementing `IAny<TCollection>`:
  `ListOf` → `List<T>`, `ArrayOf` → `T[]`, `SequenceOf` → `IEnumerable<T>`, `SetOf` →
  `HashSet<T>`, `DictionaryOf` → `Dictionary<TKey,TValue>`.
* `NullableExtensions.OrNull<T>()` exists in two forms, one for value types and one for annotated
  reference types. **D10 forbids emitting either.**

### 14.5 Semantic invariants the emitted code depends on

These five are the ones that would silently break the emitted code if they changed. Each is
exercised by §17.

1. **The ambient source resolves at draw time.** Every `Any.*` factory captures a singleton
   ambient source, and that source reads the current `AsyncLocal` frame inside `Generate()`, not
   at construction. This is why a recipe built outside a reproducibility scope still replays
   inside it (§8.2).
2. **`IAny<out T>` is covariant.** Which is why the collection interface rows of §5.2 need no
   adapter — and why the value-type nullable row does.
3. **Generators are immutable recipes.** Every fluent constraint returns a new instance. D2
   inherits this.
4. **`Any.String()` unconstrained draws 0 to 16 ASCII letters and digits.** It can return the
   empty string; it can never return whitespace. Both halves matter to §5.2 and §5.3.
5. **`Any.OneOf(value)` requires at least one value, rejects `null` elements, and consumes a
   draw.** All three are why §4.2 emits a private `FixedValue<TValue>` instead.

### 14.6 Analyzer inventory

28 diagnostic identifiers over 27 analyzer classes — `JD023` and `JD024` share one.

| Range | Category | Severities |
|---|---|---|
| `JD001`–`JD004` | Reproducibility | all **Error** |
| `JD005` | Usage | **Error** |
| `JD006` | Usage | Warning |
| `JD007`–`JD010` | Reproducibility | Warning |
| `JD011` | Usage | **Disabled by default** |
| `JD012`–`JD013` | Usage | Warning |
| `JD014`–`JD017` | Constraints | Warning |
| `JD018` | Reproducibility | Warning |
| `JD019` | Reproducibility | **Disabled by default** |
| `JD020` | Reproducibility | Info |
| `JD021` | Reproducibility | Warning |
| `JD022` | Reproducibility | Info |
| `JD023` | Constraints | Warning |
| `JD024` | Constraints | Info |
| `JD025`–`JD026` | Constraints | Warning |
| `JD027`–`JD028` | Composition | Warning |

Three facts about them drive decisions in this document:

* **All 27 call `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`** — hence D3.
* **The `Usage` rules match any type implementing `IAny<T>`**, not a list of built-in generators —
  hence D2's second benefit.
* **The `Reproducibility` rules match chains rooted at the static `Any` façade**, deliberately
  answering "no" for a generator reached through a local, a field or a parameter. `new
  AnyOrder().Generate()` is therefore invisible to them; that is a known and accepted limit, not
  a defect the tool can fix.

### 14.7 How to re-derive these facts

From the library's repository root:

```console
# 14.1  package identity and the TFM split
grep -n "TargetFrameworks\|PackageId\|analyzers/dotnet/cs" JustDummies/JustDummies.csproj
grep -n "#if NET8_0_OR_GREATER" JustDummies/Any.Primitive.cs

# 14.2  entry points, and the AnyContext mirror
grep -hn "public static" JustDummies/Any.*.cs
grep -n "public " JustDummies/AnyContext.cs
grep -rhoP "^public (sealed )?class \KAny\w+" JustDummies/*.cs | sort

# 14.3  constraint surfaces
grep -oP "public AnyInt32 \K\w+(?=\()" JustDummies/AnyInt32.cs | sort -u
grep -oP "public AnyUInt32 \K\w+(?=\()" JustDummies/AnyUInt32.cs | sort -u   # note: no Positive/Negative

# 14.4  composition seams
grep -n "public static" JustDummies/AnyExtensions.cs JustDummies/NullableExtensions.cs

# 14.5  invariants — read the XML docs, they state all five
sed -n '1,60p' JustDummies/IAny.cs
grep -n "AmbientRandomSource.Instance" JustDummies/Any.Primitive.cs | head -3

# 14.6  analyzer inventory and the generated-code exemption
cat JustDummies.Analyzers/AnalyzerReleases.Unshipped.md
grep -rlc "ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)" JustDummies.Analyzers/*.cs | wc -l
```

Paths are those of the current repository; adjust them if the library has moved.

---

## 15. ADRs to draft

Drafted as `Status: Proposed`, one per decision; the maintainer accepts. None conflicts with an
accepted decision: D2 reinforces the recipe-versus-value analyzer decision, and D9 respects the
library's standalone-dependency rule.

| Decision | Proposed title |
|---|---|
| D1 | Scaffold the generator once and hand the file to the developer |
| D3 | Leave the scaffolded file open to the JustDummies analyzers |
| D4 | Emit only members resolved in the target compilation |
| D5 + D6 | Seed generators from constructor guards, and leave the rest as a compile error |
| D9 | Give the scaffolder no dependency on the JustDummies package |
| D11 | Keep the scaffolding engine loadable by a Roslyn host |

One library-side follow-up is worth a separate proposal and is **not** required for v1.0:
`Any.Fixed<T>(value)`, an `IAny<T>` returning a constant. `Any.OneOf(value)` almost fills the
role but rejects `null` and consumes a draw (§14.5). Adding it would let the emitter drop the
nested `FixedValue<TValue>` helper. Library API addition, so: maintainer's call.

---

## 16. Reserved for v1.1+

v1.0 must not paint these into a corner; §11.3 is the constraint that keeps the first one cheap.

**Naming.** `AnyOrder` → `OrderFactory`, or any other pattern. Shape:

```console
dum generate Order --name OrderFactory        # this type only
dum generate Order --pattern "{Type}Factory"  # this run
```

plus an optional `dum.json` at the project root for a project-wide default:

```json
{ "naming": { "pattern": "Any{Type}" } }
```

`{Type}` is the only placeholder. The default pattern stays `Any{Type}`, so an existing project
sees no change. This is also the answer to the shadowing warning of §7.

**Other deferred items.** `--all`; `init` / `required` members and property-only construction;
`AnyContext` support (D7); an `--ctor` selector when several constructors compete; extending §5.3
to a `Guard.Against`-style helper library; publishing `JustDummies.GenAny` as its own package once
an IDE consumer exists; the IDE code refactoring itself.

Deliberately **not** deferred — dropped: a `check` verb, a source-generator mode, and any form of
regeneration or drift detection. D1 removes the problem they would solve.

---

## 17. Verification

### 17.1 What was checked

The emitted file of §4.1 was written out by hand exactly as specified — including the `int?`
parameter, the `FixedValue<TValue>` helper and the composed `AnyCustomer` — then compiled and run
against `JustDummies.dll` built from source (`net8.0` asset), with the JustDummies analyzers wired
in. The results below are what the harness printed.

| Claim | Where | Result |
|---|---|---|
| The specified skeleton compiles as written | §4.1 | compiles, 0 warnings |
| `.WithX` chaining works and does not disturb a shared base | D2, §4.2 | two `.WithStatus` calls off one base stay independent |
| `AnyOrder` is accepted by the library's composition seams | D2, §2.2 | `Any.ListOf`, `Any.PairOf` and `.As` all accept it |
| `.WithX(IAny<T>)` keeps constrained composition open | §4.2 | `.WithReference(Any.String().StartingWith("ORD-").As(...))` yields `ORD-x9vDEd2` |
| A recipe built **outside** a scope still replays inside it | §8.2, §14.5 | two `Any.Reproducibly(20260730, …)` runs produced identical values |
| The guard-derived chain never throws | §5.3 | 500 draws through `OrderReference.Create`, no `AnyGenerationException` |
| The chain **without** guard reading throws intermittently | §5.3 | **594 / 10 000** draws threw — about 1 in 16 |
| Collection covariance needs no adapter | §5.2, §14.5 | `Any.ListOf(...)` assigned to `IAny<IReadOnlyList<string>>` |
| A value-type nullable **does** need the `.As` hop | §5.2 | `IAny<int>` is not an `IAny<int?>`; `.As(value => (int?)value)` compiles |
| The scaffolded output raises no JD diagnostic | D3, §12 | 0 diagnostics on the emitted files |
| The analyzers were genuinely loaded | D3 | a control file raised `JD006` and `JD005` in the same build |
| `<auto-generated/>` silences them | D3, §2.1 | the same control file, so marked, raised **0** — including the `JD005` error |

### 17.2 How to re-run it

Nothing about the harness is exotic; it is worth recreating whenever the library moves or its
version changes.

1. Build the library and the analyzers in `Release` (`net8.0` leg for the library).
2. Create a throwaway `net8.0` console project **outside** the repository, so no repository-wide
   build properties apply. Reference the built `JustDummies.dll` with a `<Reference>` /
   `<HintPath>`, and the built analyzer with
   `<Analyzer Include="…/JustDummies.Analyzers.dll" />`.
3. Add the domain of §4.1 (`Order`, `OrderReference` with its guarding `Create`, `Customer`,
   `OrderStatus`) and the scaffolded `AnyOrder.cs` / `AnyCustomer.cs` exactly as §4.1 specifies.
4. Add a **control file** with two known violations — a discarded constraint
   (`Any.String().NonEmpty();` as a statement, `JD006`) and a generator in an interpolated string
   (`$"{Any.Int32()}"`, `JD005`). Build, and confirm **both fire**. Without this step, "no
   diagnostics on the scaffolded file" is indistinguishable from "the analyzer never loaded" — a
   trap this verification fell into on the first attempt.
5. Prepend `// <auto-generated/>` to that same control file and rebuild: both diagnostics vanish
   and the build succeeds. That is D3's evidence.
6. Run the assertions of §17.1. For the measurement, loop
   `Any.String().As(OrderReference.Create).Generate()` 10 000 times, counting
   `AnyGenerationException`.

A note on running: if only a newer .NET runtime is installed, the `net8.0` output still runs under
`DOTNET_ROLL_FORWARD=LatestMajor`.
