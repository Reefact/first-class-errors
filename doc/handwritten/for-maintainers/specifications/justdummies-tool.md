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
* **§15 is the reasoning.** Seven decision records in this repository's ADR format, held inside
  the specification because the repository that should hold them does not exist yet. Read them
  when you want to know *why*, or when you are tempted to reverse something in §2.
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

These are the load-bearing decisions. Seven of them carry a full decision record in §15 — context,
argument, alternatives rejected, consequences. This table is the index; it holds no argument of its
own.

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
constructor body was unavailable so no guard could be read, and `unavailable` when the generator
exists in the library but not in the asset this project resolves.

That last value matters more than it looks. Without it, D4's degradation is indistinguishable from
the tool simply not knowing: a `DateOnly` parameter on a downlevel project would read as "not
inferred", when the truth is "inferred, but `Any.DateOnly()` does not exist here — retarget, or
write it yourself". One word turns a dead end into an instruction.

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

## 15. Decision records

Seven of the decisions in §2 are architectural: a future maintainer would question each of them,
and each would stand unchanged if the implementation were rewritten. In the ordinary course they
would be entered into a repository's ADR base as `Proposed`, numbered there, and accepted by the
maintainer.

**They are held inside this specification instead, because the repository that should hold them
does not exist yet.** JustDummies is expected to move out of `Reefact/first-class-errors` before
this tool is built, and these records describe a tool that will live in that new repository.
Entering them into the current base would assign them numbers — the stable handles the whole base
is built on — that would have to be abandoned or rewritten on migration, and would leave this
repository's log carrying decisions about code it no longer holds.

Keeping them here costs nothing and buys two things. The reasoning stays attached to the
specification it justifies, so the decision history travels as a single artefact rather than as a
document plus six files someone must remember to bring. And each record follows this repository's
ADR format section for section, so admission is mechanical: lift the record into the destination
repository's ADR base, assign its number there, keep its `Proposed:` date, and replace the record
here with a link.

Until then they are drafts. No status is flipped in this document; the maintainer accepts them in
the base that will hold them.

