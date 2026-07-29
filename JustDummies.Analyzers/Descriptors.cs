using Microsoft.CodeAnalysis;

namespace JustDummies.Analyzers;

/// <summary>
///     The <see cref="DiagnosticDescriptor" /> for every JustDummies rule. One field per JDxxx.
/// </summary>
internal static class Descriptors {

    public static readonly DiagnosticDescriptor AsyncBodyPassedToReproducibly = new(
        id: DiagnosticIds.AsyncBodyPassedToReproducibly,
        title: "An asynchronous body is passed to Any.Reproducibly",
        messageFormat: "Pass the asynchronous body to Any.ReproduciblyAsync and await it: Any.Reproducibly takes an Action, so an async lambda runs as 'async void' and its failures never reach the test",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.Reproducibly takes a synchronous Action. An async lambda bound to it becomes 'async void', whose exceptions escape the reproducible scope entirely and never fail the test. Use Any.ReproduciblyAsync(Func<Task>) and await it.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.AsyncBodyPassedToReproducibly));

    public static readonly DiagnosticDescriptor DiscardedReproduciblyAsyncResult = new(
        id: DiagnosticIds.DiscardedReproduciblyAsyncResult,
        title: "The task returned by Any.ReproduciblyAsync is discarded",
        messageFormat: "Await the task returned by Any.ReproduciblyAsync; discarding it silently drops the body's failures",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.ReproduciblyAsync returns a Task that faults with the body's exception. Discarding it (as a bare statement or via '_ =') lets a failing test pass green. Await it.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DiscardedReproduciblyAsyncResult));

    public static readonly DiagnosticDescriptor AwaitableBodyPassedToReproducibly = new(
        id: DiagnosticIds.AwaitableBodyPassedToReproducibly,
        title: "An asynchronous body reaches Any.Reproducibly without being awaited",
        messageFormat: "Pass the asynchronous body to Any.ReproduciblyAsync and await it: bound to Any.Reproducibly's Action the body is never awaited, so the scope returns before the assertions run and their failures never reach the test",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.Reproducibly takes a synchronous Action. A synchronous lambda whose body produces a task drops that task, and an 'async void' method group bound to the Action raises its failures outside the scope entirely. Neither is reported by the compiler — CS4014 does not fire when the enclosing lambda is not itself async. Use Any.ReproduciblyAsync(Func<Task>) and await it.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.AwaitableBodyPassedToReproducibly));

    public static readonly DiagnosticDescriptor DiscardedSeedingResult = new(
        id: DiagnosticIds.DiscardedSeedingResult,
        title: "The result of a seeding call is discarded",
        messageFormat: "Do not discard the result of Any.{0}: {1}",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.UseSeed returns the handle that closes the scope it opened; dropping it leaves the seed pinned for whatever runs next in the same execution context, silently making later tests replay one fixed sequence. Any.WithSeed returns an isolated context and pins nothing, so discarding it is dead code at a call site that reads as if the run had been seeded.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DiscardedSeedingResult));

    public static readonly DiagnosticDescriptor GeneratorRenderedAsText = new(
        id: DiagnosticIds.GeneratorRenderedAsText,
        title: "A generator is rendered as text instead of the value it would draw",
        messageFormat: "Call Generate() on the {0}: rendered as text a generator yields its own type name, not an arbitrary value",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A generator is an immutable recipe, and no JustDummies generator overrides ToString(). Interpolating, concatenating or calling ToString() on one therefore produces the builder's type name — a non-empty, plausible, run-invariant string that flows into the code under test as if it were an arbitrary value. Materialize the value with Generate().",
        helpLinkUri: HelpLinks.For(DiagnosticIds.GeneratorRenderedAsText));

    public static readonly DiagnosticDescriptor DiscardedGeneratorResult = new(
        id: DiagnosticIds.DiscardedGeneratorResult,
        title: "The generator returned by a constraint is discarded",
        messageFormat: "Assign the result of {0} back: a generator is an immutable recipe, so a constraint whose result is discarded constrains nothing",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every constraint returns a new generator rather than mutating the receiver. A discarded result therefore silently drops the invariant the arrangement declared, and the generator keeps drawing from the wider domain — so the test passes on most runs and fails on the one that draws outside it, with a value nobody can reproduce.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DiscardedGeneratorResult));

    public static readonly DiagnosticDescriptor DrawOutsideThePinnedScope = new(
        id: DiagnosticIds.DrawOutsideThePinnedScope,
        title: "An arbitrary value is drawn before [Reproducible] pins the seed",
        messageFormat: "Draw this value inside the test body: {0} runs before [Reproducible] opens the seed scope, so the seed the failure reports does not replay it",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "xUnit constructs the test-class instance, and awaits IAsyncLifetime.InitializeAsync, before running the hooks the adapter pins the seed from. A value drawn there comes from the unseeded ambient source, so the test advertises full reproducibility while part of its arrangement is unpinned: pinning the reported seed does not bring the failure back.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DrawOutsideThePinnedScope));

    public static readonly DiagnosticDescriptor ArbitraryValueInTheoryData = new(
        id: DiagnosticIds.ArbitraryValueInTheoryData,
        title: "A theory's data provider draws an arbitrary value",
        messageFormat: "Draw this value in the test body, or let the provider yield the generator: theory data is produced at discovery, before any seed is pinned, and every case shares the one value",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "xUnit evaluates a theory's data provider at discovery time, once for the whole run and outside every seed scope. The drawn value is therefore shared by every case of the theory, replayable from no reported seed, and constant where the theory reads as if it enumerated arbitrary cases.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ArbitraryValueInTheoryData));

    public static readonly DiagnosticDescriptor DrawInStaticInitializer = new(
        id: DiagnosticIds.DrawInStaticInitializer,
        title: "An arbitrary value is drawn in a static initializer",
        messageFormat: "Hold the generator rather than the value: a static initializer draws once for the whole suite, under whichever test happened to run first",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type initializer runs once, lazily, when the first test touches the type. The value is drawn under whatever ambient context that test had pinned, is shared by every other test in the class, and is replayable from none of their reported seeds — so the tests become order-dependent and stop varying between runs. Store the generator in the static field and call Generate() per test.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DrawInStaticInitializer));

    public static readonly DiagnosticDescriptor ReproducibleOnNonTestMethod = new(
        id: DiagnosticIds.ReproducibleOnNonTestMethod,
        title: "[Reproducible] is applied to a method that is not a test",
        messageFormat: "Remove [Reproducible] from '{0}' or make it a test: xUnit collects the attribute from the test method, its class and the assembly only, so here it pins nothing",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The adapter's hooks are collected from a test method, its declaring class and the assembly. On a helper — or on a method whose [Fact] was removed during a refactor — the attribute is never read: it pins no seed and reports none. Because a working [Reproducible] is silent on a passing test by design, nothing else distinguishes the inert form from the working one.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ReproducibleOnNonTestMethod));

    public static readonly DiagnosticDescriptor GeneratorWhereValueExpected = new(
        id: DiagnosticIds.GeneratorWhereValueExpected,
        title: "A generator reaches a position that expected its value",
        messageFormat: "Call Generate() on the {0}: passed where an object is expected, the recipe itself is stored, compared or asserted on, never the value it would draw",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        // Opt-in, on the evidence ADR-0059's follow-up asked for rather than on intuition: dogfooded over this
        // repository's suites the rule found no true positive and two false ones, both in a convention test that
        // collects generators into a List<object> on purpose. That shape is indistinguishable from the theory-row
        // mistake this rule exists to catch, so it cannot be narrowed away. The rule earns its keep in a consumer
        // suite, where object-typed assertion helpers are common and reflection over generators is not.
        isEnabledByDefault: false,
        description: "Generators are reference types, so an object, dynamic or params object[] position accepts one with no conversion — the residue the removal of the implicit conversions could not close. An assertion helper taking object then inspects the recipe (Assert.NotNull(Any.String()) is green for ever), a theory row carries the recipe into the code under test, and Equals against a value is false for every run and every seed. Opt-in: a suite that manipulates generators as objects on purpose would see this fire on legitimate code.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.GeneratorWhereValueExpected));

    public static readonly DiagnosticDescriptor GeneratorPooledAsValue = new(
        id: DiagnosticIds.GeneratorPooledAsValue,
        title: "A choice pool is built from generators rather than values",
        messageFormat: "Call Generate() on each pooled generator: Any.{0} inferred a pool of recipes, so drawing from it yields a recipe rather than a value",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.OneOf(Any.Int32(), Any.Int32()) compiles and infers the builder type as the pool's element type, so the pool holds recipes. What makes this a trap rather than an obvious mistake is that the surface is inconsistent about it: pooling generators of different types fails type inference and the compiler catches it, while two of the same type bind cleanly.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.GeneratorPooledAsValue));

    public static readonly DiagnosticDescriptor HeldCollectionPassedToOneOf = new(
        id: DiagnosticIds.HeldCollectionPassedToOneOf,
        title: "A held collection is passed to Any.OneOf, making a pool of one",
        messageFormat: "Use Any.ElementOf to draw from the collection's elements: passed to OneOf it binds T to {0}, so the pool holds one item and every draw returns the same one",
        category: DiagnosticCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.OneOf takes params T[], so a single collection argument binds T to the collection type itself rather than to its elements. The call compiles, draws succeed, and every one of them returns the same collection — the arbitrary choice the test claims to make never varies. Any.ElementOf is the entry point that draws from a collection's elements; an explicit type argument states the opposite intent and is left alone.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.HeldCollectionPassedToOneOf));

    public static readonly DiagnosticDescriptor RejectedConstantArgument = new(
        id: DiagnosticIds.RejectedConstantArgument,
        title: "A constant argument is one the generator rejects",
        messageFormat: "{0} throws for this argument: {1}",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The argument is a compile-time constant the generator's own guard refuses, so the call throws every time it runs. Nothing is decided at run time that is not already decided here, and the failure otherwise surfaces late — inside an arrange helper shared by many tests, where it reads as a library problem rather than as the transposition typo it usually is. The run-time guards stay for every argument this cannot see.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.RejectedConstantArgument));

    public static readonly DiagnosticDescriptor StringConstraintsAdmitNoValue = new(
        id: DiagnosticIds.StringConstraintsAdmitNoValue,
        title: "The declared string constraints admit no value",
        messageFormat: "No string satisfies this chain: {0}",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The constraints contradict each other for the constants written at the call site, so the chain throws a ConflictingAnyConstraintException the moment the arrange line runs. This is the case ADR-0035 names as the one an analyzer should carry: Numeric().StartingWith(\"ORD-\") conflicts while Numeric().StartingWith(\"123\") does not, from identical call sites and identical static types — only the argument value tells them apart.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.StringConstraintsAdmitNoValue));

    public static readonly DiagnosticDescriptor CollectionConstraintsAdmitNoValue = new(
        id: DiagnosticIds.CollectionConstraintsAdmitNoValue,
        title: "The declared collection constraints admit no value",
        messageFormat: "No collection satisfies this chain: {0}",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The count constraints contradict each other for the constants written at the call site, or the chain asks for more distinct elements than its element generator can produce — the cardinality gate ADR-0013 records. Both throw at declaration time, so the value here is a build-time red rather than an arrange-time one: the chain usually sits in a helper several call frames away from the test that dies on it.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.CollectionConstraintsAdmitNoValue));

    public static readonly DiagnosticDescriptor EnumUniverseViolation = new(
        id: DiagnosticIds.EnumUniverseViolation,
        title: "An enum constraint steps outside the generator's universe",
        messageFormat: "Any.Enum draws only declared members: {0}",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.Enum<T>() draws uniformly across T's declared members and never an undeclared numeric value. That is deliberate and surprising: on a [Flags] enum, writing a combination in OneOf is the natural thing to do and the generator refuses it unless AllowingCombinations() is declared. An exclusion that removes every declared member is the same category error from the other side.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.EnumUniverseViolation));

    public static readonly DiagnosticDescriptor NestedReproducibilityScope = new(
        id: DiagnosticIds.NestedReproducibilityScope,
        title: "A reproducibility scope is nested inside another",
        messageFormat: "This Any.Reproducibly runs inside {0}, whose reported seed then replays nothing: the inner scope draws a fresh seed on every run",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.Reproducibly takes its seed from Guid.NewGuid().GetHashCode(), not from the ambient source, so an inner scope ignores whatever the outer one pinned and draws afresh every run. The outer mechanism still reports its own seed, so the failure names a seed that reproduces nothing — a wrong instruction rather than a wrong result. The seeded overload is left alone: pinning a chosen seed inside is deliberate.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.NestedReproducibilityScope));

    public static readonly DiagnosticDescriptor CommittedReplaySeed = new(
        id: DiagnosticIds.CommittedReplaySeed,
        title: "A replay seed is pinned in committed code",
        messageFormat: "Seed {0} is pinned: the values stop varying between runs, so the test no longer surfaces a dependency on one particular value",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Info,
        // Opt-in, and it must be: this repository's own maintainer guide instructs the opposite for a whole class of
        // tests ("Pin a seed for anything statistical"), so a rule enabled by default would fight documented practice.
        isEnabledByDefault: false,
        description: "The seeded overloads exist to replay a run a failure reported — correct while reproducing, wrong once committed, because the test then draws the same values for ever and stops surfacing the coupling the library exists to reveal. Opt-in: a statistical test legitimately pins a seed, and this repository's maintainer guide says so, which makes the rule a pre-release sweep rather than a standing check.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.CommittedReplaySeed));

    public static readonly DiagnosticDescriptor SharedStaticAnyContext = new(
        id: DiagnosticIds.SharedStaticAnyContext,
        title: "An AnyContext is shared through a static field",
        messageFormat: "Give each unit of work its own context: '{0}' is shared, and interleaved draws make neither the sequence nor the multiset stable across runs",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "AnyContext's own documentation states the hazard: a context is safe to draw from concurrently, but sharing one across threads costs the replay rather than the values. A static context looks maximally deterministic — a literal seed, right there in the source — while a parallel suite gets a different value per test per run from it.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.SharedStaticAnyContext));

    public static readonly DiagnosticDescriptor BlankReplaySnippet = new(
        id: DiagnosticIds.BlankReplaySnippet,
        title: "Any.UseSeed is given a blank replay snippet",
        messageFormat: "Pass the code a reader copies to replay the run, or drop the argument: a blank snippet is rejected at run time",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.UseSeed(int, string) rejects a blank snippet. Because that scope is normally opened from a test-framework adapter's hook, the throw surfaces as an infrastructure failure on every test in the suite rather than as one failing assertion — a disproportionately expensive way to learn about a typo the compiler can already see.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.BlankReplaySnippet));

    public static readonly DiagnosticDescriptor ParallelDrawWithoutPerItemSeed = new(
        id: DiagnosticIds.ParallelDrawWithoutPerItemSeed,
        title: "A parallel work item draws without its own seed scope",
        messageFormat: "Open an Any.UseSeed scope inside the work item: the ambient scope reaches every worker, so the draws interleave and the run replays nothing",
        category: DiagnosticCategories.Reproducibility,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The ambient seed scope flows with the execution context, so a scope opened around a parallel loop reaches every worker and their draws interleave: neither the sequence nor the multiset is stable across runs. A scope opened inside the loop body gives each unit of work its own sequence, and the whole run replays — the shape the library's documentation names.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ParallelDrawWithoutPerItemSeed));

    public static readonly DiagnosticDescriptor ScalarChainAdmitsNoValue = new(
        id: DiagnosticIds.ScalarChainAdmitsNoValue,
        title: "The declared scalar constraints admit no value",
        messageFormat: "No value satisfies this chain once {0} is applied",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The constant constraints narrow the domain to nothing, so the chain throws a ConflictingAnyConstraintException the moment the arrange line runs. The library computes this with one emptiness test over bounds, lattice and allow-list; this rule runs the same test over the constants written at the call site, and stays silent for every argument it cannot fold.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ScalarChainAdmitsNoValue));

    public static readonly DiagnosticDescriptor ConstraintWithNoEffect = new(
        id: DiagnosticIds.ConstraintWithNoEffect,
        title: "A constraint narrows nothing",
        messageFormat: "This constraint changes nothing: {0}",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The constraint is legal and inert: the domain it produces is the one that already existed. This is the only member of the constraint family the run time NEVER reports — every other contradiction throws eventually and loudly, while an inert constraint leaves the test green and exercising a domain the author did not write. The dangerous case is an exclusion of a sentinel the generator could never draw: it silently misses, and starts mattering the day someone widens the range.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.ConstraintWithNoEffect));

    public static readonly DiagnosticDescriptor DuplicatePoolValue = new(
        id: DiagnosticIds.DuplicatePoolValue,
        title: "The same value is listed twice in a pool",
        messageFormat: "This value is already in the pool; a duplicate neither weights it nor widens the domain",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A pool is deduplicated under the default equality when it is built, so a value listed twice contributes exactly once. The library declines to weight a pool on purpose — writing a value twice therefore cannot mean 'draw this more often', and the pool is one value smaller than it reads. That gap surfaces far from here, when a distinct collection over the pool gates against the real distinct count and reports a number the author cannot find in their source.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.DuplicatePoolValue));

    public static readonly DiagnosticDescriptor EmptyRelativeUri = new(
        id: DiagnosticIds.EmptyRelativeUri,
        title: "The declared relative URI is empty",
        messageFormat: "A relative URI with exactly 0 path segments and no query, fragment or root is empty, which is not a valid URI reference",
        category: DiagnosticCategories.Constraints,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The chain describes the empty reference, which no URI can be. The library reports it, but only at Generate() — this is the one constraint family member whose failure lands at act time rather than at the arrange line, so the stack points at the code under test instead of at the declaration that is wrong. Add WithQuery(), WithFragment(), Rooted(), or a positive segment count.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.EmptyRelativeUri));

    public static readonly DiagnosticDescriptor UnusedCombineOperand = new(
        id: DiagnosticIds.UnusedCombineOperand,
        title: "A Combine operand never reaches the composed value",
        messageFormat: "This generator is drawn and thrown away: the composer never reads its parameter '{0}'",
        category: DiagnosticCategories.Composition,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Combine draws every operand before calling the composer, so an operand the composer ignores is still generated — constraints, conflict checks and all — and then dropped. Nothing fails: the composed value is well-formed, and simply does not carry the part the call site says it carries. The usual causes are a constructor argument forgotten during a refactor and a composer whose parameters no longer line up with its operands. Rename the parameter to '_' to say the draw is deliberate.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.UnusedCombineOperand));

    public static readonly DiagnosticDescriptor InertDistinctness = new(
        id: DiagnosticIds.InertDistinctness,
        title: "Distinctness is declared over an element type that has no value equality",
        messageFormat: "Distinctness cannot bind here: '{0}' inherits reference equality, so every freshly generated element already counts as distinct",
        category: DiagnosticCategories.Composition,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The element type neither overrides Equals nor implements IEquatable, so the default comparer falls back to reference equality — and every element the generator produces is a new instance. Distinctness is therefore satisfied by construction and constrains nothing: the collection can hold the same value several times, which is precisely what the declaration asks it not to. The library cannot report this, because from its side the requirement is met. Give the type value equality, or pass an explicit comparer.",
        helpLinkUri: HelpLinks.For(DiagnosticIds.InertDistinctness));

}
