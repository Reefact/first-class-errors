using Microsoft.CodeAnalysis;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     Small operation-tree helpers shared by analyzers that walk method or lambda bodies.
/// </summary>
internal static class OperationFacts {

    /// <summary>
    ///     Enumerates an operation subtree, root first, guaranteeing every ancestor is yielded before its descendants.
    /// </summary>
    public static IEnumerable<IOperation> EnumerateOperations(IOperation root) {
        Stack<IOperation> pending = new();
        pending.Push(root);

        while (pending.Count > 0) {
            IOperation current = pending.Pop();
            yield return current;

            foreach (IOperation child in current.ChildOperations) {
                pending.Push(child);
            }
        }
    }

}