Four decisions of §2 deliberately carry no record. **D7** (ambient context only) and **D8** (the
target type's namespace) are scope and default choices, revisable without lasting consequence —
D7 is already listed as deferred in §16. **D10** (never `.OrNull()`) is one line of emitter
behaviour. All three fail the test that decides the matter: *if the implementation changed but the
decision stood, would the record need editing?* For these, the record would simply be the
implementation restated.

---

### D1 — Scaffold the generator once and hand the file to the developer

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

#### Context

The tool writes a C# file, containing a generator for a type of the developer's own code, into the
developer's own project. Three shapes exist for such a tool, all of them in use by real tooling: a
Roslyn source generator producing the file into the build's intermediate output; a file written
once into the source tree; and a file written into the source tree together with a verification
command that fails when it no longer matches what the tool would produce today.

A file in the source tree can fall out of step, silently, with the type it was derived from when
that type's constructor changes.

The library the tool serves states the absence of magic as part of its positioning: no reflection,
no object-graph filling, and its own description is "small, deterministic, explicit".

The tool cannot infer every constructor parameter. Some parameters carry invariants expressed in
ways no closed rule set can read (§9), so a scaffolded file is expected to be incomplete for some
types.

A source generator's output is not editable by the developer and does not appear in code review. A
file in the source tree is both.

#### Decision

The tool writes each generator file once and transfers ownership of it to the developer, who may
edit it freely and is never asked to regenerate it.

#### Rationale

Drift is the only serious objection to writing into the source tree, and it exists only while the
tool claims ownership of the file. Once ownership is transferred, "the file no longer matches what
the tool would produce" stops being a defect and becomes the expected state of a file the developer
has edited — which is precisely what the tool asks them to do. The objection dissolves rather than
being mitigated.

That transfer is also what makes an incomplete file acceptable. A tool that owns its output must
produce something complete or fail; a tool that hands over a skeleton may stop where its knowledge
stops and say so, which is the honest position given that some invariants are unreadable. D5 and
D6 depend on this being settled first.

Editability and review visibility serve a library whose selling point is that nothing happens
behind the developer's back. A generator they can read, step through in a debugger and modify is
consistent with that positioning; one materialised by the compiler is not.

Removing ownership removes an entire class of machinery with it: no verification verb, no
regeneration protocol, no drift detection, no rules about which regions may be hand-edited. For a
tool whose first design rule is that it must be trivial to adopt, the machinery not built is worth
more than the guarantees it would have offered.

#### Alternatives Considered

##### A Roslyn source generator

Considered because it makes drift structurally impossible: it re-runs on every build, so its output
cannot lag the type.

Rejected because it forfeits everything that the file being real buys. The developer cannot edit
it, cannot complete the parameters the tool failed to infer, and reviewers never see it. It also
has no useful way to leave work unfinished, so the unresolved-parameter case would have to fail the
build with no place for the developer to act.

##### A written file plus a verification verb

Considered because it is the standard answer to drift for committed generated artefacts, and
integrates cleanly into continuous integration.

Rejected because verification and editing are mutually exclusive. A command that fails whenever the
file differs from a fresh generation forbids the very editing this tool exists to invite. Keeping
both would mean encoding which regions belong to the tool and which to the developer — more
machinery than the whole feature is worth.

#### Consequences

**Positive.** The tool has one verb and no protocol. The scaffolded file is ordinary code:
reviewable, debuggable, editable. The unresolved-parameter path of D6 becomes available.

**Negative.** A generator can fall behind its type. Adding a constructor parameter breaks the
generator's compilation, which surfaces the problem; changing a parameter's invariant does not — the
generator keeps producing values the constructor now rejects, and only a failing test reveals it.

**Risks.** A developer may expect regeneration to preserve their edits. Mitigated by the emitted
header, which states that regeneration overwrites and that the type is `partial` so neighbouring
files survive, and by `--force` being required to overwrite at all.

#### Follow-up Actions

* State the "this file is yours" position prominently in the tool's user documentation: it inverts
  the expectation set by most scaffolding tools.

#### References

* §1, §3, §4.3 of this specification.

---

### D2 — Make the emitted generator a first-class `IAny<T>`

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

#### Context

`IAny<T>` is the library's composition seam: `As`, `Combine`, the collection generators and the
choice generators all consume and produce it (§14.4).

The interface is documented as an immutable recipe, and every generator in the library honours
that — each fluent constraint returns a new instance (§14.5).

The analyzers' `Usage` category recognises a generator as the `IAny<T>` interface itself or any
type implementing it, rather than as a fixed list of built-in types (§14.6).

The emitted type exposes one fluent method per constructor parameter, which gives it the shape of a
builder. Builders in the wider ecosystem conventionally mutate and return `this`.

#### Decision

The emitted type implements `IAny<T>` and is immutable, every `With` method returning a new
instance.

#### Rationale

Implementing the seam is what makes nested aggregates work with no additional code. An emitted
generator is directly usable as an element generator, a `Combine` operand or an `As` source;
without the interface, either the tool would emit adapters or the developer would write them.

The second benefit is less obvious and worth as much: the `Usage` analyzers key on the interface,
so an emitted type that implements it is covered by them exactly as a built-in generator is. That
coverage matters more here than anywhere else, because the emitted file is the one the developer
edits (D3), often while meeting this API for the first time.

Immutability is not a style preference but the seam's documented contract. A mutating `With` would
make the emitted type the only mutable generator in the ecosystem, and would behave surprisingly:
two generators derived from a shared base would interfere with each other. The cost is one
allocation per `With` call, on a code path that is not hot.

#### Alternatives Considered

##### A mutating builder returning `this`

Considered because it is the conventional builder shape and allocates less.

Rejected because it contradicts the documented contract of the interface it would implement, and
because deriving two generators from a shared base would silently corrupt both.

##### A plain type exposing `Generate`, not implementing `IAny<T>`

Considered because it keeps the emitted file free of any library interface.

Rejected because it forfeits both benefits at once: no composition with the library's seams, and no
analyzer coverage on the file that needs it most.

#### Consequences

**Positive.** Composition with every library seam comes free. Four analyzer rules extend to the
emitted type at no cost.

**Negative.** One allocation per `With` call. The private all-arguments constructor grows with the
parameter count, so the emitted file is verbose for wide constructors.

**Risks.** If the library ever relaxed the immutability contract, the emitted shape would be
stricter than required — harmless, and no action would be needed.

#### Follow-up Actions

* None.

#### References

* §4.2, §14.4, §14.5, §14.6 of this specification.

---

### D3 — Leave the scaffolded file open to the JustDummies analyzers

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

#### Context

The analyzers ship inside the library's own package, so every consumer of the library receives them
automatically (§14.1).

All 27 of them exempt generated code (§14.6). Roslyn classifies a file as generated when it is
named `*.g.cs` or `*.generated.cs`, or when it opens with an auto-generated header comment.

The exemption was measured. One file containing exactly two violations — a `JD006` warning and a
`JD005` error — was compiled twice, changing nothing but its first line: without the header, both
were reported and the build failed; with `// <auto-generated/>`, neither was reported and the build
succeeded (§17).

The scaffolded file is the file the developer edits (D1), and it may leave the tool incomplete
(D6).

The one way the emitter can produce a chain the library rejects at run time is two guard-derived
constraints landing on the same axis (§5.3). `JD015` and `JD023` detect exactly that class of
unsatisfiable chain.

The ecosystem convention is to mark generated files, chiefly so that style analyzers do not fire on
machine-written code.

#### Decision

The scaffolded file carries no generated-code marker, so the JustDummies analyzers analyse it as
they analyse hand-written code.

#### Rationale

The exemption is total, and the measurement shows how quietly it applies: a compile error became
silence on a one-line change. Marking the file would make it the only file in the developer's test
project outside the library's own safety net.

It would also be the worst possible file to exempt. It is the one the developer will edit, using an
API they may be meeting for the first time, in a file the tool has just told them to complete.

The coverage additionally backstops the emitter's own mistakes. The same-axis rule of §5.3 removes
the conflicting-chain case by construction, but a defect in that rule would otherwise surface only
as a run-time exception; with the file analysed, it surfaces in the editor instead.

The conventional reason for marking — sparing machine-written code from style rules — does not
apply to a file that is, by D1, not machine-owned. It is the developer's code from the moment it is
written, and it should answer to the same rules as its neighbours.

#### Alternatives Considered

##### Marking the file with an auto-generated header

Considered because it is the ecosystem convention, and because it would spare a scaffolded file
from the developer's own style analyzers on first generation.

Rejected because it disables every JustDummies diagnostic on that file, which is the opposite of
what a file about to be hand-edited against an unfamiliar API needs. The measurement makes the cost
concrete: an error-severity diagnostic disappears without trace.

##### Naming the file `*.g.cs`

Considered as a lighter-touch variant of the same idea.

Rejected for the same reason, plus one more: the name asserts machine ownership, which D1 denies.

#### Consequences

**Positive.** The scaffolded file is covered by the same diagnostics as the code around it, and
emitter mistakes surface at edit time rather than at run time.

**Negative.** The developer's own analyzers and style rules also fire on it, so a first scaffold may
need a formatting pass to match house style. The emitter reduces this by writing explicit types and
conventional layout, but it cannot match every configuration.

**Risks.** A future emitter change could introduce a diagnostic into every scaffolded file at once.
Mitigated by the compile-the-output tests (§12), which fail on any `JD` diagnostic.

#### Follow-up Actions

* Keep the control file in the compile-the-output test. Without a known violation asserted to fire,
  the test cannot distinguish "no diagnostics" from "the analyzers never loaded", and silently
  becomes a no-op — the trap this specification's own verification fell into on its first attempt
  (§17.2).

#### References

* §2, §5.3, §14.6, §17 of this specification.

---

### D4 — Emit only members resolved in the target compilation

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

#### Context

The library ships two divergent assets. The modern one carries five generator entry points that do
not exist on the downlevel one, because the underlying framework types do not exist there (§14.1).

The unsigned integer generators expose no `Positive` or `Negative` constraint, since an unsigned
type cannot express either (§14.3).

The tool holds no reference to the library (D9), so it cannot see the library's API at its own
compile time.

The developer's compilation is the authority on what is actually available in their project: their
target framework selects the asset, and their package version selects the surface.

A member emitted but absent is a compile error in the developer's project, attributed to the tool.

#### Decision

The engine emits a JustDummies member only after resolving that member in the developer's
compilation.

#### Rationale

The alternative is a table, inside the tool, of what exists per library version and per target
framework. It would need maintaining for every library release, would be wrong for any version the
tool predates, and would encode facts the compilation already knows exactly.

Resolution replaces four independent special cases with one rule: the asset split, the unsigned
numeric surface, the tool being older or newer than the library, and the developer's own generators
being discovered. None of them has to be named anywhere in the emitter.

The failure mode it produces is the right one. A member that cannot be resolved turns the parameter
into an unresolved one (D6) — a state the tool already handles and reports — rather than an
emission the developer meets as a compile error they did not cause and cannot interpret.

It also makes the public-API guarantee free rather than something to enforce: anything resolvable
in the compilation is by construction part of the library's shipped public surface, so the tool
cannot emit against an internal member or one outside the compatibility baseline.

#### Alternatives Considered

##### A hard-coded table of members per library version

Considered because it is simpler, needs no symbol lookup, and makes the emitter's knowledge
explicit and reviewable.

Rejected because it is unmaintainable across versions and simply wrong for any library version
released after the tool.

##### Referencing the library and emitting against its compile-time types

Considered because it would let the compiler check the emitter's own use of the API, removing the
silent-typo failure mode that D9 accepts.

Rejected because it contradicts D9, and because it would answer the wrong question anyway: the
version the tool references is not the version in the developer's project.

#### Consequences

**Positive.** The tool is correct against any library version and any target framework, holding no
per-version knowledge at all.

**Negative.** Degradation is quiet by nature: a member that fails to resolve simply does not appear
in the emission, and without deliberate reporting the developer cannot tell a parameter the tool
could not infer from one whose generator exists but is unavailable here.

**Risks.** A resolution defect — looking up a wrong metadata name — would degrade everything to
TODOs at once, which reads as the tool not working rather than as a bug. Mitigated by the
asset-selection test (§12), which asserts both the present and the absent case.

#### Follow-up Actions

* The console provenance column must distinguish "not inferred" from "unavailable in this
  compilation" (§6), so the quiet degradation above is visible rather than merely silent.

#### References

* §5.2, §5.3, §6, §14.1, §14.3 of this specification.

---

### D5 + D6 — Seed generators from constructor guards, and leave the rest as a compile error

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

#### Context

Unconstrained generators draw their full domain: the string generator yields zero to sixteen
characters, so it can return the empty string, and the integer generator draws the whole range
including negatives (§14.5).

Domain constructors commonly reject part of that domain.

This was measured on a real validating factory from this repository: an unconstrained string
generator composed onto it threw 594 times in 10 000 draws, roughly one in sixteen (§17).

Guard clauses at the head of a constructor are the dominant validation idiom in the code this tool
targets.

The tool has the constructor body as source for any type in the developer's solution, and does not
for a type coming from a package.

Some invariants are not expressed as guards at all — validation delegated to a helper, a guard
library, or a rule spanning two parameters.

The developer runs the tool and opens the resulting file within the same minute.

#### Decision

The engine derives constraints from a closed set of recognised constructor guard clauses, and emits
an identifier that does not exist for any parameter whose generator it cannot infer.

#### Rationale

Without guard reading the tool's default output is not merely imprecise, it is harmful: it
manufactures, inside the developer's test suite, the intermittent failure the library exists to
eliminate. One failure in sixteen is worse than no tool at all, because it discredits the library
at the moment of first use.

A closed, syntactic set bounds the risk. Reading guards is not inference about intent; each
recognised form maps to exactly one constraint, and anything outside the set is ignored.
Conservative matching — one parameter, no boolean composition, constant operands — under-reports
rather than misfires, which is the correct bias here: a missing constraint yields a value the
constructor may reject and a visible failure, whereas a wrong constraint yields a value that
silently mis-exercises the test.

For the parameters that remain unresolved, a compile error is the cheapest signal available. The
developer is in the file, having just run the tool; the compiler names the parameter in its own
message, and that message reaches the editor, the error list and continuous integration alike. A
signal delivered later costs more, and one never delivered costs most.

Shipping a file that does not compile is defensible only because of D1. A tool that owned its
output could not do it; a tool handing over a skeleton can, and stating the gap plainly is more
honest than a file that compiles and fails later.

#### Alternatives Considered

##### Neutral generators, leaving all tightening to the developer

Considered because it makes the tool claim nothing it cannot prove, which is attractive for a
library built on precision.

Rejected on the measurement. The default output would fail intermittently for most validating
constructors, which is the highest-cost failure mode available and the one the library was built to
remove.

##### A run-time exception for unresolved parameters

Considered because the file then compiles, which is friendlier at first sight.

Rejected because it defers the signal past the moment the developer is looking at the file, and
converts a scaffolding gap into a test failure whose cause is a line they never read.

##### Omitting the unresolved parameter from the recipe

Considered because it is the most elegant of the three: the generator would simply require the
developer to supply that parameter.

Rejected because it is silent. The generator becomes partially usable without saying so, and the
gap surfaces as a null or a default deep inside a test.

##### A declaration file mapping types to their construction

Considered because it would let the developer teach the tool once, covering invariants no guard
expresses, and would make composition correct for value objects in general rather than only for
guarded ones.

Rejected for the first version because it converts the tool into a convention system, contradicting
the design rule that nothing be configured before first use. Left open in §16.

#### Consequences

**Positive.** The emitted default works for the dominant validation idiom. Unresolved parameters
are impossible to overlook.

**Negative.** A scaffolded file may not compile until edited, which will surprise anyone expecting
scaffolding to produce working code. Invariants outside the recognised set still yield values the
constructor rejects.

**Risks.** The recognised set may match a guard whose meaning it mistakes, producing a constraint
that is wrong rather than absent — the one outcome worse than inferring nothing. Mitigated by the
conservative matching conditions and the same-axis conflict rule; the own-code test (§12) is the
check most likely to catch it, because it runs the emitter over code written for other reasons.

#### Follow-up Actions

* Every addition to the recognised guard set needs a case in the resolver suite and, where
  possible, an instance in the own-code test.

#### References

* §5.3, §5.5, §9, §14.5, §17 of this specification.

---

### D9 — Give the scaffolder no dependency on the JustDummies package

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

#### Context

The tool emits code that calls the library's API, but never calls that API itself.

If the tool referenced the library, the developer's project would hold two versions of it: the one
the tool was built against and the one the project actually references.

The library's own analyzers already resolve every library symbol by metadata name against the
consumer's compilation, referencing no library assembly; a rule whose type is absent from the
compilation simply stays silent.

The host repository publishes package families on release trains, each train shipping its members
at a single version.

#### Decision

Neither the engine nor the CLI references the JustDummies package or project; every JustDummies
symbol is resolved by metadata name against the developer's compilation.

#### Rationale

The tool's correctness question is never "what does the library version I was built against offer"
but "what does the library version in this project offer". A reference answers the first while
implying the second, which is exactly how a tool begins emitting code that does not compile for
someone on a different version.

Together with D4, removing the reference makes version skew structurally impossible rather than
merely tested. There is no version pair to test, because the tool holds no version of the library
at all.

The library's analyzers already work this way, which demonstrates the pattern is sufficient for
exactly this job: symbols resolved by name, graceful silence when a type is absent.

It also decouples the release trains. The tool ships when the tool changes and the library when the
library changes, and neither forces a release of the other.

#### Alternatives Considered

##### Referencing the library and versioning the two in lockstep

Considered because it lets the compiler check the emitter's own use of the API, and because a
matching version number is an obvious compatibility story to present to users.

Rejected because lockstep only guarantees the tool matches the library it shipped alongside, not
the one in the developer's project — the only case that matters — and because it would force a tool
release for every library release.

#### Consequences

**Positive.** No version matrix, no compatibility question to manage, and independent release
cadences.

**Negative.** The emitter's knowledge of the API is expressed as strings, so a mistyped member name
is not a compile error in the tool. It surfaces as an unresolved member, which D4 turns into a
TODO — output that is wrong but quiet.

**Risks.** That quiet failure mode is the real cost of this decision. Mitigated by the
compile-the-output and own-code tests (§12), which exercise the emitted expressions against a real
compilation, where a mistyped member appears as a TODO in a position that should have carried a
value.

#### Follow-up Actions

* The tool's package must assert at packing time that it declares no JustDummies dependency
  (§13.6) — the executable form of this decision.

#### References

* §10.4, §13.6, §14.2 of this specification.

---

### D11 — Keep the scaffolding engine loadable by a Roslyn host

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

#### Context

The CLI must open a project on disk, which requires an MSBuild-aware workspace; that is available
on modern .NET only, not on the downlevel target.

An assembly loaded by a consumer's compiler — an analyzer, a code fix, a code refactoring — must
target the downlevel framework and be compiled against the lowest Roslyn version it has to load
under. Built against a higher one, it fails to load, and it fails silently.

A Roslyn code refactoring is a plausible second surface for the engine: the library already ships
analyzers, so the packaging and load path exist, and applying a document is the natural operation
of a refactoring.

The engine's work is symbol inspection, syntax reading and string building. It needs no file
system, no console and no MSBuild.

The test surface described in §12 is dominated by engine behaviour rather than by command
plumbing.

The host repository measures mutation on every project whose code ships or runs (§13.5).

#### Decision

The scaffolding engine is a separate library targeting the downlevel framework and compiled against
the analyzer Roslyn floor, performing no input or output, with the CLI as a shell over it.

#### Rationale

The constraint is asymmetric in time. Targeting the floor costs the engine almost nothing today,
because none of its work needs a modern API. Discovering later that it must be loadable by a
compiler means re-verifying every API it uses against that floor, throughout a codebase written
without the constraint in mind. Paying now is cheap and paying later is not, which is what
justifies building for a consumer that does not yet exist.

The boundary the future consumer requires is the same one the present code wants. An engine that
takes a compilation and returns a model, with no output of its own, is the testable shape: the
resolver and emitter can be exercised over an in-memory compilation, with no project on disk and no
argument parsing in the way.

Separating the two also separates the mutation budget. Command plumbing and the resolution rules do
not deserve equal scrutiny, and a single project cannot express that difference.

The argument that the CLI may grow further verbs justifies none of this. Extra verbs are extra
files above the same engine, and after D1 the plausible list is nearly empty in any case.

#### Alternatives Considered

##### One CLI project holding everything

Considered because it is the smallest thing that works for a tool with a single verb, and avoids
two projects and two test suites.

Rejected because it closes the Roslyn-host path at the moment of creation, and because it forces
every engine test through the CLI's dependencies.

##### A separate engine targeting modern .NET

Considered because it keeps the boundary, and with it the testing and mutation benefits, without
accepting the downlevel constraint.

Rejected because the boundary's principal purpose is the consumer that this variant excludes.

#### Consequences

**Positive.** The engine is loadable by a compiler host unchanged. Its tests need no project on
disk. Mutation measurement can be aimed where it pays.

**Negative.** Two projects and two test suites for one verb. The engine is written against the
downlevel framework, so modern convenience APIs are unavailable to it.

**Risks.** The Roslyn floor pin can drift if the engine's package reference is allowed to float,
and the resulting load failure is silent. Mitigated by pinning to the same floor property the
analyzer package uses (§13.2).

#### Follow-up Actions

* If a code refactoring is ever built, the engine will need publishing as its own package (§16).

#### References

* §10, §12, §13.2, §13.5, §16 of this specification.

---

### A library follow-up, not a decision record

`Any.Fixed<T>(value)` — an `IAny<T>` returning a constant — would let the emitter drop the nested
`FixedValue<TValue>` helper of §4.2. `Any.OneOf(value)` almost fills the role but rejects `null`
and consumes a draw (§14.5). This is an addition to the library's public API rather than a decision
about the tool, so it belongs to the library's own decision base and is **not** required for v1.0.

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
