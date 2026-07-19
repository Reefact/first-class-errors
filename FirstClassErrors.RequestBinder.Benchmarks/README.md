# FirstClassErrors.RequestBinder.Benchmarks

The measurement harness behind issue #151 (*binder: per-property binding uses
uncached reflection and allocates a path string on the happy path* — review
finding 15/19, gated on a decision before the v1 API freeze). It measures the
binder's **happy path**: the per-request cost paid when every argument is
valid, which is the common case at a primary-adapter boundary.

Run it with:

```bash
dotnet run -c Release --project FirstClassErrors.RequestBinder.Benchmarks -- --filter '*'
```

The project is built by CI (it lives in the solution, so it cannot bit-rot)
but is never packed and never run as a test. Timings below come from a
4-core CI-class container — treat them as relative, not absolute; allocated
bytes are exact (`MemoryDiagnoser`).

## What each scenario isolates

| Scenario | Isolates |
|---|---|
| `Manual_Scalars5_HappyPath` (baseline) | The floor: hand-written null checks + the same value objects, no binder. |
| `FullBooking_HappyPath` | The canonical `BookingBinder` shape: 9 inputs, a nested binder, three lists. |
| `Scalars5_HappyPath` / `Scalar1_*` | Scalar scaling; the marginal per-property cost, reference and nullable-value-type. |
| `Scalars5_HoistedSelectors_HappyPath` | The same public API with `Expression<>` selectors pre-built in static fields — everything **except** the call-site expression-tree allocation. |
| `ListOfStrings10_HappyPath` | Per-element costs (paths, list growth). |
| `Nested_Stay_HappyPath` | Nested-binder prefixes. |
| `OutOfDtoArgument_HappyPath` | The no-reflection path (`Argument(...)`): what the DTO path could approach. |
| `Scalars5_OneMissing_FailurePath` | The failure path — optimizations must not regress it. |
| `Micro_ExpressionTreeSelector_Allocation` / `Micro_CachedDelegateSelector_Allocation` | The irreducible call-site cost of an expression-tree selector vs a compiler-cached delegate — what any API alternative would buy. |

## Results — after the #151 optimization (this tree)

BenchmarkDotNet, net10.0, Release, 4-core container:

| Method | Mean | Allocated |
|---|---:|---:|
| FullBooking_HappyPath | 9 712 ns | 11 488 B |
| Scalars5_HappyPath | 2 805 ns | 3 600 B |
| Scalars5_HoistedSelectors_HappyPath | 342 ns | 880 B |
| Scalar1_String_HappyPath | 481 ns | 760 B |
| Scalar1_NullableInt_HappyPath | 432 ns | 728 B |
| ListOfStrings10_HappyPath | 786 ns | 1 432 B |
| Nested_Stay_HappyPath | 981 ns | 1 472 B |
| OutOfDtoArgument_HappyPath | 81 ns | 216 B |
| Scalars5_OneMissing_FailurePath | 4 492 ns | 5 704 B |
| Manual_Scalars5_HappyPath (floor) | 62 ns | 184 B |
| Micro_ExpressionTreeSelector_Allocation | 416 ns | 488 B |
| Micro_CachedDelegateSelector_Allocation | 1 ns | 0 B |

Byte-exact before/after (same session, `GC.GetAllocatedBytesForCurrentThread`
probe, pre-#151 HEAD vs this tree):

| Scenario | Before | After | Δ |
|---|---:|---:|---:|
| Scalars5 | 3 728 B | 3 600 B | −128 B |
| Scalar1_String | 896 B | 848 B | −48 B |
| Scalar1_NullableInt | 920 B | 816 B | **−104 B (−11 %)** |
| OutOfDtoArgument | 352 B | 336 B | −16 B |
| ListOfStrings10 | 2 504 B | 1 624 B | **−880 B (−35 %)** |
| Nested_Stay | 1 584 B | 1 536 B | −48 B |
| FullBooking | 13 256 B | 11 992 B | **−1 264 B (−10 %)** |
| Failure_OneMissing | 5 880 B | 5 784 B | −96 B |

Time deltas (BenchmarkDotNet, before → after): lists −63 %
(2 112 → 786 ns), nullable value type −39 % (703 → 432 ns), full booking
−16 % (11 558 → 9 712 ns), hoisted scalars −17 % (410 → 342 ns); plain
scalars are within run-to-run noise because their cost is dominated by the
call-site expression tree (below).

## Reading the numbers

1. **The issue's premise re-measured.** ~700 B/property is confirmed
   (3 600 B / 5 scalars ≈ 720 B), but its decomposition is not what the
   issue assumed. On the pre-#151 code, a top-level scalar with the default
   name provider allocated **no path string at all** (the path was the
   runtime-cached `PropertyInfo.Name`); eager path allocation only cost on
   list elements (~3 allocations per element), nested prefixes, and custom
   name providers.

2. **~70–75 % of the per-property cost is the call-site expression tree**
   (`d => d.GuestEmail`): ~488 B / ~416 ns per property, allocated by the
   C# compiler in the *caller's* body — Roslyn caches delegate lambdas, never
   expression-tree lambdas. No library-internal change can remove it; hoisting
   the selectors into static fields removes it today with the same API
   (2 805 → 342 ns, 3 600 → 880 B for 5 properties).

3. **What #151 removed** (library-internal, no public API change):
   * per-call `PropertyInfo.GetValue` reflection → compiled getters cached
     per (DTO type, property), which also deletes the nullable-value-type
     box and the `Nullable.GetUnderlyingType` check on every re-bind;
   * eager list-element paths (`Tags[0]`…`Tags[9]`) and complex-list element
     prefixes (`Guests[2]`) → built only when that element actually fails;
   * eager DTO-property paths and name-provider calls → deferred to failure
     recording (a custom, allocating provider now costs zero on an all-valid
     bind; a bound complex property still resolves its one prefix segment);
   * the per-binding error `List<>` (top-level, nested, and per element) →
     created on first failure;
   * unsized result lists → pre-sized from `ICollection<T>.Count`.

4. **What remains is the API's own shape**: one converter stage + one field
   token per property (~88 B), one `Outcome<T>` per converter call (~32 B),
   and the expression tree. Removing any of those is an API decision, not an
   optimization — see ADR-0023 (keep expression-tree selectors for v1; a
   delegate+name overload family or a source generator are the additive
   post-v1 options, with the `Micro_*` pair quantifying their ceiling).

5. **Context for the absolute numbers**: a full realistic request binds in
   ~10 µs / ~11 KB against tens-to-hundreds of µs of deserialization, I/O
   and domain work around it. The failure path is unchanged (−96 B, time in
   noise), and the first bind of each (DTO type, property) pays a one-time
   getter compilation (~150 µs, amortized process-wide; environments without
   IL emission fall back to the expression interpreter, and, failing that, to
   the original reflection read).
