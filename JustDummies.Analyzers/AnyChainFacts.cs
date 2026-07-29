using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Walks a fluent generator chain back to the factory that started it, and yields the constraint calls in the
///     order they were written. Every rule that reasons about a <b>combination</b> of constraints — rather than about
///     one argument — needs this, because the library's own conflict detection is order-sensitive in its messages and
///     order-insensitive in its verdict.
/// </summary>
/// <remarks>
///     Syntactic on purpose. The chain must be written as one expression; a generator passed through a local, a field
///     or a helper is not followed. That is a deliberate limit, not an oversight: following it would mean dataflow,
///     and a rule that claims a chain is unsatisfiable must be certain of every constraint the chain carries.
/// </remarks>
internal static class AnyChainFacts {

    /// <summary>
    ///     The constraint calls of the chain <paramref name="invocation" /> belongs to, outermost call last, together
    ///     with the factory that rooted it. Returns <c>false</c> when the chain does not start at a
    ///     <c>JustDummies.Any</c> / <c>AnyContext</c> factory, which is the only case a rule can reason about
    ///     completely.
    /// </summary>
    public static bool TryGetChain(IInvocationOperation invocation, KnownSymbols symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory) {
        List<IInvocationOperation> collected = [];
        constraints = collected;
        factory     = null;

        // Climb to the outermost invocation of the chain, so a rule registered on any link sees the whole of it.
        IInvocationOperation outermost = invocation;
        while (outermost.Parent is IInvocationOperation parent && ReferenceEquals(GeneratorFacts.Unwrap(parent.Instance ?? parent), outermost)) {
            outermost = parent;
        }

        for (IInvocationOperation? current = outermost; current is not null;) {
            if (current.Instance is null) {
                // A static call roots the chain: it is the factory when it belongs to Any, otherwise this is not a
                // JustDummies chain at all.
                if (!IsFactoryOwner(current.TargetMethod.ContainingType, symbols)) { return false; }

                factory = current;
                collected.Reverse();

                return true;
            }

            IOperation receiver = GeneratorFacts.Unwrap(current.Instance);

            // An instance call on AnyContext roots the chain the same way Any's static factories do.
            if (receiver is not IInvocationOperation next) {
                if (IsFactoryOwner(current.TargetMethod.ContainingType, symbols)) {
                    factory = current;
                    collected.Reverse();

                    return true;
                }

                return false;
            }

            collected.Add(current);
            current = next;
        }

        return false;
    }

    private static bool IsFactoryOwner(INamedTypeSymbol? type, KnownSymbols symbols) {
        return SymbolEqualityComparer.Default.Equals(type, symbols.Any)
            || SymbolEqualityComparer.Default.Equals(type, symbols.AnyContext);
    }

}
