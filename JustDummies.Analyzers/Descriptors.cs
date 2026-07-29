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

}
