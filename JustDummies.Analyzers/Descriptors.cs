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

}
